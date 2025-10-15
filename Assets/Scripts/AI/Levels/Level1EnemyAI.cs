using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

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
    private const float StaticInitialStopDistance = 6000f;
    private const float FirstMoveSentinel = -1f;
    private bool _hasIssuedAdaptiveAttackMove = false;

    // Tunables for adaptive targeting
    private const float SenseRadius = 400f;
    private const float AttackMinScore = 2.5f;
    private const float RetreatOutnumberFactor = 1f;

    private const float W_Density = 0.15f;
    private const float W_LowHealth = 1.2f;
    private const float W_HighHealthPenalty = 1.0f;
    private const float W_Score = 0.4f;

    private Entity _cachedBestTarget;


    public override void ProcessCombatBehavior(List<Entity> aiEntities)
    {
        if (opponentBase == null) return;
        if (aiEntities.Count == 0) return;

        // Delegate to appropriate AI implementation based on training state
        if (OpenOceanMain.inst.currentTrainingState == TrainingState.Adaptive)
        {
            List<Entity> visibleOpponents = FindAllEnemyTargets();
            Transform fallbackPoint = GetFallbackPointTransform();
            ProcessAdaptiveBehavior(aiEntities, visibleOpponents, fallbackPoint);
        }
        else
        {
            ProcessStaticBehavior(aiEntities);
        }
    }

    #region Adaptive AI Implementation
    private void ProcessAdaptiveBehavior(List<Entity> allies, List<Entity> visibleOpponents, Transform fallbackPoint)
    {
        if (opponentBase == null) return;
        if (allies == null || allies.Count == 0) return;
        if (visibleOpponents == null || visibleOpponents.Count == 0) return;

        for (int i = 0; i < allies.Count; i++)
        {
            Entity ally = allies[i];
            if (ally != null && ally.isGreyed)
            {
                return;
            }
        }

        float healthSum = 0f;
        int validHealthCount = 0;
        int myAlive = 0;
        for (int i = 0; i < allies.Count; i++)
        {
            Entity ally = allies[i];
            if (ally == null) continue;

            healthSum += Mathf.Max(ally.health, 0f);
            validHealthCount++;

            if (ally.health > 0f)
            {
                myAlive++;
            }
        }

        if (validHealthCount == 0 || myAlive == 0) return;

        float myAvgHealth = Mathf.Max(1e-3f, healthSum / validHealthCount);

        Vector3 alliesCentroid = AveragePos(allies);
        int enemiesNearGroup = CountWithin(visibleOpponents, alliesCentroid, SenseRadius);
        int alliesNearGroup = CountWithin(allies, alliesCentroid, SenseRadius);
        bool globallyOutnumberedHere = enemiesNearGroup > alliesNearGroup;

        Vector3 fallbackPosition = fallbackPoint != null ? fallbackPoint.position : opponentBase.position;

        if (globallyOutnumberedHere && !_hasIssuedAdaptiveAttackMove)
        {
            AIMgr.inst.HandleAttackMove(allies, fallbackPosition, fallbackPoint != null ? fallbackPoint.GetComponent<Entity>() : null, false, acquireTarget: false);
            _hasIssuedAdaptiveAttackMove = true;
            return;
        }

        Entity bestTarget = null;
        float bestScore = float.NegativeInfinity;

        // Check if the cached target is still valid
        if (_cachedBestTarget != null && (_cachedBestTarget.health <= 0f || !visibleOpponents.Contains(_cachedBestTarget)))
        {
            _cachedBestTarget = null; // Invalidate cache if target is dead or not visible
        }

        foreach (Entity enemy in visibleOpponents)
        {
            if (enemy == null || enemy.health <= 0f) continue;

            int localEnemies = CountWithin(visibleOpponents, enemy.transform.position, SenseRadius);
            int localAllies = CountWithin(allies, enemy.transform.position, SenseRadius);
            bool outnumberedLocally = localEnemies > localAllies;

            float densityTerm = W_Density * localEnemies;
            float lowHealthTerm = W_LowHealth * Mathf.Clamp01((myAvgHealth - enemy.health) / myAvgHealth);
            float highHealthPenalty = enemy.health > myAvgHealth ? W_HighHealthPenalty : 0f;
            float scoreTerm = W_Score * GetEntityScore(enemy);

            float outnumberPenalty = outnumberedLocally ? RetreatOutnumberFactor : 0f;

            float attractiveness = densityTerm + lowHealthTerm + scoreTerm - highHealthPenalty - outnumberPenalty;

            if (attractiveness > bestScore)
            {
                bestScore = attractiveness;
                bestTarget = enemy;
                Debug.Log($"New best target: {enemy.name} with score {bestScore}");
            }
        }

        // Decide whether to switch to the new best target or stick with the cached one
        if (bestTarget != null && bestScore >= AttackMinScore)
        {
            // A simple hysteresis could be added here to prevent rapid target switching
            // For now, we'll just update to the new best target if it's better.
            _cachedBestTarget = bestTarget;
        }

        // If we have a valid target (either new or cached), attack it.
        if (_cachedBestTarget != null)
        {
            AIMgr.inst.HandleAttackMove(allies, _cachedBestTarget.transform.position, _cachedBestTarget, false, acquireTarget: true);
        }
    }
    #endregion

    private Transform GetFallbackPointTransform()
    {
        if (aiBases != null)
        {
            for (int i = 0; i < aiBases.Count; i++)
            {
                Entity baseEntity = aiBases[i];
                if (baseEntity != null)
                {
                    return baseEntity.transform;
                }
            }
        }

        return opponentBase != null ? opponentBase.transform : null;
    }

    private static Vector3 AveragePos(List<Entity> list)
    {
        Vector3 sum = Vector3.zero;
        int n = 0;
        for (int i = 0; i < list.Count; i++)
        {
            Entity entity = list[i];
            if (entity == null) continue;
            sum += entity.transform.position;
            n++;
        }

        return n > 0 ? sum / n : Vector3.zero;
    }

    private static int CountWithin(List<Entity> list, Vector3 pos, float radius)
    {
        float radiusSquared = radius * radius;
        int count = 0;
        for (int i = 0; i < list.Count; i++)
        {
            Entity entity = list[i];
            if (entity == null) continue;
            Vector3 delta = entity.transform.position - pos;
            if (delta.sqrMagnitude <= radiusSquared)
            {
                count++;
            }
        }

        return count;
    }

    private static float GetEntityScore(Entity entity)
    {
        if (entity == null) return 0f;

        // Base score can be derived from max health or a fixed value
        float baseScore = entity.maxHealth > 0f ? entity.maxHealth : 100f;

        // Assign score multipliers based on entity type/class
        // These values should be tuned based on gameplay balance.
        switch (entity.entityType)
        {
            // High-value combatants
            case EntityType.DDG51: // Destroyer
                baseScore *= 2.0f;
                break;

            // Medium-value combatants / USVs
            case EntityType.JARIUSV:
            case EntityType.SeaHunter:
                baseScore *= 1.2f;
                break;

            // High-value static targets
            case EntityType.Rig_Balder: // Assuming this is a base/rig
                baseScore *= 3.0f;
                break;

            // Default for other known combatants from original 
            // Default case for any other entity types
            default:
                baseScore *= 1.0f;
                break;
        }

        return baseScore;
    }

    #region Static AI Implementation
    private void ProcessStaticBehavior(List<Entity> aiEntities)
    {
        if (opponentBase == null) return;
        if (aiEntities.Count == 0) return;
        for (int i = 0; i < aiEntities.Count; i++)
        {
            if (aiEntities[i].isGreyed == true) return;
        }

        AIMgr.inst.HandleAttackMove(aiEntities, opponentBase.position, opponentBase.GetComponent<Entity>(), false, acquireTarget: true);

    }
    #endregion
}
