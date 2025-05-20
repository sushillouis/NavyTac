using System.Collections.Generic;
using System.Linq; 
using UnityEngine;

public class EnemyAIMgr : MonoBehaviour
{
    public int currentLevel = 1; 
    public static EnemyAIMgr inst; 

    private Entity opponentBase; 
    public List<Entity> aiBases = new List<Entity>(); 
    private Dictionary<Entity, float> entityCooldowns = new Dictionary<Entity, float>(); 

    private const string AiOwnerName = "Ai"; 
    private const float DefaultUpdateInterval = 0.5f; 
    private const float DefaultInitialStopDistance = 6000f; 
    private const float DefaultMinDistanceReduction = 200f; 
    private const float DefaultWeaponRangeFallback = 600f; 
    private const float EntityMoveCooldownDuration = 0.5f; 
    private const float InitialMoveTargetBuffer = 50f; 
    private const float FirstMoveSentinel = -1f; 

    private const float BaseDefenseRadiusLevel3 = 1200f; 
    private const float DefenderHoldDistanceLevel3 = 150f; 
    private const float DefenderOffsetFromBaseLevel3 = 250f; 
    private static readonly Vector3 MapCenterLevel3 = Vector3.zero; 
    private const float DefenderPositionToleranceSqLevel3 = 25f; 


    private readonly float updateInterval = DefaultUpdateInterval; 
    private float lastUpdateTime; 
    private readonly float initialStopDistance = DefaultInitialStopDistance; 
    private readonly float minDistanceReduction = DefaultMinDistanceReduction; 
    private readonly System.StringComparison aiOwnerNameComparison = System.StringComparison.OrdinalIgnoreCase; 

    private List<Entity> _reusableAiEntitiesList = new List<Entity>();
    private List<Entity> _reusableEnemyTargetsList = new List<Entity>();
    private List<Entity> _reusableThreatsNearBaseList = new List<Entity>();
    private List<Entity> _reusableDefendersList = new List<Entity>();
    private List<Entity> _reusableAttackersList = new List<Entity>();
    private List<Entity> _keysToRemoveCache = new List<Entity>();
    private HashSet<Entity> _activeEntitiesSetCache = new HashSet<Entity>();


    void Awake()
    {
        if (inst == null)
        {
            inst = this;
        }
        else if (inst != this)
        {
            //Debug.LogWarning("Duplicate EnemyAIMgr instance found. Destroying this one.");
            Destroy(gameObject);
            return;
        }
    }

    private void Update()
    {
        if (Time.time - lastUpdateTime < updateInterval) return;
        lastUpdateTime = Time.time;

        CheckAndLogDestroyedAIBases(); 
        FindAIBases(); 

        if (aiBases.Count == 0)
        {
            if (currentLevel == 3)
            {
            }
            else 
            {
                HandleNoAIBases(); 
                return;
            }
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
            return; 
        }
        
        PruneEntityCooldowns(aiEntities); 
        ProcessLevelBehavior(aiEntities); 
    }

    private void PruneEntityCooldowns(List<Entity> activeAiEntities)
    {
        _activeEntitiesSetCache.Clear();
        foreach (Entity entity in activeAiEntities)
        {
            if (entity != null) 
            {
                _activeEntitiesSetCache.Add(entity);
            }
        }

        _keysToRemoveCache.Clear();
        foreach (KeyValuePair<Entity, float> pair in entityCooldowns)
        {
            Entity entityKey = pair.Key;
            if (entityKey == null || !_activeEntitiesSetCache.Contains(entityKey))
            {
                _keysToRemoveCache.Add(entityKey);
            }
        }

        foreach (var key in _keysToRemoveCache)
        {
            entityCooldowns.Remove(key);
        }
    }

    private void CheckAndLogDestroyedAIBases()
    {
        for (int i = aiBases.Count - 1; i >= 0; i--)
        {
            Entity baseEntity = aiBases[i];
            if (baseEntity == null) 
            {
                //Debug.Log("AI base previously identified was found destroyed.");
                aiBases.RemoveAt(i);
            }
        }
    }

