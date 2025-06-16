using Unity.Netcode;
using GameNetcodeStuff;
using UnityEngine;
using System.Linq;
using Unity.Netcode.Components;
using CodeRebirthLib.Util.Pathfinding;

namespace YandereCuteamena;

[RequireComponent(typeof(SmartAgentNavigator))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(NetworkAnimator))]
[RequireComponent(typeof(NetworkTransform))]
[RequireComponent(typeof(Collider))]
public abstract class AdvancedEnemyAI : EnemyAI
{
    internal EnemyAI? targetEnemy = null;
    internal PlayerControllerB? previousTargetPlayer = null;

    [Header("Required Components")]
    [SerializeField]
    internal NetworkAnimator creatureNetworkAnimator = null!;
    [SerializeField]
    internal SmartAgentNavigator smartAgentNavigator = null!;

    [Header("Inherited Fields")]
    public AudioClip[] _hitBodySounds = [];
    public AudioClip spawnSound = null!;

    [HideInInspector]
    public System.Random enemyRandom = new();

    public override void Start()
    {
        base.Start();
        enemyRandom = new System.Random(StartOfRound.Instance.randomMapSeed + RoundManager.Instance.SpawnedEnemies.Count + 69);

        if (spawnSound != null)
            creatureVoice.PlayOneShot(spawnSound);


        smartAgentNavigator.OnUseEntranceTeleport.AddListener(SetEnemyOutside);
        smartAgentNavigator.SetAllValues(isOutside);
        // Plugin.ExtendedLogging(enemyType.enemyName + " Spawned.");

        GrabEnemyRarity(enemyType.enemyName);
    }

    public override void HitEnemy(int force = 1, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
        if (!isEnemyDead && playHitSFX && _hitBodySounds.Length > 0)
        {
            creatureSFX.PlayOneShot(_hitBodySounds[enemyRandom.Next(_hitBodySounds.Length)]);
        }
    }

    public void GrabEnemyRarity(string enemyName)
    {
        // Search in OutsideEnemies
        SpawnableEnemyWithRarity? enemy = RoundManager.Instance.currentLevel.OutsideEnemies
            .FirstOrDefault(x => x.enemyType.enemyName.Equals(enemyName)) ?? RoundManager.Instance.currentLevel.DaytimeEnemies
                .FirstOrDefault(x => x.enemyType.enemyName.Equals(enemyName)) ?? RoundManager.Instance.currentLevel.Enemies
                    .FirstOrDefault(x => x.enemyType.enemyName.Equals(enemyName));

        /*foreach (var spawnableEnemyWithRarity in RoundManager.Instance.currentLevel.Enemies)
        {
            Plugin.ExtendedLogging($"{spawnableEnemyWithRarity.enemyType.enemyName} has Rarity: {spawnableEnemyWithRarity.rarity.ToString()}");
        }

        foreach (var spawnableEnemyWithRarity in RoundManager.Instance.currentLevel.DaytimeEnemies)
        {
            Plugin.ExtendedLogging($"{spawnableEnemyWithRarity.enemyType.enemyName} has Rarity: {spawnableEnemyWithRarity.rarity.ToString()}");
        }

        foreach(var spawnableEnemyWithRarity in RoundManager.Instance.currentLevel.OutsideEnemies)
        {
            Plugin.ExtendedLogging($"{spawnableEnemyWithRarity.enemyType.enemyName} has Rarity: {spawnableEnemyWithRarity.rarity.ToString()}");
        }*/

        // Log the result
        if (enemy != null)
        {
            YandereCuteamena.Logger.LogDebug(enemyName + " has Rarity: " + enemy.rarity.ToString());
        }
        else
        {
            YandereCuteamena.Logger.LogWarning("Enemy not found.");
        }
    }

