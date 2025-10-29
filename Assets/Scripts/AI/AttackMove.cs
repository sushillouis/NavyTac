using UnityEngine;

[System.Serializable]
public class AttackMove : Move
{
    private Entity explicitTarget;
    private bool hasExplicitTarget;
    private readonly Entity commandedTarget;
    private Vector3 lastKnownCommandedTargetPosition;
    private bool isAcquiredTarget;
    private readonly bool acquireTargetsOnWay;
    private Vector3 originalDestinationForAttackMove;
    private readonly bool wasOriginallyAttackMoveToPosition;
    private readonly float basePathUpdateCooldown;
    private Vector3 lastKnownTargetPosition;
    private WeaponsAspect _weaponsAspect;
    private const float DefaultPathUpdateCooldown = 0.3f;
    private const float MovingTargetPathUpdateCooldown = 0.2f;
    private const float TargetMovingSpeedThreshold = 1.0f;
    // Optimization fields
    private readonly float threatCheckCooldown = 0.5f;
    private float timeSinceLastThreatCheck = 0f;
    private float timeSinceLastPathUpdate = 0f;
    private const float SignificantMovementThresholdSq = 1.0f; // 1 unit squared
    
    public AttackMove(Entity ent, Vector3 pos, bool maxSpeed = false, float doneDistanceSq = 100000f, bool isWaypoint = false) 
        : base(ent, pos, maxSpeed, doneDistanceSq, isWaypoint)
    {
        InitializeWeaponsAspect();
        hasExplicitTarget = false;
        explicitTarget = null;
        commandedTarget = null;
        isAcquiredTarget = false;
        acquireTargetsOnWay = false;
        pathUpdateCooldown = DefaultPathUpdateCooldown;
        basePathUpdateCooldown = DefaultPathUpdateCooldown;
        originalDestinationForAttackMove = pos;
        wasOriginallyAttackMoveToPosition = true;
        lastKnownTargetPosition = pos;
        lastKnownCommandedTargetPosition = pos;
       
    }

    public AttackMove(Entity ent, Entity target, bool acquireTargetsOnWay = false, bool maxSpeed = false, float doneDistanceSq = 100000f, bool isWaypoint = false)
        : base(ent, target != null ? target.position : ent.position, maxSpeed, doneDistanceSq, isWaypoint)
    {
        InitializeWeaponsAspect();
        explicitTarget = target;
        hasExplicitTarget = (target != null);
        commandedTarget = target;
        isAcquiredTarget = false;
        this.acquireTargetsOnWay = acquireTargetsOnWay;

        lastKnownTargetPosition = target != null ? target.position : ent.position;
        lastKnownCommandedTargetPosition = target != null ? target.position : ent.position;

        if (target != null && _weaponsAspect != null && _weaponsAspect.IsTargetValid(target))
        {
            pathUpdateCooldown = target.speed > TargetMovingSpeedThreshold
                ? MovingTargetPathUpdateCooldown
                : DefaultPathUpdateCooldown;
        }
        else
        {
            pathUpdateCooldown = DefaultPathUpdateCooldown;
        }

        basePathUpdateCooldown = DefaultPathUpdateCooldown;
        wasOriginallyAttackMoveToPosition = false;
    }

    private void InitializeWeaponsAspect()
    {
        if (entity != null) // Check if the entity is valid (not null and not destroyed)
        {
            _weaponsAspect = entity.GetComponentInChildren<WeaponsAspect>();
            if (_weaponsAspect == null)
            {
            }
        }
        else
        {
            _weaponsAspect = null; // Ensure _weaponsAspect is null if entity is invalid
        }
    }

    public override void Init()
    {
        base.Init();
        

        if (!FogWarMgr.inst.nonRevelers.Contains(entity))
        {
            line = LineMgr.inst.CreateAttackMoveLine(entity.position, movePosition,entity.isAI);
            if (line != null)
            {
                line.gameObject.SetActive(false);
            }
        }
    }

