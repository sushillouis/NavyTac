using System.Collections.Generic;
using System.Linq; // Added for LINQ usage in PruneEntityCooldowns
using UnityEngine;

public class EnemyAIMgr : MonoBehaviour
{
    public int currentLevel = 1;
    public static EnemyAIMgr inst;

    private Entity opponentBase; // Cached opponent base
    public List<Entity> aiBases = new List<Entity>();
    private Dictionary<Entity, float> entityCooldowns = new Dictionary<Entity, float>();

    // Constants for configuration and magic numbers
    private const string AiOwnerName = "Ai";
    private const float DefaultUpdateInterval = 0.5f;
    private const float DefaultInitialStopDistance = 2000f;
    private const float DefaultMinDistanceReduction = 200f;
    private const float DefaultWeaponRangeFallback = 600f;
    private const float EntityMoveCooldownDuration = 0.5f;
    private const float InitialMoveTargetBuffer = 50f; // Buffer for reaching initial stop distance
    private const float FirstMoveSentinel = -1f; // Sentinel value for entityCooldowns indicating initial move

    private float updateInterval = DefaultUpdateInterval;
    private float lastUpdateTime;
    private float initialStopDistance = DefaultInitialStopDistance;
    private float minDistanceReduction = DefaultMinDistanceReduction;
    private readonly System.StringComparison aiOwnerNameComparison = System.StringComparison.OrdinalIgnoreCase;

    void Awake()
    {
        if (inst == null)
        {
            inst = this;
            // DontDestroyOnLoad(gameObject); // Uncomment if this manager should persist across scenes
        }
        else if (inst != this)
        {
            Debug.LogWarning("Duplicate EnemyAIMgr instance found. Destroying this one.");
            Destroy(gameObject);
            return;
        }
    }

    private void Update()
    {
        if (Time.time - lastUpdateTime < updateInterval) return;
        lastUpdateTime = Time.time;

        FindAIBases();
        CheckAndLogDestroyedAIBases();

        if (aiBases.Count == 0)
        {
            HandleNoAIBases();
            return;
        }

        UpdateOpponentBaseCache();
        if (opponentBase == null)
        {
            HandleNoOpponentBase();
            return;
        }

        List<Entity> aiEntities = GetAIEntities();
        if (aiEntities.Count == 0)
        {
            // No AI units available, could be a specific game state or just wait.
            return;
        }
        
        PruneEntityCooldowns(aiEntities);
        ProcessLevelBehavior(aiEntities);
    }

    private void PruneEntityCooldowns(List<Entity> activeAiEntities)
    {
        var activeSet = new HashSet<Entity>(activeAiEntities);
        var keysToRemove = entityCooldowns.Keys
            .Where(entityKey => entityKey == null || !activeSet.Contains(entityKey))
            .ToList();

        foreach (var key in keysToRemove)
        {
            entityCooldowns.Remove(key);
        }
    }

    private void CheckAndLogDestroyedAIBases()
    {
        for (int i = aiBases.Count - 1; i >= 0; i--)
        {
            Entity baseEntity = aiBases[i];
            if (baseEntity == null) // Unity's overloaded null check for destroyed objects
            {
                Debug.Log("AI base previously identified was found destroyed.");
                aiBases.RemoveAt(i);
            }
        }
    }

    private void FindAIBases()
    {
        aiBases.Clear();
        foreach (Entity e in EntityMgr.inst.entities)
        {
            // Using Unity's overloaded null check (e == null) is important for destroyed GameObjects
            if (e == null || e.owner == null ||
                !e.owner.name.Equals(AiOwnerName, aiOwnerNameComparison) ||
                e.entityRole != EntityRole.Base)
            {
                continue;
            }
            aiBases.Add(e);
        }
    }
    
    private void HandleNoAIBases()
    {
        Debug.Log("No AI bases found. This might trigger a game event (e.g., AI loss or scene reload).");
        // GameMgr.inst.ReloadScene(); // Example action
    }

    private void UpdateOpponentBaseCache()
    {
        // If opponentBase is destroyed, Unity's overloaded '==' will make it appear null.
        // Also, ensure it's still in the canonical list of entities if EntityMgr prunes destroyed ones.
        if (opponentBase == null || !EntityMgr.inst.entities.Contains(opponentBase))
        {
            opponentBase = FindOpponentBase();
        }
    }

    private void HandleNoOpponentBase()
    {
        ScoreMgr.inst.aiWon = true;
        ScoreMgr.inst.CheckVictory();
    }

    private void ProcessLevelBehavior(List<Entity> aiEntities)
    {
        switch (currentLevel)
        {
            case 1:
                HandleLevel1CombatBehavior(aiEntities);
                break;
            case 2:
                HandleLevel2CombatBehavior(aiEntities);
                break;
            default:
                Debug.LogWarning($"Unhandled AI level: {currentLevel}");
                break;
        }
    }

