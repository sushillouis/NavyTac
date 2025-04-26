using UnityEngine;

[System.Serializable]
public class AttackMove : Move
{
    private Entity explicitTarget;
    private bool hasExplicitTarget;
    private float basePathUpdateCooldown;

    public AttackMove(Entity ent, Vector3 pos, bool maxSpeed = false) : base(ent, pos, maxSpeed)
    {
        hasExplicitTarget = false;
        pathUpdateCooldown = 0.3f; // Faster updates for combat
        basePathUpdateCooldown = pathUpdateCooldown;
    }

    public AttackMove(Entity ent, Entity target, bool maxSpeed = false) : base(ent, target.position, maxSpeed)
    {
        explicitTarget = target;
        hasExplicitTarget = true;
        pathUpdateCooldown = 0.2f; // Most frequent updates for moving targets
        basePathUpdateCooldown = pathUpdateCooldown;
    }

    public override void Init()
    {
        base.Init(); // Initialize base pathfinding
        if (!FogWarMgr.inst.nonRevelers.Contains(entity))
        {
            line = LineMgr.inst.CreateAttackMoveLine(entity.position, movePosition);
            line.gameObject.SetActive(false);
        }
    }

    public override void Tick()
    {
        // Update target position for moving targets
        if (hasExplicitTarget && explicitTarget != null)
        {
            movePosition = explicitTarget.position;
            
            // Increase update frequency when target is moving
            pathUpdateCooldown = explicitTarget.speed > 1f ? 0.2f : basePathUpdateCooldown;
        }

        // Check for immediate threats
        WeaponsAspect weapons = entity.GetComponentInChildren<WeaponsAspect>();
        Entity immediateTarget = FindImmediateThreat(weapons);
        
        if (immediateTarget != null)
        {
            HandleEngagement(immediateTarget, weapons);
        }
        else
        {
            base.Tick(); // Proceed with normal pathfinding
        }

        UpdateAttackLineRenderer();
    }

    private void UpdateAttackLineRenderer()
    {
        if (line != null && !FogWarMgr.inst.nonRevelers.Contains(entity))
        {
            line.gameObject.SetActive(entity.isSelected);
            line.positionCount = 2;
            line.SetPosition(0, entity.position);
            line.SetPosition(1, movePosition);
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

    private void HandleEngagement(Entity target, WeaponsAspect weapons)
    {
        if (weapons == null || target == null) return;

        float rangeSq = weapons.weapon.range * weapons.weapon.range;
        float distSq = (target.position - entity.position).sqrMagnitude;

        if (distSq <= rangeSq)
        {
            // Engage target
            entity.desiredSpeed = 0;
            Vector3 direction = target.position - entity.position;
            entity.desiredHeading = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            WeaponsMgr.inst.handleWeapon(entity,target);
        }
        else
        {
            // Approach target using pathfinding
            movePosition = target.position;
            base.Tick();
        }
    }

    public override bool IsDone()
    {
        if (hasExplicitTarget)
        {
            return !IsTargetValid(explicitTarget) || 
                   (entity.position - explicitTarget.position).sqrMagnitude < doneDistanceSq;
        }
        return base.IsDone();
    }

    private bool IsTargetValid(Entity target)
    {
        return target != null && 
               target.transform.GetChild(0).gameObject.activeSelf &&
               target.gameObject.activeSelf && 
               target.entityClass != EntityClass.Missile;
    }

    public override void Stop()
    {
        if (hasExplicitTarget)
        {
            movePosition = explicitTarget.position;
            base.Stop();
        }
        else
        {
            base.Stop();
        }
    }
}