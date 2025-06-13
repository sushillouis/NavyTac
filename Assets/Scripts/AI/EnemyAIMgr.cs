using System.Diagnostics;
using System.Collections.Generic;
using System.Linq; 
using UnityEngine;
using System.Collections;

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

        if (OpenOceanMain.inst.currentTrainingState == TrainingState.Tutorial)
        {
            return; // Skip AI processing in tutorial mode
        }
        if (Time.time - lastUpdateTime < updateInterval) return;
        lastUpdateTime = Time.time;

        CheckAndLogDestroyedAIBases(); 
        FindAIBases(); 

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
            case 0 :
                //Debug.LogWarning("AI level 0 is not implemented.");
                break;
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

    float diff = GameMgr.inst.difficultyLevel ;

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
            ? weaponAspect.weapon.range -100f
            : adaptiveWeaponRangeFallback;

            float weaponRange = baseRange;
        
        // Initialize cooldown if not already done
        if (!entityCooldowns.ContainsKey(aiEntity))
        {
            entityCooldowns[aiEntity] = FirstMoveSentinel;
        }

        float currentDistance = Vector3.Distance(aiEntity.position, opponentPos);
        float targetDistance;

        bool isInitialMovePhase = entityCooldowns[aiEntity] == FirstMoveSentinel;

        if (isInitialMovePhase)
        {
            targetDistance = adaptiveInitialStopDistance;

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

            float weaponRange = DefaultWeaponRangeFallback; // Default value
            if (weaponAspect != null && weaponAspect.weapon != null)
            {
                weaponRange = weaponAspect.weapon.range;
            }

            if (!entityCooldowns.ContainsKey(aiEntity))
            {
                entityCooldowns[aiEntity] = FirstMoveSentinel;
            }

            float currentDistance = Vector3.Distance(aiEntity.position, opponentPos);
            // targetDistance variable was unused in this path for HandleMove, so it's removed.

            bool isInitialMovePhase = entityCooldowns[aiEntity] == FirstMoveSentinel;

            if (isInitialMovePhase)
            {
                // The logic here is to determine when to transition out of the initial phase.
                if (currentDistance <= initialStopDistance - InitialMoveTargetBuffer)
                {
                    entityCooldowns[aiEntity] = Time.time + EntityMoveCooldownDuration;
                }
            }
            else
            {
                if (Time.time < entityCooldowns[aiEntity]) // Check cooldown
                {
                    continue; // Still on cooldown
                }

                // Cooldown passed, check for enemies
                Entity nearestEnemy = FindNearestEnemy(aiEntity, weaponRange);
                if (nearestEnemy != null) // Enemy found
                {
                    if (unitAIComponent != null) // Explicit null check
                    {
                        unitAIComponent.StopAndRemoveAllCommands();
                    }
                    entityCooldowns[aiEntity] = Time.time + EntityMoveCooldownDuration; // Set cooldown
                    continue; // Do not issue a move command to base, engage/stop for enemy
                }
                else // No enemy found
                {
                    // Set cooldown for the next check. Entity will continue moving towards base via HandleMove below.
                    entityCooldowns[aiEntity] = Time.time + EntityMoveCooldownDuration;
                }
            }
            // If not continued (due to cooldown or finding an enemy), move towards the opponent base.
            AIMgr.inst.HandleMove(new List<Entity> { aiEntity }, opponentBase.position, false);
        }
    }
private bool _level2CommandsStarted = false;
private float BatchDelay = 45f;
private Vector3 _aiBasePosition;
private List<Entity> _batch1 = new List<Entity>();
private List<Entity> _batch2 = new List<Entity>();
private Coroutine _level2Coroutine;

public void ResetLevel2State()
{
    _level2CommandsStarted = false;
    _aiBasePosition = Vector3.zero;

    _batch1.Clear();
    _batch2.Clear();

    if (_level2Coroutine != null)
    {
        StopCoroutine(_level2Coroutine);
        _level2Coroutine = null;
    }
}

private void HandleLevel2CombatBehavior(List<Entity> aiEntities)
{
    if (opponentBase == null) return;
    if (aiEntities.Count == 0) return;
    
    if (!_level2CommandsStarted && aiBases.Count > 0)
    {
        _aiBasePosition = aiBases[0].position; 
        _level2CommandsStarted = true;
        
        var validTypes = new HashSet<EntityType> 
        { 
            EntityType.DDG51, 
            EntityType.JARIUSV, 
            EntityType.SeaHunter 
        };
        
        var filteredEntities = aiEntities
            .Where(e => e != null && validTypes.Contains(e.entityType))
            .ToList();
            
        CreateBatches(filteredEntities);
        
        if (OpenOceanMain.inst.currentTrainingState == TrainingState.Adaptive)
        {
            float diff = GameMgr.inst.difficultyLevel; 
            BatchDelay = Mathf.Lerp(60f, 20f, (diff - 0.33f) / (0.66f - 0.33f));
        }

        if (_level2Coroutine != null) 
        {
            StopCoroutine(_level2Coroutine);
        }
       
        _level2Coroutine = StartCoroutine(RunLevel2CommandSequence()); 
    }
}

