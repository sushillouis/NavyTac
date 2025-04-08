using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AttackMove : Move
{
    private Entity explicitTarget;
    private bool hasExplicitTarget;

    public AttackMove(Entity ent, Vector3 pos, bool maxSpeed = false) : base(ent, pos, maxSpeed)
    {
        hasExplicitTarget = false;
    }

    public AttackMove(Entity ent, Entity target, bool maxSpeed = false) : base(ent, target.position, maxSpeed)
    {
        explicitTarget = target;
        hasExplicitTarget = true;
    }
    public override void Init() 
    {
        if(!FogWarMgr.inst.nonRevelers.Contains(entity) ) 
        {
            line = LineMgr.inst.CreateAttackMoveLine(entity.position, movePosition);
            line.gameObject.SetActive(false);
            potentialLine = LineMgr.inst.CreatePotentialLine(entity.position);
            if (potentialLine != null)
                potentialLine.gameObject.SetActive(false);
        }
        
    }

    public override void Tick()
    {
        bool shouldEngage = false;
        Entity immediateTarget = null;
        WeaponsAspect weapons = entity.GetComponentInChildren<WeaponsAspect>();

        // Priority 1: Explicit Target (always takes precedence)
        if (hasExplicitTarget)
        {
            HandleExplicitTarget(weapons, ref immediateTarget, ref shouldEngage);
        }

        // Priority 2: Immediate Threats (only when no valid explicit target)
        if (!shouldEngage && weapons != null)
        {
            immediateTarget = FindImmediateThreat(weapons);
            shouldEngage = immediateTarget != null;
        }

        if (shouldEngage)
        {
            HandleEngagement(immediateTarget, weapons);
        }
        else
        {
            base.Tick();
        }
    }

    private void HandleExplicitTarget(WeaponsAspect weapons, ref Entity target, ref bool shouldEngage)
    {
        if (!IsTargetValid(explicitTarget) || weapons == null || weapons.weapon == null)
        {
            hasExplicitTarget = false;
            return;
        }

        // Continuously update destination to track moving targets
        movePosition = explicitTarget.position;
        target = explicitTarget;
        shouldEngage = true;

        float rangeSq = weapons.weapon.range * weapons.weapon.range;
        float distSq = (explicitTarget.position - entity.position).sqrMagnitude;

        if (distSq <= rangeSq)
        {
            // Maintain position and face target
            entity.desiredSpeed = 0;
            Vector3 direction = explicitTarget.position - entity.position;
            entity.desiredHeading = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            WeaponsMgr.inst.handleWeapon(entity, explicitTarget);
        }
        else
        {
            // Continue moving toward explicit target
            base.Tick();
        }
    }

    private void HandleEngagement(Entity target, WeaponsAspect weapons)
    {
        if (weapons == null) return;

        float rangeSq = weapons.weapon.range * weapons.weapon.range;
        float distSq = (target.position - entity.position).sqrMagnitude;

        if (distSq <= rangeSq)
        {
            entity.desiredSpeed = 0;
            Vector3 direction = target.position - entity.position;
            entity.desiredHeading = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;  
            WeaponsMgr.inst.handleWeapon(entity,target);
            }
        else
        {
            base.Tick();
        }
    }

    private Entity FindImmediateThreat(WeaponsAspect weapons)
    {
        if (weapons == null || weapons.weapon == null) return null;

        float rangeSq = weapons.weapon.range * weapons.weapon.range;
        Entity closest = null;
        float minDistSq = float.MaxValue;

        foreach (Entity e in EntityMgr.inst.entities)
        {
            if (e == entity || e.owner == entity.owner || !IsTargetValid(e)) continue;

            float distSq = (e.position - entity.position).sqrMagnitude;
            if (distSq < rangeSq && distSq < minDistSq)
            {
                minDistSq = distSq;
                closest = e;
            }
        }
        return closest;
    }

    private bool IsTargetValid(Entity target)
    {
        return target != null && 
               target.transform.GetChild(0).gameObject.activeSelf &&
               target.gameObject.activeSelf && 
               target.entityClass != EntityClass.Missile ;
    }

    public override bool IsDone()
    {
        if (hasExplicitTarget) return !IsTargetValid(explicitTarget);
        return base.IsDone();
    }
}