    private void HandleLevel1CombatBehavior(List<Entity> aiEntities)
    {
        Vector3 opponentPos = opponentBase.position;

        for (int i = aiEntities.Count - 1; i >= 0; i--)
        {
            Entity aiEntity = aiEntities[i];
            if (aiEntity == null) continue; // Skip destroyed entities

            WeaponsAspect weaponAspect = aiEntity.GetComponentInChildren<WeaponsAspect>();
            float weaponRange = weaponAspect != null ? weaponAspect.weapon.range : DefaultWeaponRangeFallback;

            if (!entityCooldowns.ContainsKey(aiEntity))
            {
                entityCooldowns[aiEntity] = FirstMoveSentinel; // Mark for initial move
            }

            float currentDistance = Vector3.Distance(aiEntity.position, opponentPos);
            float targetDistance;

            bool isInitialMovePhase = entityCooldowns[aiEntity] == FirstMoveSentinel;

            if (isInitialMovePhase)
            {
                targetDistance = initialStopDistance;
                if (currentDistance <= initialStopDistance + InitialMoveTargetBuffer)
                {
                    // Completed initial approach, set cooldown for next decision
                    entityCooldowns[aiEntity] = Time.time + EntityMoveCooldownDuration;
                }
                // If not yet at initialStopDistance + buffer, no cooldown is set here;
                // it will re-evaluate next frame until it reaches the spot or the state changes.
            }
            else // Not initial move phase, regular behavior
            {
                // Check if entity is cooling down from a previous move/action
                if (Time.time < entityCooldowns[aiEntity]) 
                {
                    continue; // Still cooling down
                }

                Entity nearestEnemy = FindNearestEnemy(aiEntity, weaponRange);
                if (nearestEnemy != null)
                {
                    UnitAI unitAI = aiEntity.GetComponentInChildren<UnitAI>();
                    unitAI?.StopAndRemoveAllCommands(); // Engage enemy
                    // Potentially set a cooldown after engaging
                    entityCooldowns[aiEntity] = Time.time + EntityMoveCooldownDuration; 
                    continue; 
                }
                else
                {
                    targetDistance = CalculateDynamicTargetDistance(aiEntity, weaponRange, currentDistance);
                    entityCooldowns[aiEntity] = Time.time + EntityMoveCooldownDuration; // Set cooldown for next move decision
                }
            }
            AIMgr.inst.HandleMove(new List<Entity> { aiEntity }, opponentBase.position, false, doneDistanceSq: targetDistance * targetDistance);
        }
    }

    private void HandleLevel2CombatBehavior(List<Entity> aiEntities)
    {
        // For Level 2, AI directly attack-moves towards the opponent base.
        // Cooldowns or complex positioning might not be needed here as per original logic.
        AIMgr.inst.HandleAttackMove(aiEntities, opponentBase.position, null, false);
    }

    private float CalculateDynamicTargetDistance(Entity aiEntity, float weaponRange, float currentDistance)
    {
        float targetDist = initialStopDistance; // Default to initial stop distance

        // If no enemies are nearby (checked up to initialStopDistance) and we're beyond weapon range, gradually reduce the distance
        if (currentDistance > weaponRange && FindNearestEnemy(aiEntity, initialStopDistance) == null)
        {
            targetDist = Mathf.Max(weaponRange, currentDistance - minDistanceReduction);
        }
        else if (currentDistance <= weaponRange) // Once within weapon range (or if enemies were found closer), target weapon range
        {
            targetDist = weaponRange;
        }
        return targetDist;
    }

    private Entity FindNearestEnemy(Entity aiEntity, float range)
    {
        Vector3 aiPos = aiEntity.position;
        float sqrRange = range * range;
        Entity nearest = null;
        float nearestDistSq = float.MaxValue;

        foreach (Entity e in EntityMgr.inst.entities)
        {
            if (e == null || e.owner == null || e.entityClass == EntityClass.Missile ||
                e.owner.name.Equals(AiOwnerName, aiOwnerNameComparison)) // Skip own units and missiles
            {
                continue;
            }

            float distSq = (e.position - aiPos).sqrMagnitude;
            if (distSq < sqrRange && distSq < nearestDistSq)
            {
                nearest = e;
                nearestDistSq = distSq;
            }
        }
        return nearest;
    }

    private Entity FindOpponentBase()
    {
        foreach (Entity e in EntityMgr.inst.entities)
        {
            if (e == null || e.owner == null ||
                e.owner.name.Equals(AiOwnerName, aiOwnerNameComparison) ||
                e.entityRole != EntityRole.Base)
            {
                continue;
            }
            return e; // Found an opponent's base
        }
        return null;
    }

    private List<Entity> GetAIEntities()
    {
        List<Entity> result = new List<Entity>(EntityMgr.inst.entities.Count / 2); // Pre-allocate
        foreach (Entity e in EntityMgr.inst.entities)
        {
            if (e == null || e.owner == null ||
                !e.owner.name.Equals(AiOwnerName, aiOwnerNameComparison) ||
                e.entityClass == EntityClass.Missile) // Exclude missiles
            {
                continue;
            }
            result.Add(e);
        }
        return result;
    }
}