    private void FindAIBases()
    {
        aiBases.Clear(); 
        foreach (Entity e in EntityMgr.inst.entities)
        {
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
        //Debug.Log("No AI bases found. This might trigger a game event (e.g., AI loss or scene reload).");
    }

    private void UpdateOpponentBaseCache()
    {
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
            case 3:
                HandleLevel3CombatBehavior(aiEntities);
                break;
            default:
                //Debug.LogWarning($"Unhandled AI level: {currentLevel}");
                break;
        }
    }

    private void HandleLevel1CombatBehavior(List<Entity> aiEntities)
    {
        if (opponentBase == null) return; // opponentBase should be valid due to checks in Update()
        Vector3 opponentPos = opponentBase.position; // Position of the opponent's base.

        // Iterate backwards to safely handle potential removal or modification of the list during iteration (though not done here).
        for (int i = aiEntities.Count - 1; i >= 0; i--)
        {
            Entity aiEntity = aiEntities[i];
            if (aiEntity == null) continue; // Skip null entities.

            WeaponsAspect weaponAspect = aiEntity.GetComponentInChildren<WeaponsAspect>();
            UnitAI unitAIComponent = aiEntity.GetComponentInChildren<UnitAI>();
            
            // Determine the weapon range, using a fallback if necessary.
            float weaponRange = weaponAspect != null && weaponAspect.weapon != null ? weaponAspect.weapon.range : DefaultWeaponRangeFallback;

            // Initialize cooldown if this entity is new to the system.
            if (!entityCooldowns.ContainsKey(aiEntity))
            {
                entityCooldowns[aiEntity] = FirstMoveSentinel; // Mark for initial move phase.
            }

            float currentDistance = Vector3.Distance(aiEntity.position, opponentPos);
            float targetDistance; // The distance the AI unit will try to maintain from the target.

            bool isInitialMovePhase = entityCooldowns[aiEntity] == FirstMoveSentinel;

            if (isInitialMovePhase)
            {
                targetDistance = initialStopDistance; // Target the initial stopping distance.
                // If close enough to the initial stop distance, transition out of the initial move phase.
                if (currentDistance <= initialStopDistance + InitialMoveTargetBuffer)
                {
                    entityCooldowns[aiEntity] = Time.time + EntityMoveCooldownDuration;
                }
            }
            else 
        {
                // If on cooldown, skip this entity for this update.
                if (Time.time < entityCooldowns[aiEntity]) 
                {
                    continue; 
                }

                // Check for nearby enemies within weapon range.
                Entity nearestEnemy = FindNearestEnemy(aiEntity, weaponRange);
                if (nearestEnemy != null)
                {
                    // If an enemy is found, stop and clear commands (to allow default attack behavior).
                    unitAIComponent?.StopAndRemoveAllCommands(); 
                    entityCooldowns[aiEntity] = Time.time + EntityMoveCooldownDuration; // Set cooldown.
                    continue; 
        }
        else
        {
                    // No enemy nearby, calculate a new target distance to advance.
                    targetDistance = CalculateDynamicTargetDistance(aiEntity, weaponRange, currentDistance);
                    entityCooldowns[aiEntity] = Time.time + EntityMoveCooldownDuration; // Set cooldown.
                }
            }
            // Issue a move command towards the opponent's base, stopping at the target distance.
            AIMgr.inst.HandleMove(new List<Entity> { aiEntity }, opponentBase.position, false, doneDistanceSq: targetDistance * targetDistance);
        }
    }

    /// <summary>
    /// Handles combat behavior for Level 2 AI.
    /// Level 2 AI units perform an attack-move towards the opponent's base.
    /// </summary>
    /// <param name="aiEntities">The list of AI-controlled entities.</param>
    private void HandleLevel2CombatBehavior(List<Entity> aiEntities)
    {
        if (opponentBase == null) return; 
        AIMgr.inst.HandleAttackMove(aiEntities, opponentBase.position, opponentBase, false,acquireTarget:true);
    }

