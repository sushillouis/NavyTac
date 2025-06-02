using UnityEngine;

[System.Serializable]
public class AttackMove : Move
{
    private Entity explicitTarget;
    private bool hasExplicitTarget; // True if currently pursuing a specific target
    private Entity commandedTarget; // The original entity target provided in the constructor
    private Vector3 lastKnownCommandedTargetPosition; // Last known position of the commandedTarget
    private bool isAcquiredTarget; // True if explicitTarget is an acquired target along the way
    private bool acquireTargetsOnWay; // True if unit should acquire targets along the path

    // Fields for original command type
    private Vector3 originalDestinationForAttackMove; // Stores the original destination for an attack-move to a point
    private bool wasOriginallyAttackMoveToPosition;   // True if the command was an attack-move to a point

    private float basePathUpdateCooldown;
    private Vector3 lastKnownTargetPosition; // Last known position of an 'explicitTarget'
    private WeaponsAspect _weaponsAspect;

    private const float DefaultPathUpdateCooldown = 0.3f;
    private const float MovingTargetPathUpdateCooldown = 0.2f;
    private const float TargetMovingSpeedThreshold = 1.0f;
    

    // Constructor for attack-move to a position
    public AttackMove(Entity ent, Vector3 pos, bool maxSpeed = false, float doneDistanceSq = 100000f) : base(ent, pos, maxSpeed, doneDistanceSq)
    {
        _weaponsAspect = entity.GetComponentInChildren<WeaponsAspect>();
        if (_weaponsAspect == null)
        {
            Debug.LogError("WeaponsAspect not found on entity: " + ent.name);
            return;
        }
        hasExplicitTarget = false;
        explicitTarget = null;
        commandedTarget = null;
        isAcquiredTarget = false;
        acquireTargetsOnWay = false; // Default for position constructor
        pathUpdateCooldown = DefaultPathUpdateCooldown;
        basePathUpdateCooldown = DefaultPathUpdateCooldown;
        this.originalDestinationForAttackMove = pos;
        this.wasOriginallyAttackMoveToPosition = true;
        lastKnownTargetPosition = pos;
        lastKnownCommandedTargetPosition = pos;
    }

    // Constructor for attacking a specific entity
    public AttackMove(Entity ent, Entity target, bool acquireTargetsOnWay = false, bool maxSpeed = false, float doneDistanceSq = 100000f) : base(ent, target.position, maxSpeed, doneDistanceSq)
    {
        _weaponsAspect = entity.GetComponentInChildren<WeaponsAspect>();
        if (_weaponsAspect == null)
        {
            Debug.LogError("WeaponsAspect not found on entity: " + ent.name);
            return;
        }
        explicitTarget = target;
        hasExplicitTarget = true;
        commandedTarget = target;
        isAcquiredTarget = false;
        this.acquireTargetsOnWay = acquireTargetsOnWay;
        lastKnownTargetPosition = target != null ? target.position : ent.position;
        lastKnownCommandedTargetPosition = target != null ? target.position : ent.position;

        if (target != null && _weaponsAspect.IsTargetValid(target))
        {
            pathUpdateCooldown = target.speed > TargetMovingSpeedThreshold ? MovingTargetPathUpdateCooldown : DefaultPathUpdateCooldown;
        }
        else
        {
            pathUpdateCooldown = DefaultPathUpdateCooldown;
        }
        basePathUpdateCooldown = DefaultPathUpdateCooldown;
        this.wasOriginallyAttackMoveToPosition = false;
    }

    public override void Init()
    {
        base.Init();
        

        if (!FogWarMgr.inst.nonRevelers.Contains(entity))
        {
            line = LineMgr.inst.CreateAttackMoveLine(entity.position, movePosition);
            line.gameObject.SetActive(false);
        }
    }

