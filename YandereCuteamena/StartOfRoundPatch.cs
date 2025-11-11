using Dawn.Utils;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YandereCuteamena;
[HarmonyPatch(typeof(StartOfRound))]
static class StartOfRoundPatch
{
	[HarmonyPatch(nameof(StartOfRound.Awake))]
	[HarmonyPostfix]
	public static void StartOfRound_Awake(ref StartOfRound __instance)
	{
		__instance.NetworkObject.OnSpawn(CreateNetworkManager);
	}
	
	private static void CreateNetworkManager()
	{
		if (StartOfRound.Instance.IsServer || StartOfRound.Instance.IsHost)
		{
			if (CuteamenaUtils.Instance == null)
			{
				GameObject utilsInstance = GameObject.Instantiate(YandereCuteamena.Assets.UtilsPrefab);
				SceneManager.MoveGameObjectToScene(utilsInstance, StartOfRound.Instance.gameObject.scene);
				utilsInstance.GetComponent<NetworkObject>().Spawn();
			}
			else
			{
			}
		}
	}
}