    private void HandleLevel3CombatBehavior(List<Entity> aiEntities)
    {
        if (opponentBase == null && !(aiBases.Count > 0 && aiEntities.Any(e => e != null))) 
        {
             if (aiBases.Count == 0) return; 
        }


        _reusableDefendersList.Clear();
        _reusableAttackersList.Clear();
        HashSet<Entity> assignedDefenders = new HashSet<Entity>(); 

        Entity primaryAiBase = null;
        if (aiBases.Count > 0)
        {
            primaryAiBase = aiBases[0]; 
        }

        if (primaryAiBase != null && aiEntities.Count > 0)
        {
            var defenderTypeConfigs = new Dictionary<EntityType, float>
            {
                { EntityType.DDG51, 0.1f },    
                { EntityType.JARIUSV, 0.1f },  
                { EntityType.SeaHunter, 0.1f } 
            };

            var entitiesByType = aiEntities
                .Where(e => e != null && e.entityType != default(EntityType)) 
                .GroupBy(e => e.entityType)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var config in defenderTypeConfigs)
            {
                EntityType type = config.Key;
                float percentage = config.Value;

                if (entitiesByType.TryGetValue(type, out List<Entity> unitsOfType))
                {
                    int numToDefend;
                    if (unitsOfType.Count > 0)
                    {
                        int tenPercentOfUnits = Mathf.FloorToInt(unitsOfType.Count * percentage);
                        numToDefend = Mathf.Max(1, tenPercentOfUnits);
                        numToDefend = Mathf.Min(numToDefend, unitsOfType.Count);
                    }
                    else
                    {
                        numToDefend = 0; 
                    }

                    for (int i = 0; i < numToDefend; i++)
                    {
                        Entity candidateDefender = unitsOfType[i];
                        if (candidateDefender != null && !assignedDefenders.Contains(candidateDefender))
                        {
                            _reusableDefendersList.Add(candidateDefender);
                            assignedDefenders.Add(candidateDefender);
                        }
                    }
                }
            }
        }

        foreach (Entity entity in aiEntities)
        {
            if (entity != null && !assignedDefenders.Contains(entity))
            {
                _reusableAttackersList.Add(entity);
            }
        }
        
        if (primaryAiBase != null)
        {
            HandleDefenderLogicLevel3(_reusableDefendersList, primaryAiBase);
        }

