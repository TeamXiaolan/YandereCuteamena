using System.Collections;
using Dawn.Utils;
using Unity.Netcode;
using UnityEngine;

namespace YandereCuteamena;
internal class CuteamenaUtils : NetworkSingleton<CuteamenaUtils>
{
    public NetworkObjectReference SpawnScrap(Item? item, Vector3 position, bool defaultRotation, int valueIncrease, Quaternion rotation = default)
    {
        if (StartOfRound.Instance == null || item == null)
        {
            return default;
        }

        Transform? parent = null;
        if (parent == null)
        {
            parent = StartOfRound.Instance.propsContainer;
        }

        Vector3 spawnPosition = position;
        if (Physics.Raycast(position + Vector3.up * 1f, Vector3.down, out RaycastHit hit, 100f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
        {
            spawnPosition = hit.point + Vector3.up * item.verticalOffset;
        }

        GameObject go = Instantiate(item.spawnPrefab, spawnPosition + Vector3.up * 0.2f, Quaternion.identity, parent);
        GrabbableObject grabbableObject = go.GetComponent<GrabbableObject>();
        NetworkObject networkObject = grabbableObject.NetworkObject;
        grabbableObject.fallTime = 0;
        networkObject.Spawn();
        UpdateParentAndRotationsServerRpc(new NetworkObjectReference(go), defaultRotation ? Quaternion.Euler(item.restingRotation) : rotation);

        int value = (int)(UnityEngine.Random.Range(item.minValue, item.maxValue) * RoundManager.Instance.scrapValueMultiplier) + valueIncrease;
        ScanNodeProperties? scanNodeProperties = go.GetComponentInChildren<ScanNodeProperties>();
        if (scanNodeProperties != null)
        {
            scanNodeProperties.scrapValue = value;
            scanNodeProperties.subText = $"Value: ${value}";
            grabbableObject.scrapValue = value;
            UpdateScanNodeServerRpc(new NetworkObjectReference(networkObject), value);
        }
        return new NetworkObjectReference(go);
    }


    [ServerRpc(RequireOwnership = false)]
    public void UpdateParentAndRotationsServerRpc(NetworkObjectReference go, Quaternion rotation)
    {
        UpdateParentAndRotationsClientRpc(go, rotation);
    }

    [ClientRpc]
    public void UpdateParentAndRotationsClientRpc(NetworkObjectReference go, Quaternion rotation)
    {
        go.TryGet(out NetworkObject netObj);
        if (netObj != null)
        {
            if (netObj.AutoObjectParentSync && IsServer)
            {
                netObj.transform.parent = StartOfRound.Instance.propsContainer;
            }
            else if (!netObj.AutoObjectParentSync)
            {
                netObj.transform.parent = StartOfRound.Instance.propsContainer;
            }
            StartCoroutine(ForceRotationForABit(netObj.gameObject, rotation));
        }
    }

    private IEnumerator ForceRotationForABit(GameObject go, Quaternion rotation)
    {
        float duration = 0.25f;
        while (duration > 0)
        {
            duration -= Time.deltaTime;
            go.transform.rotation = rotation;
            yield return null;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdateScanNodeServerRpc(NetworkObjectReference go, int value)
    {
        UpdateScanNodeClientRpc(go, value);
    }

    [ClientRpc]
    public void UpdateScanNodeClientRpc(NetworkObjectReference go, int value)
    {
        go.TryGet(out NetworkObject netObj);
        if (netObj != null)
        {
            if (netObj.gameObject.TryGetComponent(out GrabbableObject grabbableObject))
            {
                grabbableObject.SetScrapValue(value);
            }
        }
    }
}