    public override void Tick()
    {
        // Update timers
        timeSinceLastThreatCheck += Time.deltaTime;
        timeSinceLastPathUpdate += Time.deltaTime;

        // Update last known positions - cheap operation
        if (commandedTarget != null && _weaponsAspect != null && _weaponsAspect.IsTargetValid(commandedTarget))
        {
            lastKnownCommandedTargetPosition = commandedTarget.position;
        }
        if (hasExplicitTarget && explicitTarget != null && _weaponsAspect != null && _weaponsAspect.IsTargetValid(explicitTarget))
        {
            lastKnownTargetPosition = explicitTarget.position;
        }

        // Handle explicitTarget becoming invalid
        if (hasExplicitTarget && (explicitTarget == null || (_weaponsAspect != null && !_weaponsAspect.IsTargetValid(explicitTarget))))
        {
            if (isAcquiredTarget)
            {
                if (commandedTarget != null && _weaponsAspect != null && _weaponsAspect.IsTargetValid(commandedTarget))
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
                hasExplicitTarget = false;
                movePosition = lastKnownTargetPosition;
            }
        }

        // Threat detection with cooldown
        if (acquireTargetsOnWay && timeSinceLastThreatCheck >= threatCheckCooldown && _weaponsAspect != null)
        {
            Entity threat = _weaponsAspect.FindImmediateThreatInRange();
            if (threat != null && threat != explicitTarget)
            {
                explicitTarget = threat;
                isAcquiredTarget = true;
                hasExplicitTarget = true;
            }
            timeSinceLastThreatCheck = 0f;
        }

        // Determine if we need to update path
        bool needsPathUpdate = timeSinceLastPathUpdate >= pathUpdateCooldown;
        bool targetMovedSignificantly = false;

        if (hasExplicitTarget && explicitTarget != null)
        {
            // Check if target has moved significantly since last path update
            float moveDistanceSq = (explicitTarget.position - movePosition).sqrMagnitude;
            targetMovedSignificantly = moveDistanceSq > SignificantMovementThresholdSq;
        }

        // Update destination only when needed
        if (needsPathUpdate || targetMovedSignificantly)
        {
            if (hasExplicitTarget && explicitTarget != null)
            {
                movePosition = explicitTarget.position;
                if (explicitTarget.speed > TargetMovingSpeedThreshold)
                {
                    pathUpdateCooldown = MovingTargetPathUpdateCooldown;
                }
                else
                {
                    pathUpdateCooldown = basePathUpdateCooldown;
                }
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
            timeSinceLastPathUpdate = 0f;
        }

        // Engagement Logic
        Entity targetToEngage = null;
        bool canEngage = _weaponsAspect != null && _weaponsAspect.weapon != null;

        if (canEngage)
        {
            if (hasExplicitTarget && explicitTarget != null && _weaponsAspect.IsTargetValid(explicitTarget))
            {
                float rangeSq = (_weaponsAspect.weapon.range * _weaponsAspect.weapon.range)-100f*100f;
                if ((explicitTarget.position - entity.position).sqrMagnitude <= rangeSq)
                {
                    targetToEngage = explicitTarget;
                }
            }
            else
            {
                // Only check for immediate threats if not in cooldown
                if (timeSinceLastThreatCheck >= threatCheckCooldown * 0.5f)
                {
                    targetToEngage = _weaponsAspect.FindImmediateThreatInRange();
                }
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

            Vector3 directionToTarget = (targetToEngage.position - entity.position).normalized;
            entity.desiredHeading = Mathf.Atan2(directionToTarget.x, directionToTarget.z) * Mathf.Rad2Deg;
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
            bool shouldShow = entity.isSelected && (hasExplicitTarget || !IsDone());
            line.gameObject.SetActive(shouldShow);
            
            if (shouldShow)
            {
                line.positionCount = 2;
                line.SetPosition(0, entity.position);
                line.SetPosition(1, movePosition);
            }
        }
    }

    private void AimAndFireAtTarget(Entity target)
    {
        if (target == null || _weaponsAspect == null || _weaponsAspect.weapon == null) return;
        WeaponsMgr.inst.handleWeapon(entity, target);
    }

    public override bool IsDone()
    {
        if (hasExplicitTarget)
        {
            return false;
        }
    
        if (_weaponsAspect != null && _weaponsAspect.weapon != null)
        {
            // Only check for threats periodically
            if (timeSinceLastThreatCheck >= threatCheckCooldown * 0.7f)
            {
                Entity threatInRange = _weaponsAspect.FindImmediateThreatInRange();
                if (threatInRange != null) return false;
            }
        }
        return base.IsDone();
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