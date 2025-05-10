using UnityEngine;

/// <summary>
/// Represents an attack-move command for an entity.
/// The entity will move towards a target position or entity,
/// engaging any enemies it encounters along the way or near its destination.
/// </summary>
[System.Serializable]
public class AttackMove : Move
{
    private Entity explicitTarget;          // The specific entity to target, if any.
    private bool hasExplicitTarget;         // Flag indicating if an explicit target was given.
    private float basePathUpdateCooldown;   // Base cooldown for path updates, can be adjusted dynamically.
    private Vector3 lastKnownTargetPosition; // Stores the last known position of the explicit target if it becomes invalid.
    private WeaponsAspect _weaponsAspect;   // Cached WeaponsAspect component.

    // Constants for path update logic
    private const float DefaultPathUpdateCooldown = 0.3f; // For combat scenarios or static targets.
    private const float MovingTargetPathUpdateCooldown = 0.2f; // For actively moving targets.
    private const float TargetMovingSpeedThreshold = 1.0f; // Speed above which a target is considered "moving".

    /// <summary>
    /// Constructor for an attack-move to a specific position.
    /// </summary>
    /// <param name="ent">The entity performing the move.</param>
    /// <param name="pos">The target position.</param>
    /// <param name="maxSpeed">Whether the entity should move at maximum speed.</param>
    public AttackMove(Entity ent, Vector3 pos, bool maxSpeed = false) : base(ent, pos, maxSpeed)
    {
        hasExplicitTarget = false;
        pathUpdateCooldown = DefaultPathUpdateCooldown;
        basePathUpdateCooldown = pathUpdateCooldown;
    }

    /// <summary>
    /// Constructor for an attack-move towards a specific target entity.
    /// </summary>
    /// <param name="ent">The entity performing the move.</param>
    /// <param name="target">The target entity.</param>
    /// <param name="maxSpeed">Whether the entity should move at maximum speed.</param>
    public AttackMove(Entity ent, Entity target, bool maxSpeed = false) : base(ent, target.position, maxSpeed)
    {
        explicitTarget = target;
        hasExplicitTarget = true;
        lastKnownTargetPosition = target.position;
        pathUpdateCooldown = MovingTargetPathUpdateCooldown; 
        basePathUpdateCooldown = pathUpdateCooldown;
    }

    /// <summary>
    /// Initializes the attack move, setting up pathfinding, line renderers, and caching components.
    /// </summary>
    public override void Init()
    {
        base.Init(); 
        _weaponsAspect = entity.GetComponentInChildren<WeaponsAspect>();

        if (!FogWarMgr.inst.nonRevelers.Contains(entity))
        {
            line = LineMgr.inst.CreateAttackMoveLine(entity.position, movePosition);
            line.gameObject.SetActive(false); 
        }
    }

