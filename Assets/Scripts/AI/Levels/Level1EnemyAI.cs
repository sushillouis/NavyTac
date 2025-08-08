using System.Collections.Generic;
using UnityEngine;

public class Level1EnemyAI : BaseEnemyAI
{
    // Constants for adaptive AI
    private const float AdaptiveBaseInitialStopDistanceEasy = 11000f;
    private const float AdaptiveBaseInitialStopDistanceHard = 6000f;
    private const float AdaptiveBaseWeaponRangeFallbackEasy = 800f;
    private const float AdaptiveBaseWeaponRangeFallbackHard = 400f;
    private const float AdaptiveBaseCooldownEasy = 1f;
    private const float AdaptiveBaseCooldownHard = 0.25f;
    private const float AdaptiveBaseInitialBufferEasy = 100f;
    private const float AdaptiveBaseInitialBufferHard = 25f;

    // Constants for static AI
    private const float StaticInitialStopDistance = 6000f;
    private const float StaticWeaponRangeFallback = 600f;
    private const float StaticEntityMoveCooldownDuration = 0.5f;
    private const float StaticInitialMoveTargetBuffer = 50f;
    
    // Common constants
    private const float FirstMoveSentinel = -1f;

    public override void ProcessCombatBehavior(List<Entity> aiEntities)
    {
        if (opponentBase == null) return;
        if (aiEntities.Count == 0) return;

        // Delegate to appropriate AI implementation based on training state
        if (OpenOceanMain.inst.currentTrainingState == TrainingState.Adaptive)
        {
            ProcessAdaptiveBehavior(aiEntities);
        }
        else
        {
            ProcessStaticBehavior(aiEntities);
        }
    }

    #region Adaptive AI Implementation
    private void ProcessAdaptiveBehavior(List<Entity> aiEntities)
    {
        if (opponentBase == null) return;
        Vector3 opponentPos = opponentBase.position;

        float diff = GameMgr.inst.difficultyLevel;

        // Calculate adaptive values based on difficulty level
        float adaptiveInitialStopDistance = Mathf.Lerp(AdaptiveBaseInitialStopDistanceEasy, AdaptiveBaseInitialStopDistanceHard, diff);
        float adaptiveWeaponRangeFallback = Mathf.Lerp(AdaptiveBaseWeaponRangeFallbackEasy, AdaptiveBaseWeaponRangeFallbackHard, diff);
        float adaptiveCooldown = Mathf.Lerp(AdaptiveBaseCooldownEasy, AdaptiveBaseCooldownHard, diff);
        float adaptiveInitialBuffer = Mathf.Lerp(AdaptiveBaseInitialBufferEasy, AdaptiveBaseInitialBufferHard, diff);

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
    #endregion

    #region Static AI Implementation
    private void ProcessStaticBehavior(List<Entity> aiEntities)
    {
        if (opponentBase == null) return;
        Vector3 opponentPos = opponentBase.position;

        for (int i = aiEntities.Count - 1; i >= 0; i--)
        {
            Entity aiEntity = aiEntities[i];
            if (aiEntity == null) continue;

            WeaponsAspect weaponAspect = aiEntity.GetComponentInChildren<WeaponsAspect>();
            UnitAI unitAIComponent = aiEntity.GetComponentInChildren<UnitAI>();

            // Use static weapon range or fallback
            float weaponRange = StaticWeaponRangeFallback;
            if (weaponAspect != null && weaponAspect.weapon != null)
            {
                weaponRange = weaponAspect.weapon.range;
            }

            // Initialize entity cooldown if not present
            if (!entityCooldowns.ContainsKey(aiEntity))
            {
                entityCooldowns[aiEntity] = FirstMoveSentinel;
            }

            float currentDistance = Vector3.Distance(aiEntity.position, opponentPos);
            bool isInitialMovePhase = entityCooldowns[aiEntity] == FirstMoveSentinel;

            if (isInitialMovePhase)
            {
                // Initial movement phase - move until within static stop distance
                if (currentDistance <= StaticInitialStopDistance - StaticInitialMoveTargetBuffer)
                {
                    entityCooldowns[aiEntity] = Time.time + StaticEntityMoveCooldownDuration;
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
                    if (unitAIComponent != null)
                    {
                        unitAIComponent.StopAndRemoveAllCommands();
                    }
                    entityCooldowns[aiEntity] = Time.time + StaticEntityMoveCooldownDuration;
                    continue;
                }
                else
                {
                    // No enemy in range - prepare for next move
                    entityCooldowns[aiEntity] = Time.time + StaticEntityMoveCooldownDuration;
                }
            }

            // Execute movement command
            AIMgr.inst.HandleMove(new List<Entity> { aiEntity }, opponentBase.position, false);
        }
    }
    #endregion
}