private void CreateBatches(List<Entity> allEntities)
{
    _batch1.Clear();
    _batch2.Clear();

    // Split each entity type into 2 batches
    foreach (EntityType type in new[] { EntityType.DDG51, EntityType.JARIUSV, EntityType.SeaHunter })
    {
        var entitiesOfType = allEntities.Where(e => e.entityType == type).ToList();
        int half = Mathf.CeilToInt(entitiesOfType.Count / 2f);

        _batch1.AddRange(entitiesOfType.Take(half));
        _batch2.AddRange(entitiesOfType.Skip(half));
    }
}



    private IEnumerator RunLevel2CommandSequence()
    {
        IssueDirectCommand(_batch1, GetStagingPosition(1000f), true, false);  
        yield return new WaitForSeconds(BatchDelay);
        IssueDirectCommand(_batch1, opponentBase.position, true);  
        IssueDirectCommand(_batch2, GetStagingPosition(1000f), true, false);  
       

        // Phase 2: T=45 seconds
        yield return new WaitForSeconds(BatchDelay*2);
        IssueDirectCommand(_batch2, opponentBase.position, true);  
    }

private Vector3 GetStagingPosition(float position)
{
    // Fallback position if base position isn't set
    if (_aiBasePosition == Vector3.zero)
    {
        return new Vector3(position, 0f, position);
    }
    
    Vector3 stagingDir = (Vector3.zero - _aiBasePosition).normalized;
    Vector3 stagingPos = _aiBasePosition + stagingDir * position;
    
    
    
    return stagingPos ;
}

private Vector3 GetStagingPosition2()
{
    // Fallback position if opponent base isn't set
    if (opponentBase == null)
    {
        return new Vector3(4000f, 0f, 4000f);
    }
    
    Vector3 opponentPos = opponentBase.position;
    
    // Calculate direction from opponent base to center
    Vector3 centerDir = (Vector3.zero - opponentPos).normalized;
    
    // Position 4000 units from opponent base toward center
    Vector3 stagingPos = opponentPos + centerDir * 4000f;



        return stagingPos;
}

private void IssueDirectCommand(List<Entity> entities, Vector3 position, bool isAttackMove, bool towardOpponentBase = true)
{
    if (entities.Count == 0) return;

    if (isAttackMove)
    {
        if (towardOpponentBase && opponentBase != null)
        {
            AIMgr.inst.HandleAttackMove(entities, position, opponentBase, false, acquireTarget: true);
        }
        else
        {
            // If not attacking the opponent base, or opponent base is null, attack move to position
            foreach (Entity entity in entities)
            {
                if (entity == null) continue;

                // Add jitter to the target position for each individual unit
                float minAbsJitter = 0f; 
                float maxAbsJitter = 500f;

                float randomMagnitudeX = Random.Range(minAbsJitter, maxAbsJitter);
                float offsetX = (Random.value < 0.5f) ? -randomMagnitudeX : randomMagnitudeX;

                float randomMagnitudeZ = Random.Range(minAbsJitter, maxAbsJitter);
                float offsetZ = (Random.value < 0.5f) ? -randomMagnitudeZ : randomMagnitudeZ;
                        
                Vector3 jitteredPosition = position + new Vector3(offsetX, 0, offsetZ);
                        
                AIMgr.inst.HandleAttackMove(new List<Entity> { entity }, jitteredPosition, null, false, acquireTarget: true);
            }
        }
    }
    else // This is a move command, likely to a staging position
    {
        foreach (Entity entity in entities)
        {
            if (entity == null) continue;

            // Add jitter to the staging position for each individual unit
            float minAbsJitter = 0f;
            float maxAbsJitter = 500f;

            float randomMagnitudeX = Random.Range(minAbsJitter, maxAbsJitter);
            float offsetX = (Random.value < 0.5f) ? -randomMagnitudeX : randomMagnitudeX;

            float randomMagnitudeZ = Random.Range(minAbsJitter, maxAbsJitter);
            float offsetZ = (Random.value < 0.5f) ? -randomMagnitudeZ : randomMagnitudeZ;
            
            Vector3 jitteredPosition = position + new Vector3(offsetX, 0, offsetZ);
            
            AIMgr.inst.HandleMove(new List<Entity> { entity }, jitteredPosition, false);
        }
    }
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

