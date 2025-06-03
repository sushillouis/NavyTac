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
    Entity nearest = null;
    float nearestDistSq = float.MaxValue;

    Collider[] hitColliders = Physics.OverlapSphere(entityPos, weapon.range);

    foreach (Collider hitCollider in hitColliders)
    {
        Entity potentialEnemy = hitCollider.GetComponentInParent<Entity>();

        if (potentialEnemy == null || potentialEnemy.owner == null ||
            potentialEnemy.owner == entity.owner || // Check if the owner is the same
            potentialEnemy.entityClass == EntityClass.Missile ||
            potentialEnemy == entity)
        {
            continue;
        }
        
        if (!IsTargetValid(potentialEnemy)) // Use existing IsTargetValid for additional checks
        {
            continue;
        }

        float distSq = (potentialEnemy.position - entityPos).sqrMagnitude;
        if (distSq < nearestDistSq)
        {
            nearest = potentialEnemy;
            nearestDistSq = distSq;
        }
    }
    return nearest;
}
    
    public bool IsTargetValid(Entity target)
    {
        return target != null &&
               target.isVisible &&
               target.gameObject.activeSelf &&
               target.entityClass != EntityClass.Missile;
    }

}