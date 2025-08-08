using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Adaptive AI implementation for Level 1 enemy behavior.
/// This AI adjusts its parameters based on the current difficulty level,
/// providing dynamic challenge scaling.
/// </summary>
public class Level1AdaptiveEnemyAI : BaseEnemyAI
{
    private const float DefaultWeaponRangeFallback = 600f;
    private const float FirstMoveSentinel = -1f;

    // Adaptive parameters - these will be adjusted based on difficulty
    private readonly float baseInitialStopDistanceEasy = 11000f;
    private readonly float baseInitialStopDistanceHard = 6000f;
    private readonly float baseWeaponRangeFallbackEasy = 800f;
    private readonly float baseWeaponRangeFallbackHard = 400f;
    private readonly float baseCooldownEasy = 1f;
    private readonly float baseCooldownHard = 0.25f;
    private readonly float baseInitialBufferEasy = 100f;
    private readonly float baseInitialBufferHard = 25f;

    public override void ProcessCombatBehavior(List<Entity> aiEntities)
    {
        if (opponentBase == null) return;
        if (aiEntities.Count == 0) return;

        HandleAdaptiveCombatBehavior(aiEntities);
    }

    /// <summary>
    /// Handles combat behavior with adaptive parameters based on difficulty level.
    /// Higher difficulty results in more aggressive behavior with shorter cooldowns
    /// and closer engagement distances.
    /// </summary>
    private void HandleAdaptiveCombatBehavior(List<Entity> aiEntities)
    {
        if (opponentBase == null) return;
        Vector3 opponentPos = opponentBase.position;

        float diff = GameMgr.inst.difficultyLevel;

        // Calculate adaptive values based on difficulty level
        float adaptiveInitialStopDistance = Mathf.Lerp(baseInitialStopDistanceEasy, baseInitialStopDistanceHard, diff);
        float adaptiveWeaponRangeFallback = Mathf.Lerp(baseWeaponRangeFallbackEasy, baseWeaponRangeFallbackHard, diff);
        float adaptiveCooldown = Mathf.Lerp(baseCooldownEasy, baseCooldownHard, diff);
        float adaptiveInitialBuffer = Mathf.Lerp(baseInitialBufferEasy, baseInitialBufferHard, diff);

        for (int i = aiEntities.Count - 1; i >= 0; i--)
        {
            Entity aiEntity = aiEntities[i];
            if (aiEntity == null) continue;

            WeaponsAspect weaponAspect = aiEntity.GetComponentInChildren<WeaponsAspect>();
            UnitAI unitAIComponent = aiEntity.GetComponentInChildren<UnitAI>();

            float baseRange = weaponAspect != null && weaponAspect.weapon != null
                ? weaponAspect.weapon.range - 100f
                : adaptiveWeaponRangeFallback;

            float weaponRange = baseRange;

            // Initialize entity cooldown if not present
            if (!entityCooldowns.ContainsKey(aiEntity))
            {
                entityCooldowns[aiEntity] = FirstMoveSentinel;
            }

            float currentDistance = Vector3.Distance(aiEntity.position, opponentPos);
            bool isInitialMovePhase = entityCooldowns[aiEntity] == FirstMoveSentinel;

            if (isInitialMovePhase)
            {
                // Initial movement phase - move until within adaptive stop distance
                if (currentDistance <= adaptiveInitialStopDistance + adaptiveInitialBuffer)
                {
                    entityCooldowns[aiEntity] = Time.time + adaptiveCooldown;
                }
            }
            else
            {
                // Regular combat phase with cooldown management
                if (Time.time < entityCooldowns[aiEntity])
                {
                    continue; // Still on cooldown
                }

                Entity nearestEnemy = FindNearestEnemy(aiEntity, weaponRange);
                if (nearestEnemy != null)
                {
                    // Enemy found within range - stop and engage
                    unitAIComponent?.StopAndRemoveAllCommands();
                    entityCooldowns[aiEntity] = Time.time + adaptiveCooldown;
                    continue;
                }
                else
                {
                    // No enemy in range - prepare for next move
                    entityCooldowns[aiEntity] = Time.time + adaptiveCooldown;
                }
            }

            // Execute movement command
            AIMgr.inst.HandleMove(new List<Entity> { aiEntity }, opponentBase.position, false);
        }
    }
}
