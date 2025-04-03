using System.Collections.Generic;
using UnityEngine;

public class EnemyAIMgr : MonoBehaviour
{
    public int currentLevel = 1;
    public static EnemyAIMgr inst;

    private Entity opponentBase; // Cached opponent base
    public List<Entity> aiBases = new List<Entity>();
    private Dictionary<Entity, float> entityCooldowns = new Dictionary<Entity, float>();
    private float updateInterval = 0.5f;
    private float lastUpdateTime;
    private float initialStopDistance = 2000f; // Initial stopping distance
    private float minDistanceReduction = 200f; // Reduce distance by this amount each step

    void Awake() 
    {
        
        inst = this;
        opponentBase = FindOpponentBase(); // Initial search
    }

    private void Update()
    {
        if (Time.time - lastUpdateTime < updateInterval) return;
        lastUpdateTime = Time.time;
        FindAIBases();
        CheckAIBases();
        if (aiBases.Count == 0) {
            Debug.Log("No AI bases found, reloading scene.");
            // GameMgr.inst.ReloadScene(); // No AI bases found
            return;
        }
         // No AI bases found
        if (currentLevel == 1)
            HandleLevel1Behavior();
        if (currentLevel == 2)
            HandleLevel2Behavior();
    }
     private void CheckAIBases()
    {
        for (int i = aiBases.Count - 1; i >= 0; i--)
        {
            Entity baseEntity = aiBases[i];
            if (baseEntity == null)
            {
                Debug.Log("AI base destroyed");
                aiBases.RemoveAt(i);
            }
        }
    }

    // New method to find all AI bases
    private void FindAIBases()
    {
        aiBases.Clear();
        foreach (Entity e in EntityMgr.inst.entities)
        {
            if (e != null && e.owner != null && 
                e.owner.name.Equals("Ai", System.StringComparison.OrdinalIgnoreCase) && 
                e.entityRole == EntityRole.Base)
            {
                aiBases.Add(e);
            }
        }
    }
    private void HandleLevel1Behavior()
    {
        // Check if cached base is still valid
        if (opponentBase == null || !EntityMgr.inst.entities.Contains(opponentBase))
        {
            opponentBase = FindOpponentBase();
        }

        if (opponentBase == null)
        {
            // GameMgr.inst.ReloadScene();
            Debug.Log("AI WON");
             // No opponent base found, reload scene
            return;
        }

        List<Entity> aiEntities = GetAIEntities();
        if (aiEntities.Count == 0) return;

        HandleCombatBehavior(aiEntities);
    }
    private void HandleLevel2Behavior()
    {
        // Check if cached base is still valid
        if (opponentBase == null || !EntityMgr.inst.entities.Contains(opponentBase))
        {
            opponentBase = FindOpponentBase();
        }

        if (opponentBase == null)
        {
            Debug.Log("AI WON");
            return;
        }

        List<Entity> aiEntities = GetAIEntities();
        if (aiEntities.Count == 0) return;

        Handlelevel2CombatBehavior(aiEntities);
    }
    private void Handlelevel2CombatBehavior(List<Entity> aiEntities)
    {
        AIMgr.inst.HandleAttackMove(aiEntities, opponentBase.position, opponentBase, false);
    }
    private void HandleCombatBehavior(List<Entity> aiEntities)
{
    Vector3 opponentPos = opponentBase.position;

    for (int i = aiEntities.Count - 1; i >= 0; i--)
    {
        Entity aiEntity = aiEntities[i];
        if (aiEntity == null) continue;

        WeaponsAspect weaponAspect = aiEntity.GetComponentInChildren<WeaponsAspect>();
        float weaponRange = weaponAspect != null ? weaponAspect.weapon.range : 600f;

        // Initialize cooldown if not present (use -1 to indicate "first move" state)
        if (!entityCooldowns.ContainsKey(aiEntity))
        {
            entityCooldowns[aiEntity] = -1f;
        }

        float currentDistance = Vector3.Distance(aiEntity.position, opponentPos);
        float targetDistance;

        // Check if this is the first move (hasn't reached 2000 units yet)
        if (entityCooldowns[aiEntity] < 0f)
        {
            targetDistance = initialStopDistance; // Force first stop at 2000 units

            // If close enough to 2000 units, mark as having completed first stop
            if (currentDistance <= initialStopDistance + 50f) // Adding small buffer
            {
                entityCooldowns[aiEntity] = Time.time + 0.5f; // Normal cooldown starts
            }
        }
        else
        {
            // After first stop, normal behavior
            Entity nearestEnemy = FindNearestEnemy(aiEntity, weaponRange);
            if (nearestEnemy != null)
            {
                // Engage the enemy
                UnitAI unitAI = aiEntity.GetComponentInChildren<UnitAI>();
                unitAI?.StopAndRemoveAllCommands();
                continue; // Skip movement if engaging enemy
            }
            else
            {
                // No enemies in range, calculate dynamic distance
                targetDistance = weaponAspect != null ? CalculateTargetDistance(aiEntity, weaponRange, currentDistance) : 600f;
                entityCooldowns[aiEntity] = Time.time + 0.5f;
            }
        }

        // Move towards the target distance
        AIMgr.inst.HandleMove(new List<Entity> { aiEntity }, opponentBase.position, false, doneDistanceSq: targetDistance * targetDistance);
    }
}

    private float CalculateTargetDistance(Entity aiEntity, float weaponRange, float currentDistance)
    {
        // Start with initial stop distance of 2000 units
        float targetDistance = initialStopDistance;

        // If no enemies are nearby and we're beyond weapon range, gradually reduce the distance
        if (currentDistance > weaponRange && FindNearestEnemy(aiEntity, initialStopDistance) == null)
        {
            targetDistance = Mathf.Max(weaponRange, currentDistance - minDistanceReduction);
        }
        // Once within weapon range, stop reducing
        else if (currentDistance <= weaponRange)
        {
            targetDistance = weaponRange;
        }

        return targetDistance;
    }

    private Entity FindNearestEnemy(Entity aiEntity, float range)
    {
        Vector3 aiPos = aiEntity.position;
        float sqrRange = range * range;
        Entity nearest = null;
        float nearestDist = float.MaxValue;

        foreach (Entity e in EntityMgr.inst.entities)
        {
            if (e == null || e.owner == null || e.entityClass == EntityClass.Missile) continue;
            if (e.owner.name.Equals("Ai", System.StringComparison.OrdinalIgnoreCase)) continue;

            float dist = (e.position - aiPos).sqrMagnitude;
            if (dist < sqrRange && dist < nearestDist)
            {
                nearest = e;
                nearestDist = dist;
            }
        }
        return nearest;
    }

    private Entity FindOpponentBase()
    {
        foreach (Entity e in EntityMgr.inst.entities)
        {
            if (e != null && e.owner != null && 
                !e.owner.name.Equals("Ai", System.StringComparison.OrdinalIgnoreCase) && 
                e.entityRole == EntityRole.Base)
                return e;
        }
        return null;
    }

    private List<Entity> GetAIEntities()
    {
        List<Entity> result = new List<Entity>(EntityMgr.inst.entities.Count / 2);
        foreach (Entity e in EntityMgr.inst.entities)
        {
            if (e != null && e.owner != null && 
                e.owner.name.Equals("Ai", System.StringComparison.OrdinalIgnoreCase) && 
                e.entityClass != EntityClass.Missile)
                result.Add(e);
        }
        return result;
    }
    
}