    public bool FindClosestPlayerInRange(float range, bool targetAlreadyTargettedPerson = true)
    {
        PlayerControllerB? closestPlayer = null;
        float minDistance = float.MaxValue;

        foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
        {
            bool onSight = player.IsSpawned && player.isPlayerControlled && !player.isPlayerDead && !player.isInHangarShipRoom && EnemyHasLineOfSightToPosition(player.transform.position, 60f, range);
            if (!onSight) continue;

            if (CheckIfPersonAlreadyTargetted(targetAlreadyTargettedPerson, player)) continue;

            float distance = Vector3.Distance(transform.position, player.transform.position);
            bool closer = distance < minDistance;
            if (!closer) continue;

            minDistance = distance;
            closestPlayer = player;
        }
        if (closestPlayer == null) return false;

        targetPlayer = closestPlayer;
        return true;
    }

    public bool CheckIfPersonAlreadyTargetted(bool targetAlreadyTargettedPerson, PlayerControllerB playerToCheck)
    {
        if (!targetAlreadyTargettedPerson) return false;
        foreach (var enemy in RoundManager.Instance.SpawnedEnemies)
        {
            if (enemy is AdvancedEnemyAI codeRebirthEnemyAI)
            {
                if (codeRebirthEnemyAI.targetPlayer == playerToCheck)
                    return true;
            }
        }
        return false;
    }

    public bool EnemyHasLineOfSightToPosition(Vector3 pos, float width = 60f, float range = 20f, float proximityAwareness = 5f)
    {
        Transform eyeTransform;
        if (eye == null)
        {
            eyeTransform = transform;
        }
        else
        {
            eyeTransform = eye;
        }

        float distance = Vector3.Distance(eyeTransform.position, pos);
        if (distance < proximityAwareness)
            return true;

        if (distance >= range || Physics.Linecast(eyeTransform.position, pos, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
            return false;

        Vector3 to = pos - eyeTransform.position;
        return Vector3.Angle(eyeTransform.forward, to) < width;
    }

    public bool PlayerLookingAtEnemy(PlayerControllerB player, float dotProductThreshold)
    {
        Vector3 directionToEnemy = (transform.position - player.gameObject.transform.position).normalized;
        if (Vector3.Dot(player.gameplayCamera.transform.forward, directionToEnemy) < dotProductThreshold)
            return false;

        if (Physics.Linecast(player.gameplayCamera.transform.position, transform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
            return false;

        return true;
    }

    public bool EnemySeesPlayer(PlayerControllerB player, float dotThreshold)
    {
        Transform mainTransform;
        if (eye == null)
        {
            mainTransform = this.transform;
        }
        else
        {
            mainTransform = eye.transform;
        }

        Vector3 directionToPlayer = (player.transform.position - mainTransform.position).normalized;
        if (Vector3.Dot(transform.forward, directionToPlayer) < dotThreshold)
            return false;

        float distanceToPlayer = Vector3.Distance(mainTransform.position, player.transform.position);
        if (Physics.Raycast(mainTransform.position, directionToPlayer, distanceToPlayer, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
            return false;

        return true;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetTargetServerRpc(int PlayerID)
    {
        SetTargetClientRpc(PlayerID);
    }

    [ClientRpc]
    public void SetTargetClientRpc(int PlayerID)
    {
        if (PlayerID == -1)
        {
            targetPlayer = null;
            PlayerSetAsTarget(null);
            return;
        }
        if (StartOfRound.Instance.allPlayerScripts[PlayerID] == null)
        {
            PlayerSetAsTarget(null);
            return;
        }
        previousTargetPlayer = targetPlayer;
        targetPlayer = StartOfRound.Instance.allPlayerScripts[PlayerID];
        PlayerSetAsTarget(targetPlayer);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetEnemyTargetServerRpc(int enemyID)
    {
        SetEnemyTargetClientRpc(enemyID);
    }

    [ClientRpc]
    public void SetEnemyTargetClientRpc(int enemyID)
    {
        if (enemyID == -1)
        {
            targetEnemy = null;
            EnemySetAsTarget(null);
            return;
        }

        if (RoundManager.Instance.SpawnedEnemies[enemyID] == null)
        {
            EnemySetAsTarget(null);
            return;
        }
        targetEnemy = RoundManager.Instance.SpawnedEnemies[enemyID];
        EnemySetAsTarget(targetEnemy);
    }

    public virtual void EnemySetAsTarget(EnemyAI? enemy)
    {

    }

    public virtual void PlayerSetAsTarget(PlayerControllerB? player)
    {

    }
}