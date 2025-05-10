using System.Collections.Generic;
using System.Linq; // Added for LINQ usage in PruneEntityCooldowns and Level 3 AI
using UnityEngine;

// Assuming EntityType enum exists and contains DDG51, JariusV, Seahunter
// e.g., public enum EntityType { Unknown, Base, Fighter, Bomber, DDG51, JariusV, Seahunter, /* etc. */ }

/// <summary>
/// Manages the behavior of AI-controlled entities in the game.
/// Handles different AI levels, target selection, and movement.
/// </summary>
public class EnemyAIMgr : MonoBehaviour
{
    public int currentLevel = 1; // The current difficulty level of the AI.
    public static EnemyAIMgr inst; // Singleton instance of the EnemyAIMgr.

    private Entity opponentBase; // Cached reference to the opponent's base.
    public List<Entity> aiBases = new List<Entity>(); // List of AI-controlled bases.
    private Dictionary<Entity, float> entityCooldowns = new Dictionary<Entity, float>(); // Cooldown timers for AI entity actions.

    // Constants for configuration and magic numbers
    private const string AiOwnerName = "Ai"; // The name identifier for AI-owned entities.
    private const float DefaultUpdateInterval = 0.5f; // Default interval for AI updates.
    private const float DefaultInitialStopDistance = 2000f; // Default initial distance AI units stop from the target.
    private const float DefaultMinDistanceReduction = 200f; // Default minimum distance AI units reduce when advancing.
    private const float DefaultWeaponRangeFallback = 600f; // Fallback weapon range if not determinable.
    private const float EntityMoveCooldownDuration = 0.5f; // Cooldown duration after an entity moves.
    private const float InitialMoveTargetBuffer = 50f; // Buffer distance for reaching the initial stop distance.
    private const float FirstMoveSentinel = -1f; // Sentinel value indicating an entity's initial move phase.

    // Level 3 AI Constants
    // private const int NumDefendersLevel3 = 2; // No longer used, defender count is dynamic
    private const float BaseDefenseRadiusLevel3 = 1200f; // Radius around the AI base defenders will engage enemies.
    private const float DefenderHoldDistanceLevel3 = 150f; // How close defenders try to stay to their base when idle (original behavior, now adapted)
    private const float DefenderOffsetFromBaseLevel3 = 250f; // Distance in front of the base for defenders to position themselves.
    private static readonly Vector3 MapCenterLevel3 = Vector3.zero; // Assumed center of the map for defenders to face.
    private const float DefenderPositionToleranceSqLevel3 = 25f; // Squared tolerance (5 units) for reaching defend position.


    private readonly float updateInterval = DefaultUpdateInterval; // Current AI update interval.
    private float lastUpdateTime; // Time of the last AI update.
    private readonly float initialStopDistance = DefaultInitialStopDistance; // Current initial stop distance for AI units.
    private readonly float minDistanceReduction = DefaultMinDistanceReduction; // Current minimum distance reduction for AI units.
    private readonly System.StringComparison aiOwnerNameComparison = System.StringComparison.OrdinalIgnoreCase; // String comparison type for AI owner name.

    // Reusable collections for optimization to reduce garbage collection
    private List<Entity> _reusableAiEntitiesList = new List<Entity>();
    private List<Entity> _reusableEnemyTargetsList = new List<Entity>();
    private List<Entity> _reusableThreatsNearBaseList = new List<Entity>();
    private List<Entity> _reusableDefendersList = new List<Entity>();
    private List<Entity> _reusableAttackersList = new List<Entity>();
    private List<Entity> _keysToRemoveCache = new List<Entity>();
    private HashSet<Entity> _activeEntitiesSetCache = new HashSet<Entity>();


    /// <summary>
    /// Called when the script instance is being loaded.
    /// Implements the Singleton pattern.
    /// </summary>
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

    /// <summary>
    /// Called every frame.
    /// Manages the main AI logic update cycle based on the update interval.
    /// </summary>
    private void Update()
    {
        // Throttle AI updates to the specified interval.
        if (Time.time - lastUpdateTime < updateInterval) return;
        lastUpdateTime = Time.time;

        CheckAndLogDestroyedAIBases(); // Check for and remove destroyed AI bases.
        FindAIBases(); // Update the list of AI bases.

        if (aiBases.Count == 0)
        {
            if (currentLevel == 3)
            {
                // For Level 3, if no AI bases, they might all become attackers or follow a fallback.
                // Proceeding, HandleLevel3CombatBehavior will check aiBases.Count.
            }
            else // Not Level 3 and no AI bases
            {
                HandleNoAIBases(); // Handle the scenario where no AI bases are found.
                return;
            }
        }

        UpdateOpponentBaseCache(); // Update the cached reference to the opponent's base.
        if (opponentBase == null)
        {
            HandleNoOpponentBase(); // Handle the scenario where no opponent base is found.
            return;
        }

        List<Entity> aiEntities = GetAIEntities(); // Get all active AI-controlled entities (uses reusable list).
        if (aiEntities.Count == 0)
        {
            return; // No AI entities to manage.
        }
        
        PruneEntityCooldowns(aiEntities); // Remove cooldown entries for destroyed or inactive entities.
        ProcessLevelBehavior(aiEntities); // Process AI behavior based on the current level.
    }

