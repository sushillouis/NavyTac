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
    if (unitAI != null && unitAI.commands.Count > 0 && unitAI.commands.Peek() != null)
    {
        var t = unitAI.commands.Peek().GetType();
        if (t == typeof(Move) || t == typeof(AttackMove) || t == typeof(Follow) ||
            t == typeof(Intercept) || t == typeof(Intercept3d) || t == typeof(SmartIntercept))
        {
            return;
        }
    }

    if (OpenOceanMain.inst.currentTrainingState == TrainingState.Tutorial &&
        entity.owner == PlayerMgr.inst.player2)
        return;

    Entity target = FindImmediateThreatInRange();

    if (target != null && entity.isNeutral == false)
    {
        // Maintain state EVERY frame
        SetAttackLinks(entity, target);

        bool isAmmoDepleted = (weapon.ammoCount != -1 && weapon.ammoCount <= 0);
        bool canFireNow = !isAmmoDepleted && (Time.time - weapon.lastShotTime >= weapon.cooldown);

        if (canFireNow)
        {
            WeaponsMgr.inst.handleWeapon(entity, target);
        }
    }
    else
    {
        ClearAttackLinks(entity);
    }
}

private void SetAttackLinks(Entity attacker, Entity target)
{
    Entity prev = attacker.attackingTarget;

    attacker.isAttacking = true;
    attacker.attackingTarget = target;

    if (prev != null && prev != target && prev.beingAttackedBy == attacker)
    {
        prev.isBeingAttacked = false;
        prev.beingAttackedBy = null;
    }

    target.beingAttackedBy = attacker;
    target.isBeingAttacked = true;
}

private void ClearAttackLinks(Entity attacker)
{
    if (attacker.attackingTarget != null && attacker.attackingTarget.beingAttackedBy == attacker)
    {
        attacker.attackingTarget.isBeingAttacked = false;
        attacker.attackingTarget.beingAttackedBy = null;
    }

    attacker.isAttacking = false;
    attacker.attackingTarget = null;
}


private Entity lastTarget;
private float lastTargetTime;
public float targetStickinessDuration = 2f;

public Entity FindImmediateThreatInRange()
    {
         if (lastTarget != null && Time.time - lastTargetTime < targetStickinessDuration && IsTargetValid(lastTarget))
    {
        return lastTarget;
    }

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
        if (nearest != null)
    {
        lastTarget = nearest;
        lastTargetTime = Time.time;
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