        if (opponentBase != null) 
        {
            HandleAttackerLogicLevel3(_reusableAttackersList, opponentBase);
        } 
        else if (_reusableAttackersList.Count > 0 && primaryAiBase == null)
        {
        }
    }


    private void HandleDefenderLogicLevel3(List<Entity> defenders, Entity aiBaseToDefend)
    {
        if (defenders.Count == 0 || aiBaseToDefend == null) return;

        Vector3 basePosition = aiBaseToDefend.position;
        float defenseRadiusSq = BaseDefenseRadiusLevel3 * BaseDefenseRadiusLevel3;

        _reusableThreatsNearBaseList.Clear();
        List<Entity> allEnemyTargets = FindAllEnemyTargets();
        foreach (Entity enemy in allEnemyTargets)
        {
            if (enemy == null) continue;
            if ((enemy.position - basePosition).sqrMagnitude < defenseRadiusSq)
            {
                _reusableThreatsNearBaseList.Add(enemy);
            }
        }

        foreach (Entity defender in defenders)
        {
            if (defender == null) continue;
            UnitAI unitAI = defender.GetComponentInChildren<UnitAI>();

            if (_reusableThreatsNearBaseList.Count > 0)
            {
                Entity targetToAttack = null;
                float minSqrDistToDefender = float.MaxValue;
                foreach (Entity threat in _reusableThreatsNearBaseList)
                {
                    float sqrDist = (threat.position - defender.position).sqrMagnitude;
                    if (sqrDist < minSqrDistToDefender)
                    {
                        minSqrDistToDefender = sqrDist;
                        targetToAttack = threat;
                    }
                }
                
                if (targetToAttack != null)
                {
                    AIMgr.inst.HandleAttackMove(new List<Entity> { defender }, targetToAttack.position, targetToAttack, false);
                }
            }
            else 
            {
                Vector3 directionFromBaseToCenter = (MapCenterLevel3 - basePosition).normalized;
                if (directionFromBaseToCenter == Vector3.zero) 
                {
                    directionFromBaseToCenter = (aiBaseToDefend.transform.forward != Vector3.zero) ? 
                                                 aiBaseToDefend.transform.forward.normalized : Vector3.forward;
                }

                Vector3 defendPosition = basePosition + directionFromBaseToCenter * DefenderOffsetFromBaseLevel3;

                float distanceToDefendPosSq = (defender.position - defendPosition).sqrMagnitude;

                if (distanceToDefendPosSq > DefenderPositionToleranceSqLevel3)
                {
                    AIMgr.inst.HandleMove(new List<Entity> { defender }, defendPosition, false, doneDistanceSq: DefenderPositionToleranceSqLevel3 / 4f);
                }
                else 
                {
                    unitAI?.StopAndRemoveAllCommands(); 

                    if (MapCenterLevel3 != defender.position) 
                    {
                        defender.transform.LookAt(MapCenterLevel3);
                    }
                }
            }
        }
    }

    private void HandleAttackerLogicLevel3(List<Entity> attackers, Entity globalTargetOpponentBase)
    {
        if (attackers.Count == 0) return;
        if(opponentBase == null) return;
        AIMgr.inst.HandleAttackMove(attackers, globalTargetOpponentBase.position, globalTargetOpponentBase, false, acquireTarget: true);
    }
    
    private List<Entity> FindAllEnemyTargets()
    {
        _reusableEnemyTargetsList.Clear();
        foreach (Entity e in EntityMgr.inst.entities)
        {
            if (e == null || e.owner == null || 
                e.owner.name.Equals(AiOwnerName, aiOwnerNameComparison) ||
                e.entityClass == EntityClass.Missile) 
            {
                continue;
            }
            _reusableEnemyTargetsList.Add(e);
        }
        return _reusableEnemyTargetsList;
    }


    private float CalculateDynamicTargetDistance(Entity aiEntity, float weaponRange, float currentDistance)
    {
        float targetDist = initialStopDistance; 

        if (currentDistance > weaponRange && FindNearestEnemy(aiEntity, initialStopDistance) == null)
        {
            targetDist = Mathf.Max(weaponRange, currentDistance - minDistanceReduction); 
        }
        else if (currentDistance <= weaponRange) 
        {
            targetDist = weaponRange;
        }
        return targetDist;
    }

    private Entity FindNearestEnemy(Entity aiEntity, float range)
    {
        Vector3 aiPos = aiEntity.position;
        Entity nearest = null;
        float nearestDistSq = float.MaxValue;

        Collider[] hitColliders = Physics.OverlapSphere(aiPos, range); 

        foreach (Collider hitCollider in hitColliders)
        {
            Entity potentialEnemy = hitCollider.GetComponentInParent<Entity>();

            if (potentialEnemy == null || potentialEnemy.owner == null ||
                potentialEnemy.owner.name.Equals(AiOwnerName, aiOwnerNameComparison) ||
                potentialEnemy.entityClass == EntityClass.Missile ||
                potentialEnemy == aiEntity) 
            {
                continue;
            }

            float distSq = (potentialEnemy.position - aiPos).sqrMagnitude;
            if (distSq < nearestDistSq)
            {
                nearest = potentialEnemy;
                nearestDistSq = distSq;
            }
        }
        //Debug.Log("Nearest enemy found enemy ai : " + (nearest != null ? nearest.name : "None"));
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
            return e; 
        }
        return null; 
    }

    private List<Entity> GetAIEntities()
    {
        _reusableAiEntitiesList.Clear(); 
        foreach (Entity e in EntityMgr.inst.entities)
        {
            if (e == null || e.owner == null ||
                !e.owner.name.Equals(AiOwnerName, aiOwnerNameComparison) ||
                e.entityClass == EntityClass.Missile) 
            {
                continue;
            }
            _reusableAiEntitiesList.Add(e);
        }
        return _reusableAiEntitiesList;
    }
}

