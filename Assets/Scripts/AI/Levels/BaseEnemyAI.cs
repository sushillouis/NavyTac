using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class BaseEnemyAI
{
    protected Entity opponentBase;
    protected List<Entity> neutralBases = new List<Entity>();
    protected List<Entity> aiEntitiesList = new List<Entity>();
    protected List<Entity> playerEntitiesList = new List<Entity>();
    protected List<Entity> neutralEntitiesList = new List<Entity>();
    protected List<Entity> aiBases = new List<Entity>();
    protected Dictionary<Entity, float> entityCooldowns = new Dictionary<Entity, float>();

    protected const string AiOwnerName = "Ai";
    protected readonly System.StringComparison aiOwnerNameComparison = System.StringComparison.OrdinalIgnoreCase;

    protected List<Entity> _reusableEnemyTargetsList = new List<Entity>();
    private HashSet<Entity> _activeEntitiesSetCache = new HashSet<Entity>();
    private List<Entity> _keysToRemoveCache = new List<Entity>();

    public virtual void Init(Entity opponentBase, List<Entity> aiBases, List<Entity> playerEntitiesList , List<Entity> neutralBases = null, 
        List<Entity> aiEntitiesList = null, List<Entity> neutralEntitiesList = null)
    {
        this.opponentBase = opponentBase;
        this.aiBases = aiBases;
        this.playerEntitiesList = playerEntitiesList;
        this.neutralBases = neutralBases;
        this.aiEntitiesList = aiEntitiesList;
        this.neutralEntitiesList = neutralEntitiesList;
    }

    public abstract void ProcessCombatBehavior(List<Entity> aiEntities);

    public virtual void ResetState()
    {
        entityCooldowns.Clear();
    }

    public virtual void SetEntityCooldowns(Dictionary<Entity, float> cooldowns)
    {
        entityCooldowns = cooldowns;
    }

    public virtual void PruneEntityCooldowns(List<Entity> activeAiEntities)
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

        foreach (Entity key in _keysToRemoveCache)
        {
            entityCooldowns.Remove(key);
        }
    }

    protected List<Entity> FindAllEnemyTargets()
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

    protected Entity FindNearestEnemy(Entity aiEntity, float range)
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
        return nearest;
    }
}