    /// <summary>
    /// Called every frame to update the attack-move logic.
    /// Handles target tracking, engagement, and movement.
    /// </summary>
    public override void Tick()
    {
        // 1. Update primary movePosition and pathUpdateCooldown based on explicit target (if any)
        if (hasExplicitTarget)
        {
            if (IsTargetValid(explicitTarget))
            {
                movePosition = explicitTarget.position;
                lastKnownTargetPosition = explicitTarget.position;
                pathUpdateCooldown = explicitTarget.speed > TargetMovingSpeedThreshold ? MovingTargetPathUpdateCooldown : basePathUpdateCooldown;
            }
            else
            {
                movePosition = lastKnownTargetPosition; // Move to last known position
                pathUpdateCooldown = basePathUpdateCooldown; // Revert to base cooldown
            }
        }

        // 2. Determine target for engagement
        Entity currentEngagementTarget = null;
        bool canEngage = _weaponsAspect != null && _weaponsAspect.weapon != null;

        if (canEngage)
        {
            // Prioritize explicit target if in range
            if (hasExplicitTarget && IsTargetValid(explicitTarget))
            {
                float rangeSq = _weaponsAspect.weapon.range * _weaponsAspect.weapon.range;
                if ((explicitTarget.position - entity.position).sqrMagnitude <= rangeSq)
                {
                    currentEngagementTarget = explicitTarget;
                }
            }

            // If no explicit target in range (or no explicit target), look for other threats
            if (currentEngagementTarget == null)
            {
                currentEngagementTarget = FindImmediateThreatInRange(); // This already checks weapon range and validity
            }
        }

        // 3. Act based on engagement
        if (currentEngagementTarget != null)
        {
            AimAndFireAtTarget(currentEngagementTarget); // Aim and fire

            // Once in range (currentEngagementTarget is not null), stop moving to engage.
            entity.desiredSpeed = 0; 
            // The entity will remain stationary and fire as long as currentEngagementTarget is valid and in range.
            // If the target moves out of range or is destroyed, currentEngagementTarget will become null in the next Tick,
            // and the entity will resume movement based on the 'else' block below.
        }
        else // Not engaged with any target (either no valid targets in range, or weapon system issue)
        {
            // Continue with original move command (or move to explicit target's last known position).
            // The pathUpdateCooldown is set in section 1 if hasExplicitTarget,
            // or defaults if no explicit target. This ensures appropriate pathfinding frequency.
            base.Tick(); 
        }

        UpdateAttackLineRenderer();
    }

    /// <summary>
    /// Updates the line renderer for the attack move command.
    /// </summary>
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

    /// <summary>
    /// Finds the closest valid enemy target *within weapon range*.
    /// </summary>
    /// <returns>The closest engageable entity, or null if none are in range.</returns>
    private Entity FindImmediateThreatInRange()
    {
        if (_weaponsAspect == null || _weaponsAspect.weapon == null) return null;

        float rangeSq = _weaponsAspect.weapon.range * _weaponsAspect.weapon.range;
        Entity closestThreat = null;
        float minDistanceSq = float.MaxValue;

        foreach (Entity potentialTarget in EntityMgr.inst.entities)
        {
            if (potentialTarget == entity || potentialTarget.owner == entity.owner || !IsTargetValid(potentialTarget)) continue;

            float distanceSq = (potentialTarget.position - entity.position).sqrMagnitude;
            if (distanceSq <= rangeSq && distanceSq < minDistanceSq) // Ensure it's within range
            {
                minDistanceSq = distanceSq;
                closestThreat = potentialTarget;
            }
        }
        return closestThreat;
    }

    /// <summary>
    /// Handles aiming at and firing upon a specific target.
    /// Assumes the target is valid.
    /// </summary>
    /// <param name="target">The entity to engage.</param>
    private void AimAndFireAtTarget(Entity target)
    {
        // _weaponsAspect and weapon null checks are done before calling this or by FindImmediateThreatInRange
        // but an extra check for target is good.
        if (target == null || _weaponsAspect == null || _weaponsAspect.weapon == null) return;

        Vector3 directionToTarget = target.position - entity.position;
        entity.desiredHeading = Mathf.Atan2(directionToTarget.x, directionToTarget.z) * Mathf.Rad2Deg;
        WeaponsMgr.inst.handleWeapon(entity, target);
    }

    /// <summary>
    /// Checks if the attack-move command is completed.
    /// Relies on the base class's IsDone logic, using the continuously updated movePosition.
    /// </summary>
    /// <returns>True if the command is done, false otherwise.</returns>
    public override bool IsDone()
    {
        return base.IsDone(); 
    }

    /// <summary>
    /// Checks if a target entity is valid for engagement.
    /// </summary>
    /// <param name="target">The entity to check.</param>
    /// <returns>True if the target is valid, false otherwise.</returns>
    private bool IsTargetValid(Entity target)
    {
        return target != null && 
               target.transform.GetChild(0).gameObject.activeSelf && 
               target.gameObject.activeSelf && 
               target.entityClass != EntityClass.Missile; 
    }

    /// <summary>
    /// Stops the attack-move command. Updates movePosition based on target status.
    /// </summary>
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
                movePosition = lastKnownTargetPosition;
            }
        }
        base.Stop();
    }
}
