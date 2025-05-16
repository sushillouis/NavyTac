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
        if(unitAI == null) return;
        if (unitAI.commands.Count > 0 && unitAI.commands.Peek() != null){
            if (unitAI.commands.Peek().GetType() == typeof(Move)||
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
        if (Time.time - weapon.lastShotTime < weapon.cooldown || isAmmoDepleted )
            return;
        Entity target = FindTargetInRange();
        if (target != null)
        {
            //Debug.Log("Target found: " + target.name);
            WeaponsMgr.inst.handleWeapon(entity,target);
        }
    }

    private Entity FindTargetInRange()
    {
        if (entity == null || weapon == null) return null;

        Vector3 currentPosition = entity.position;
        Entity nearestEnemy = null;
        float nearestDistSq = float.MaxValue;
        float currentWeaponRange = weapon.range;

        // Use Physics.OverlapSphere to find colliders within the specified range.
        // Consider adding a LayerMask if entities are on specific layers for optimization.
        Collider[] hitColliders = Physics.OverlapSphere(currentPosition, currentWeaponRange); 

        foreach (Collider hitCollider in hitColliders)
        {
            // Attempt to get the Entity component from the collider's game object or its parent.
            Entity potentialEnemy = hitCollider.GetComponentInParent<Entity>();

            if (potentialEnemy == null || 
                potentialEnemy == entity || // Don't target self
                potentialEnemy.owner == entity.owner || // Don't target own units
                potentialEnemy.entityClass == EntityClass.Missile || // Don't target missiles
                !potentialEnemy.gameObject.activeSelf) 
            {
                continue;
            }

            float distSq = (potentialEnemy.position - currentPosition).sqrMagnitude;
            // OverlapSphere ensures entities are within 'range', but we still need the *closest* one.
            if (distSq < nearestDistSq)
            {
                nearestEnemy = potentialEnemy;
                nearestDistSq = distSq;
            }
        }

        return nearestEnemy;
    }
}