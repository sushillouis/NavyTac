using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static AI implementation for Level 1 enemy behavior.
/// This AI uses fixed parameters regardless of difficulty level,
/// providing consistent and predictable behavior.
/// </summary>
public class Level1StaticEnemyAI : BaseEnemyAI
{
    // Static/Fixed parameters - these remain constant regardless of difficulty
    private const float StaticInitialStopDistance = 6000f;
    private const float StaticWeaponRangeFallback = 600f;
    private const float StaticEntityMoveCooldownDuration = 0.5f;
    private const float StaticInitialMoveTargetBuffer = 50f;
    private const float FirstMoveSentinel = -1f;

    public override void ProcessCombatBehavior(List<Entity> aiEntities)
    {
        if (opponentBase == null) return;
        if (aiEntities.Count == 0) return;

        HandleStaticCombatBehavior(aiEntities);
    }

    /// <summary>
    /// Handles combat behavior with static/fixed parameters.
    /// Behavior remains consistent regardless of difficulty level.
    /// </summary>
    private void HandleStaticCombatBehavior(List<Entity> aiEntities)
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
}
