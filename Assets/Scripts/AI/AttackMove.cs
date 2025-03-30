using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class AttackMove : Move
{
    public AttackMove(Entity ent, Vector3 pos, bool maxSpeedMovement = false) : base(ent, pos, maxSpeedMovement)
    {
    }

    public override void Init() 
    {
        // Initialize lines as in base class, if needed
        base.Init();
    }

    public override void Tick() 
    {
        // Move toward the destination
        base.Tick();

        // Find and attack the nearest enemy
        Entity nearestEnemy = FindNearestEnemy();
        if (nearestEnemy != null)
        {
            WeaponsMgr.inst.handleWeapon(entity, nearestEnemy);
        }
    }

    // Helper method to find the nearest enemy within weapon range
    private Entity FindNearestEnemy()
    {
        WeaponsAspect weaponAspect = entity.GetComponentInChildren<WeaponsAspect>();
        if (weaponAspect == null || weaponAspect.weapon == null)
            return null;

        float weaponRange = weaponAspect.weapon.range;
        float weaponRangeSq = weaponRange * weaponRange;
        Entity nearest = null;
        float minDistSq = float.MaxValue;

        foreach (Entity e in EntityMgr.inst.entities)
        {
            if (e == entity || e.owner == entity.owner || !e.gameObject.activeSelf)
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

    

    public override void Stop()
    {
        entity.desiredSpeed = 0;
        entity.desiredHeading = entity.heading;
        LineMgr.inst.DestroyLR(line);
        LineMgr.inst.DestroyLR(potentialLine);
        line = null;
        potentialLine = null;
    }
}
