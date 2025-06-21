using System.Collections.Generic;
using UnityEngine;

public class Level1EnemyAI : BaseEnemyAI
{
    private const float DefaultInitialStopDistance = 6000f;
    private const float DefaultWeaponRangeFallback = 600f;
    private const float EntityMoveCooldownDuration = 0.5f;
    private const float InitialMoveTargetBuffer = 50f;
    private const float FirstMoveSentinel = -1f;
    private readonly float initialStopDistance = DefaultInitialStopDistance;

    public override void ProcessCombatBehavior(List<Entity> aiEntities)
    {
        if (opponentBase == null) return;
        if (aiEntities.Count == 0) return;

        if (OpenOceanMain.inst.currentTrainingState == TrainingState.Adaptive)
        {
            HandleLevel1CombatAdaptiveBehavior(aiEntities);
        }
        else
        {
            HandleLevel1CombatNonAdaptiveBehavior(aiEntities);
        }
    }

    private void HandleLevel1CombatAdaptiveBehavior(List<Entity> aiEntities)
    {
        if (opponentBase == null) return;
        Vector3 opponentPos = opponentBase.position;

        float diff = GameMgr.inst.difficultyLevel;

        // Adaptive values
        float adaptiveInitialStopDistance = Mathf.Lerp(11000f, 6000f, diff);
        float adaptiveWeaponRangeFallback = Mathf.Lerp(800f, 400f, diff);
        float adaptiveCooldown = Mathf.Lerp(1f, 0.25f, diff);
        float adaptiveInitialBuffer = Mathf.Lerp(100f, 25f, diff);

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

            if (!entityCooldowns.ContainsKey(aiEntity))
            {
                entityCooldowns[aiEntity] = FirstMoveSentinel;
            }

            float currentDistance = Vector3.Distance(aiEntity.position, opponentPos);

            bool isInitialMovePhase = entityCooldowns[aiEntity] == FirstMoveSentinel;

            if (isInitialMovePhase)
            {
                if (currentDistance <= adaptiveInitialStopDistance + adaptiveInitialBuffer)
                {
                    entityCooldowns[aiEntity] = Time.time + adaptiveCooldown;
                }
            }
            else
            {
                if (Time.time < entityCooldowns[aiEntity])
                {
                    continue;
                }

                Entity nearestEnemy = FindNearestEnemy(aiEntity, weaponRange);
                if (nearestEnemy != null)
                {
                    unitAIComponent?.StopAndRemoveAllCommands();
                    entityCooldowns[aiEntity] = Time.time + adaptiveCooldown;
                    continue;
                }
                else
                {
                    entityCooldowns[aiEntity] = Time.time + adaptiveCooldown;
                }
            }

            AIMgr.inst.HandleMove(new List<Entity> { aiEntity }, opponentBase.position, false);
        }
    }

    private void HandleLevel1CombatNonAdaptiveBehavior(List<Entity> aiEntities)
    {
        if (opponentBase == null) return;
        Vector3 opponentPos = opponentBase.position;

        for (int i = aiEntities.Count - 1; i >= 0; i--)
        {
            Entity aiEntity = aiEntities[i];
            if (aiEntity == null) continue;

            WeaponsAspect weaponAspect = aiEntity.GetComponentInChildren<WeaponsAspect>();
            UnitAI unitAIComponent = aiEntity.GetComponentInChildren<UnitAI>();

            float weaponRange = DefaultWeaponRangeFallback;
            if (weaponAspect != null && weaponAspect.weapon != null)
            {
                weaponRange = weaponAspect.weapon.range;
            }

            if (!entityCooldowns.ContainsKey(aiEntity))
            {
                entityCooldowns[aiEntity] = FirstMoveSentinel;
            }

            float currentDistance = Vector3.Distance(aiEntity.position, opponentPos);

            bool isInitialMovePhase = entityCooldowns[aiEntity] == FirstMoveSentinel;

            if (isInitialMovePhase)
            {
                if (currentDistance <= initialStopDistance - InitialMoveTargetBuffer)
                {
                    entityCooldowns[aiEntity] = Time.time + EntityMoveCooldownDuration;
                }
            }
            else
            {
                if (Time.time < entityCooldowns[aiEntity])
                {
                    continue;
                }

                Entity nearestEnemy = FindNearestEnemy(aiEntity, weaponRange);
                if (nearestEnemy != null)
                {
                    if (unitAIComponent != null)
                    {
                        unitAIComponent.StopAndRemoveAllCommands();
                    }
                    entityCooldowns[aiEntity] = Time.time + EntityMoveCooldownDuration;
                    continue;
                }
                else
                {
                    entityCooldowns[aiEntity] = Time.time + EntityMoveCooldownDuration;
                }
            }
            AIMgr.inst.HandleMove(new List<Entity> { aiEntity }, opponentBase.position, false);
        }
    }
}
