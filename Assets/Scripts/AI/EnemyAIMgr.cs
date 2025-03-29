using System.Collections.Generic;
using UnityEngine;

public class EnemyAIMgr : MonoBehaviour
{
    public int currentLevel = 1;
    public static EnemyAIMgr inst;

    private Entity opponentBase; // Cached opponent base
    private Dictionary<Entity, float> entityCooldowns = new Dictionary<Entity, float>();
    private float updateInterval = 0.5f;
    private float lastUpdateTime;

    void Awake() 
    {
        inst = this;
        opponentBase = FindOpponentBase(); // Initial search
    }

    private void Update()
    {
        if (Time.time - lastUpdateTime < updateInterval) return;
        lastUpdateTime = Time.time;

        if (currentLevel == 1)
            HandleLevel1Behavior();
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
            Debug.Log("AI WON");
            return;
        }

        List<Entity> aiEntities = GetAIEntities();
        if (aiEntities.Count == 0) return;

        HandleCombatBehavior(aiEntities);
    }

    private void HandleCombatBehavior(List<Entity> aiEntities)
    {
        Vector3 opponentPos = opponentBase.position;

        for (int i = aiEntities.Count - 1; i >= 0; i--)
        {
            Entity aiEntity = aiEntities[i];
            if (aiEntity == null) continue;

            if (entityCooldowns.TryGetValue(aiEntity, out float cooldown) && cooldown > Time.time)
                continue;

            float range = aiEntity.GetComponentInChildren<WeaponsAspect>()?.weapon.range ?? 600f;
            Entity nearestEnemy = FindNearestEnemy(aiEntity, range);

            if (nearestEnemy != null)
            {
                UnitAI unitAI = aiEntity.GetComponentInChildren<UnitAI>();
                unitAI?.StopAndRemoveAllCommands();
                WeaponsMgr.inst.handleWeapon(aiEntity, nearestEnemy);
                entityCooldowns[aiEntity] = Time.time + 0.2f;
            }
            else
            {
                AIMgr.inst.HandleMove(new List<Entity> { aiEntity }, opponentPos, false);
                entityCooldowns[aiEntity] = Time.time + 0.5f;
            }
        }
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