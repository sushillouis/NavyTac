using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.WSA;

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
                unitAI.commands.Peek().GetType() == typeof(Follow) ||
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
            Debug.Log("Target found: " + target.name);
            WeaponsMgr.inst.handleWeapon(entity,target);
        }
    }

    private Entity FindTargetInRange()
    {
        // Debug.Log("Finding target in range...");
        if (EntityMgr.inst == null || EntityMgr.inst.entities == null)
            return null;

        float weaponRange = weapon.range; // Fixed: Use weapon.range instead of undefined 'range'
        float weaponRangeSq = weaponRange * weaponRange;
        Entity nearest = null;
        float minDistSq = float.MaxValue;

        foreach (Entity e in EntityMgr.inst.entities)
        {
            if (e == null || e == entity || e.owner == entity.owner || !e.gameObject.activeSelf || e.entityClass == EntityClass.Missile)
                continue;

            float distSq = (e.position - entity.position).sqrMagnitude;
            if (distSq < weaponRangeSq && distSq < minDistSq)
            {
                minDistSq = distSq;
                nearest = e;
            }
        }
        
        return nearest;
    }
}

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
