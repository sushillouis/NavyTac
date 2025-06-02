using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WeaponData
{
    public Transform launchPoint; 
    public float cooldown;
    public WeaponBehaviors behaviorType;
    public EntityType weaponEntityType;
    public float range = 200f;
    public List<Entity> currentWeaponEntities; // List of active weapons
    public float defaultDamage;
    public float ammoCount;
    [NonSerialized]
    public float lastShotTime;
}

public class WeaponsAspect : MonoBehaviour
{

    public Entity entity;
    public WeaponData weapon;

    private void Awake()
    {
        entity = GetComponentInParent<Entity>();
        if (entity == null) return;

        entity.weapons = this;
        weapon.currentWeaponEntities = new List<Entity>();
        weapon.lastShotTime = -weapon.cooldown;
    }

    private void Update()
    {
        if (entity == null || weapon == null) return;
        UnitAI unitAI = entity.GetComponentInChildren<UnitAI>();
        if (unitAI == null) return;
        if (unitAI.commands.Count > 0 && unitAI.commands.Peek() != null) {
            if (unitAI.commands.Peek().GetType() == typeof(Move) ||
                unitAI.commands.Peek().GetType() == typeof(AttackMove) ||
                // unitAI.commands.Peek().GetType() == typeof(Follow) ||
                unitAI.commands.Peek().GetType() == typeof(Intercept) ||
                unitAI.commands.Peek().GetType() == typeof(Intercept3d) ||
                unitAI.commands.Peek().GetType() == typeof(SmartIntercept))
            {
                return;
            }
        }

        bool isAmmoDepleted = (weapon.ammoCount != -1 && weapon.ammoCount <= 0);
        if (Time.time - weapon.lastShotTime < weapon.cooldown || isAmmoDepleted)
            return;
        Entity target = FindImmediateThreatInRange();
        if (target != null)
        {
            //Debug.Log("Target found: " + target.name);
            WeaponsMgr.inst.handleWeapon(entity, target);
        }
    }

   public Entity FindImmediateThreatInRange()
{
    Vector3 entityPos = entity.position;
    float rangeSq = weapon.range * weapon.range;
    Entity bestThreat = null;
    float bestDistanceSq = float.MaxValue;

    bool isAI = entity.owner != null && entity.owner.name.Equals("Ai", StringComparison.OrdinalIgnoreCase);
    
    // For AI: Precompute priority dictionary
    Dictionary<EntityType, int> priorityDict = null;
    if (isAI)
    {
        priorityDict = new Dictionary<EntityType, int>();
        var priorityList = SpawnEntityMgr.inst.priorityList;
        for (int i = 0; i < priorityList.Count; i++)
        {
            priorityDict[priorityList[i]] = i;
        }
    }

    // For AI: Track best priority/health state
    int bestPriorityIndex = int.MaxValue;
    float bestHealth = float.MaxValue;

    foreach (Entity potential in EntityMgr.inst.entities)
    {
        // Fast rejection checks
        if (potential == entity || 
            potential.owner == entity.owner || 
            !IsTargetValid(potential)) 
            continue;

        // Distance check
        float distSq = (potential.position - entityPos).sqrMagnitude;
        if (distSq > rangeSq) continue;

        if (isAI)
        {
            // Get priority (default to MaxValue if not found)
            int currentPriority = priorityDict.TryGetValue(potential.entityType, out int idx) 
                ? idx 
                : int.MaxValue;
            
            // Selection logic with priority order:
            // 1. Higher priority (lower index)
            // 2. Lower health
            // 3. Closer distance
            if (bestThreat == null)
            {
                UpdateBest();
            }
            else if (currentPriority < bestPriorityIndex)
            {
                UpdateBest();
            }
            else if (currentPriority == bestPriorityIndex)
            {
                if (potential.health < bestHealth)
                {
                    UpdateBest();
                }
                else if (potential.health == bestHealth && distSq < bestDistanceSq)
                {
                    UpdateBest();
                }
            }

            void UpdateBest()
            {
                bestThreat = potential;
                bestDistanceSq = distSq;
                bestPriorityIndex = currentPriority;
                bestHealth = potential.health;
            }
        }
        else // Non-AI logic
        {
            if (distSq < bestDistanceSq)
            {
                bestThreat = potential;
                bestDistanceSq = distSq;
            }
        }
    }

    return bestThreat;
}
    
    public bool IsTargetValid(Entity target)
    {
        return target != null &&
               target.isVisible &&
               target.gameObject.activeSelf &&
               target.entityClass != EntityClass.Missile;
    }

}