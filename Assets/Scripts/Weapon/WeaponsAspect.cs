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
        float rangeSq = weapon.range * weapon.range;
        Entity bestThreat = null;
        float bestDistanceSq = float.MaxValue;

        bool isAI = entity.owner != null && entity.owner.name.Equals("Ai", System.StringComparison.OrdinalIgnoreCase);
        // Assume GameMgr.inst.priorityList is a List<EntityClass> defining global priority order
        var priorityList = SpawnEntityMgr.inst.priorityList;

        foreach (Entity potential in EntityMgr.inst.entities)
        {
            if (potential == entity ||
                potential.owner == entity.owner ||
                !IsTargetValid(potential))
                continue;

            float distSq = (potential.position - entity.position).sqrMagnitude;
            if (distSq > rangeSq) continue;

            if (isAI)
            {
                // Determine priority index (lower index = higher priority)
                int potIdx = priorityList.IndexOf(potential.entityType);
                if (potIdx < 0) potIdx = int.MaxValue;

                int bestIdx = bestThreat != null
                    ? priorityList.IndexOf(bestThreat.entityType)
                    : int.MaxValue;

                if (bestThreat == null
                    || potIdx < bestIdx
                    || (potIdx == bestIdx && potential.health < bestThreat.health)
                    || (potIdx == bestIdx && potential.health == bestThreat.health && distSq < bestDistanceSq))
                {
                    bestThreat = potential;
                    bestDistanceSq = distSq;
                }
            }
            else
            {
                // Non‐AI: pick closest
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