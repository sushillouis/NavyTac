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

    public override void Tick()
    {
        Entity immediateTarget = FindImmediateThreat();
        bool shouldEngage = immediateTarget != null;

        if (hasExplicitTarget)
        {
            HandleExplicitTargetBehavior(ref immediateTarget, ref shouldEngage);
        }

        if (shouldEngage)
        {
            HandleEngagement(immediateTarget);
        }
        else
        {
            base.Tick();
        }
    }

    private void HandleExplicitTargetBehavior(ref Entity immediateTarget, ref bool shouldEngage)
    {
        if (!IsTargetValid(explicitTarget))
        {
            hasExplicitTarget = false;
            return;
        }

        WeaponsAspect weapons = entity.GetComponentInChildren<WeaponsAspect>();
        if (weapons == null || weapons.weapon == null)
        {
            hasExplicitTarget = false;
            return;
        }

        // Use weapon range for pursuit calculations
        float weaponRangeSq = weapons.weapon.range * weapons.weapon.range;
        float targetDistSq = (explicitTarget.position - entity.position).sqrMagnitude;

        // Update move position to track moving target
        movePosition = explicitTarget.position;

        if (targetDistSq <= weaponRangeSq)
        {
            // Prioritize explicit target when in weapon range
            immediateTarget = explicitTarget;
            shouldEngage = true;
        }
        else
        {
            // Allow engaging other threats while pursuing main target
            shouldEngage = immediateTarget != null;
        }
    }

    private void HandleEngagement(Entity target)
    {
        // Stop movement when in attack range
        entity.desiredSpeed = 0;
        
        // Face target
        Vector3 toTarget = target.position - entity.position;
        entity.desiredHeading = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
    }

    private Entity FindImmediateThreat()
    {
        WeaponsAspect weapons = entity.GetComponentInChildren<WeaponsAspect>();
        if (weapons == null || weapons.weapon == null) return null;

        float rangeSq = weapons.weapon.range * weapons.weapon.range;
        Entity nearest = null;
        float minDistSq = float.MaxValue;

        foreach (Entity e in EntityMgr.inst.entities)
        {
            if (e == entity || e.owner == entity.owner || !IsTargetValid(e)) 
                continue;

            float distSq = (e.position - entity.position).sqrMagnitude;
            if (distSq < rangeSq && distSq < minDistSq)
            {
                minDistSq = distSq;
                nearest = e;
            }
        }
        return nearest;
    }

    private bool IsTargetValid(Entity target)
    {
        return target != null && 
               target.transform.GetChild(0).gameObject.activeSelf &&
               target.gameObject.activeSelf;
    }

    public override bool IsDone()
    {
        if (hasExplicitTarget)
        {
            return !IsTargetValid(explicitTarget);
        }
        return base.IsDone();
    }
}