    /// <summary>
    /// Removes cooldown entries for entities that are no longer active or have been destroyed.
    /// Uses a cached HashSet and List to reduce allocations.
    /// </summary>
    /// <param name="activeAiEntities">A list of currently active AI entities.</param>
    private void PruneEntityCooldowns(List<Entity> activeAiEntities)
    {
        _activeEntitiesSetCache.Clear();
        foreach (Entity entity in activeAiEntities)
        {
            // Ensure not to add null entities if activeAiEntities might contain them,
            // though GetAIEntities should filter them out.
            if (entity != null) 
            {
                _activeEntitiesSetCache.Add(entity);
            }
        }

        _keysToRemoveCache.Clear();
        // Iterate over a copy of keys or use a different approach if modifying during iteration
        // Here, we iterate the dictionary and add keys to a separate list for removal.
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

    /// <summary>
    /// Checks for AI bases that have been destroyed and removes them from the list.
    /// Logs a message when a base is found destroyed.
    /// </summary>
    private void CheckAndLogDestroyedAIBases()
    {
        // Iterate backwards to safely remove items from the list.
        for (int i = aiBases.Count - 1; i >= 0; i--)
        {
            Entity baseEntity = aiBases[i];
            if (baseEntity == null) 
            {
                Debug.Log("AI base previously identified was found destroyed.");
                aiBases.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Finds all entities that are AI-controlled bases and updates the aiBases list.
    /// </summary>
    private void FindAIBases()
    {
        aiBases.Clear(); // Clear the existing list before repopulating.
        foreach (Entity e in EntityMgr.inst.entities)
        {
            // Filter for entities that are AI-owned and have the Base role.
            if (e == null || e.owner == null ||
                !e.owner.name.Equals(AiOwnerName, aiOwnerNameComparison) ||
                e.entityRole != EntityRole.Base)
            {
                continue;
            }
            aiBases.Add(e);
        }
    }
    
    /// <summary>
    /// Handles the scenario where no AI bases are found.
    /// This might trigger game events like AI loss or scene reload.
    /// </summary>
    private void HandleNoAIBases()
    {
        Debug.Log("No AI bases found. This might trigger a game event (e.g., AI loss or scene reload).");
        // GameMgr.inst.ReloadScene(); // Example action: Reload the current scene.
    }

    /// <summary>
    /// Updates the cached reference to the opponent's base if it's null or no longer exists.
    /// </summary>
    private void UpdateOpponentBaseCache()
    {
        if (opponentBase == null || !EntityMgr.inst.entities.Contains(opponentBase))
        {
            opponentBase = FindOpponentBase(); // Attempt to find the opponent's base.
        }
    }

    /// <summary>
    /// Handles the scenario where no opponent base is found.
    /// This typically means the AI has won.
    /// </summary>
    private void HandleNoOpponentBase()
    {
        ScoreMgr.inst.aiWon = true; // Set the AI won flag in the ScoreManager.
        ScoreMgr.inst.CheckVictory(); // Trigger the victory check.
    }

    /// <summary>
    /// Directs AI behavior based on the current AI level.
    /// </summary>
    /// <param name="aiEntities">The list of AI-controlled entities.</param>
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
                Debug.LogWarning($"Unhandled AI level: {currentLevel}");
                break;
        }
    }

    /// <summary>
    /// Handles combat behavior for Level 1 AI.
    /// Level 1 AI units move towards the opponent's base, stopping at a calculated distance.
    /// They will stop and engage if an enemy is nearby, otherwise they advance.
    /// </summary>
    /// <param name="aiEntities">The list of AI-controlled entities.</param>
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
                    entityCooldowns[aiEntity] = Time.time + EntityMoveCooldownDuration; // Set cooldown for next action.
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
        if (opponentBase == null) return; // Requires an opponent base to target.
        // Issue an attack-move command for all AI entities towards the opponent's base.
        AIMgr.inst.HandleAttackMove(aiEntities, opponentBase.position, opponentBase, false);
    }

    /// <summary>
    /// Handles combat behavior for Level 3 AI.
    /// Divides units into defenders (specific types, 10% of each) and attackers.
    /// Defenders protect the AI's primary base, while attackers target the opponent's base or high-priority enemy units.
    /// </summary>
    /// <param name="aiEntities">The list of AI-controlled entities.</param>
    private void HandleLevel3CombatBehavior(List<Entity> aiEntities)
    {
        if (opponentBase == null && !(aiBases.Count > 0 && aiEntities.Any(e => e != null))) // Opponent base is crucial for attackers, unless only defending.
        {
             // If no opponent base and no AI bases to defend or no units, can't do much.
             // Original code had 'return' if opponentBase is null. We might need it for defense if AI base exists.
             if (aiBases.Count == 0) return; // No AI base to defend, and no opponent base to attack.
        }


        _reusableDefendersList.Clear();
        _reusableAttackersList.Clear();
        HashSet<Entity> assignedDefenders = new HashSet<Entity>(); // Keep track of entities assigned as defenders

        Entity primaryAiBase = null;
        if (aiBases.Count > 0)
        {
            primaryAiBase = aiBases[0]; // Defend the first AI base in the list.
        }

        if (primaryAiBase != null && aiEntities.Count > 0)
        {
            // Define specific types for defense and their desired percentage
            // IMPORTANT: Ensure these EntityType enum values (DDG51, JariusV, Seahunter) exist in your EntityType enum.
            var defenderTypeConfigs = new Dictionary<EntityType, float>
            {
                { EntityType.DDG51, 0.1f },    // Assuming EntityType.DDG51 exists
                { EntityType.JARIUSV, 0.1f },  // Assuming EntityType.JariusV exists
                { EntityType.SeaHunter, 0.1f } // Assuming EntityType.Seahunter exists
            };

            // Group entities by type
            var entitiesByType = aiEntities
                .Where(e => e != null && e.entityType != default(EntityType)) // Filter out nulls and default/unknown types
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
                        // Calculate 10% of the units of this type, floored.
                        int tenPercentOfUnits = Mathf.FloorToInt(unitsOfType.Count * percentage);
                        // Number of defenders is the maximum of 1 or 10% (floored).
                        numToDefend = Mathf.Max(1, tenPercentOfUnits);
                        // Ensure we don't try to assign more defenders than available units.
                        numToDefend = Mathf.Min(numToDefend, unitsOfType.Count);
                    }
                    else
                    {
                        numToDefend = 0; // No units of this type, so no defenders.
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

        // Assign remaining entities (not selected as specific defenders) as attackers
        // Also, if no primary AI base, all units are attackers
        foreach (Entity entity in aiEntities)
        {
            if (entity != null && !assignedDefenders.Contains(entity))
            {
                _reusableAttackersList.Add(entity);
            }
        }
        
        // If an AI base exists, manage defender logic.
        if (primaryAiBase != null)
        {
            HandleDefenderLogicLevel3(_reusableDefendersList, primaryAiBase);
        }

        // Manage attacker logic (even if no primary AI base, attackers might have global objectives)
        if (opponentBase != null) // Attackers need an opponent base to target as a fallback
        {
            HandleAttackerLogicLevel3(_reusableAttackersList, opponentBase);
        } 
        else if (_reusableAttackersList.Count > 0 && primaryAiBase == null)
        {
            // No opponent base and no AI base to defend, attackers might roam or have other objectives.
            // For now, they do nothing if opponentBase is null. This matches original logic flow.
            // Consider a fallback behavior for attackers if opponentBase is null.
        }
    }


    /// <summary>
    /// Manages the behavior of Level 3 AI defenders.
    /// Defenders engage enemies near their assigned base. If no threats, they position themselves
    /// in front of the base, facing the map center.
    /// </summary>
    /// <param name="defenders">The list of AI entities assigned as defenders.</param>
    /// <param name="aiBaseToDefend">The AI base that defenders should protect.</param>
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
            else // No immediate threats near the base, position and orient the defender.
            {
                Vector3 directionFromBaseToCenter = (MapCenterLevel3 - basePosition).normalized;
                if (directionFromBaseToCenter == Vector3.zero) // Base is at map center or very close
                {
                    // Default direction: use base's forward, or world forward if base's forward is zero.
                    directionFromBaseToCenter = (aiBaseToDefend.transform.forward != Vector3.zero) ? 
                                                 aiBaseToDefend.transform.forward.normalized : Vector3.forward;
                }

                Vector3 defendPosition = basePosition + directionFromBaseToCenter * DefenderOffsetFromBaseLevel3;

                float distanceToDefendPosSq = (defender.position - defendPosition).sqrMagnitude;

                if (distanceToDefendPosSq > DefenderPositionToleranceSqLevel3)
                {
                    // Move to the defend position
                    AIMgr.inst.HandleMove(new List<Entity> { defender }, defendPosition, false, doneDistanceSq: DefenderPositionToleranceSqLevel3 / 4f);
                }
                else // In position, now orient
                {
                    unitAI?.StopAndRemoveAllCommands(); // Stop any current movement/activity to allow manual orientation

                    // Make the defender face the map center
                    if (MapCenterLevel3 != defender.position) // Avoid LookAt(self)
                    {
                        defender.transform.LookAt(MapCenterLevel3);
                    }
                    // Unit will hold this position and orientation until a threat appears or new orders are given.
                }
            }
        }
    }

