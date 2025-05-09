using UnityEngine;

[System.Serializable]
public class AttackMove : Move
{
    private Entity explicitTarget;
    private bool hasExplicitTarget;
    private float basePathUpdateCooldown;
    private Vector3 lastKnownTargetPosition; // Store the last known position of the explicit target

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
        // Initialize lastKnownTargetPosition with the target's initial position
        if (target != null) 
        {
            lastKnownTargetPosition = target.position;
        }
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
        if (hasExplicitTarget)
        {
            if (IsTargetValid(explicitTarget))
            {
                movePosition = explicitTarget.position;
                lastKnownTargetPosition = explicitTarget.position; // Update last known position
                
                // Increase update frequency when target is moving
                pathUpdateCooldown = explicitTarget.speed > 1f ? 0.2f : basePathUpdateCooldown;
            }
            else
            {
                // Target is invalid (e.g., destroyed), move to its last known position
                movePosition = lastKnownTargetPosition;
                pathUpdateCooldown = basePathUpdateCooldown; // Revert to base cooldown for static point
            }
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
            // If hasExplicitTarget and target became invalid, movePosition is now lastKnownTargetPosition.
            // base.Tick() will pathfind towards movePosition.
            base.Tick(); 
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
            line.SetPosition(1, movePosition); // movePosition will be target or lastKnownTargetPosition
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
            movePosition = target.position; // Ensure movePosition is the engagement target
            base.Tick();
        }
    }

    public override bool IsDone()
    {
        if (hasExplicitTarget)
        {
            if (IsTargetValid(explicitTarget))
            {
                // Target is still valid, check distance to target
                return (entity.position - explicitTarget.position).sqrMagnitude < doneDistanceSq;
            }
            else
            {
                // Target is invalid, check distance to last known position
                return (entity.position - lastKnownTargetPosition).sqrMagnitude < doneDistanceSq;
            }
        }
        return base.IsDone(); // Handles AttackMove to a position, or if explicit target part is done
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
            if (IsTargetValid(explicitTarget))
            {
                movePosition = explicitTarget.position;
            }
            else
            {
                // Target is invalid, use last known position for stopping
                movePosition = lastKnownTargetPosition;
            }
        }
        // base.Stop() will use the updated movePosition
        base.Stop();
    }
}