    public override void Tick()
    {
        // Update last known positions
        if (commandedTarget != null && _weaponsAspect.IsTargetValid(commandedTarget))
        {
            lastKnownCommandedTargetPosition = commandedTarget.position;
        }
        if (hasExplicitTarget && explicitTarget != null && _weaponsAspect.IsTargetValid(explicitTarget))
        {
            lastKnownTargetPosition = explicitTarget.position;
        }

        // Handle explicitTarget becoming invalid
        if (hasExplicitTarget && (explicitTarget == null || !_weaponsAspect.IsTargetValid(explicitTarget)))
        {
            if (isAcquiredTarget)
            {
                // Acquired target destroyed, revert to commandedTarget
                if (commandedTarget != null && _weaponsAspect.IsTargetValid(commandedTarget))
                {
                    explicitTarget = commandedTarget;
                    isAcquiredTarget = false;
                }
                else
                {
                    hasExplicitTarget = false;
                    movePosition = lastKnownCommandedTargetPosition;
                }
            }
            else
            {
                // Commanded target destroyed, move to its LKP
                hasExplicitTarget = false;
                movePosition = lastKnownTargetPosition;
            }
        }

        // Check for threats if acquireTargetsOnWay is enabled
        if (acquireTargetsOnWay)
        {
            Entity threat = _weaponsAspect.FindImmediateThreatInRange();
            if (threat != null && threat != explicitTarget)
            {
                explicitTarget = threat;
                isAcquiredTarget = true;
                hasExplicitTarget = true;
            }
        }

        // Determine movePosition
        if (hasExplicitTarget)
        {
            movePosition = explicitTarget.position;
            pathUpdateCooldown = explicitTarget.speed > TargetMovingSpeedThreshold ? MovingTargetPathUpdateCooldown : basePathUpdateCooldown;
        }
        else
        {
            if (wasOriginallyAttackMoveToPosition)
            {
                movePosition = originalDestinationForAttackMove;
            }
            else
            {
                movePosition = lastKnownCommandedTargetPosition;
            }
            pathUpdateCooldown = basePathUpdateCooldown;
        }

        // Engagement Logic
        Entity targetToEngage = null;
        bool canEngage = _weaponsAspect != null && _weaponsAspect.weapon != null;

        if (canEngage)
        {
            if (hasExplicitTarget && explicitTarget != null && _weaponsAspect.IsTargetValid(explicitTarget))
            {
                float rangeSq = _weaponsAspect.weapon.range * _weaponsAspect.weapon.range;
                if ((explicitTarget.position - entity.position).sqrMagnitude <= rangeSq)
                {
                    targetToEngage = explicitTarget;
                }
            }
            else
            {
                targetToEngage = _weaponsAspect.FindImmediateThreatInRange();
            }
        }

        // Act
        if (targetToEngage != null)
        {
            AimAndFireAtTarget(targetToEngage);

            if (hasExplicitTarget && targetToEngage == explicitTarget)
            {
                
                base.Tick();
                entity.desiredSpeed = targetToEngage.speed;
            }
            else
            {
                entity.desiredSpeed = 0f;
            }
        }
        else
        {
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
            line.SetPosition(1, movePosition);
        }
    }

    private void AimAndFireAtTarget(Entity target)
    {
        if (target == null || _weaponsAspect == null || _weaponsAspect.weapon == null) return;

        Vector3 directionToTarget = target.position - entity.position;
        // entity.desiredHeading = Mathf.Atan2(directionToTarget.x, directionToTarget.z) * Mathf.Rad2Deg;
        WeaponsMgr.inst.handleWeapon(entity, target);
    }

    public override bool IsDone()
    {
        if (hasExplicitTarget)
        {
            return false;
        }
        else
        {
            bool canEngageNow = _weaponsAspect != null && _weaponsAspect.weapon != null;
            if (canEngageNow)
            {
                Entity threatInRange = _weaponsAspect.FindImmediateThreatInRange();
                if (threatInRange != null)
                {
                    return false;
                }
            }
            return base.IsDone();
        }
    }

    

    public override void Stop()
    {
        base.Stop();
        if (line != null)
        {
            line.gameObject.SetActive(false);
        }
    }
}