    /// <summary>
    /// Manages the behavior of Level 3 AI attackers.
    /// Attackers prioritize targets based on a priority list, health, and distance.
    /// If no suitable targets are found, they move towards the opponent's base.
    /// </summary>
    /// <param name="attackers">The list of AI entities assigned as attackers.</param>
    /// <param name="globalTargetOpponentBase">The opponent's base, serving as a global target.</param>
    private void HandleAttackerLogicLevel3(List<Entity> attackers, Entity globalTargetOpponentBase)
    {
        if (attackers.Count == 0) return;
        // If globalTargetOpponentBase is null here, attackers won't have a fallback move target.
        // This is consistent with original logic where opponentBase was checked before calling.

        List<Entity> allEnemyTargets = FindAllEnemyTargets(); // Get all potential enemy targets (uses reusable list).

        foreach (Entity attacker in attackers)
        {
            if (attacker == null) continue;

            if (allEnemyTargets.Count == 0)
            {
                // No enemies found anywhere, move towards the opponent's base if it exists.
                if (globalTargetOpponentBase != null)
                {
                    AIMgr.inst.HandleMove(new List<Entity> { attacker }, globalTargetOpponentBase.position, false);
                }
                continue;
            }

            // Sort targets: 1. Priority (desc), 2. Health (asc), 3. Distance to attacker (asc).
            Entity bestTarget = allEnemyTargets
                .Where(t => t != null) 
                .OrderByDescending(t => GetTargetPriority(t)) 
                .ThenBy(t => t.health) 
                .ThenBy(t => (t.position - attacker.position).sqrMagnitude) 
                .FirstOrDefault();

            if (bestTarget != null)
            {
                AIMgr.inst.HandleAttackMove(new List<Entity> { attacker }, bestTarget.position, bestTarget, false);
            }
            else if (globalTargetOpponentBase != null) 
            {
                 AIMgr.inst.HandleMove(new List<Entity> { attacker }, globalTargetOpponentBase.position, false);
            }
        }
    }

    /// <summary>
    /// Helper method to determine the priority of a target entity.
    /// Priority is based on the entity's type as defined in GameMgr's priorityList.
    /// Higher return value means higher priority.
    /// </summary>
    /// <param name="target">The entity to evaluate.</param>
    /// <returns>An integer representing the target's priority.</returns>
    private int GetTargetPriority(Entity target)
    {
        if (target == null || GameMgr.inst == null || GameMgr.inst.priorityList == null) return 0; 

        EntityType targetType = target.entityType; 

        int index = GameMgr.inst.priorityList.IndexOf(targetType);

        if (index != -1)
        {
            return GameMgr.inst.priorityList.Count - index;
        }

        return 1; 
    }
    
    /// <summary>
    /// Finds all enemy entities in the game, excluding missiles and AI's own units.
    /// Uses a reusable list to reduce allocations.
    /// </summary>
    /// <returns>A reference to the reusable list of enemy entities.</returns>
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


    /// <summary>
    /// Calculates the dynamic target distance for Level 1 AI units.
    /// This helps units advance towards the enemy or maintain a safe distance.
    /// </summary>
    /// <param name="aiEntity">The AI entity.</param>
    /// <param name="weaponRange">The weapon range of the AI entity.</param>
    /// <param name="currentDistance">The current distance from the AI entity to its target.</param>
    /// <returns>The calculated target distance.</returns>
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

    /// <summary>
    /// Finds the nearest enemy entity within a specified range of an AI entity.
    /// </summary>
    /// <param name="aiEntity">The AI entity searching for enemies.</param>
    /// <param name="range">The search range.</param>
    /// <returns>The nearest enemy entity, or null if no enemy is found within range.</returns>
    private Entity FindNearestEnemy(Entity aiEntity, float range)
    {
        Vector3 aiPos = aiEntity.position;
        float sqrRange = range * range; 
        Entity nearest = null;
        float nearestDistSq = float.MaxValue;

        foreach (Entity e in EntityMgr.inst.entities)
        {
            if (e == null || e.owner == null || e.entityClass == EntityClass.Missile ||
                e.owner.name.Equals(AiOwnerName, aiOwnerNameComparison) || e == aiEntity) 
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

    /// <summary>
    /// Finds the opponent's base entity.
    /// </summary>
    /// <returns>The opponent's base entity, or null if not found.</returns>
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

    /// <summary>
    /// Gets a list of all AI-controlled entities, excluding missiles.
    /// Uses a reusable list to reduce allocations.
    /// </summary>
    /// <returns>A reference to the reusable list of AI entities.</returns>
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
