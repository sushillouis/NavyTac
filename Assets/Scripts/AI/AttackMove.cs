using UnityEngine;

[System.Serializable]
public class AttackMove : Move
{
    // Focus‑fire mode
    private Entity explicitTarget;
    private bool hasExplicitTarget;
    private readonly Entity commandedTarget;
    private Vector3 lastKnownCommandedTargetPosition;

    // Position‑based mode
    public bool acquireTargetsOnWay;               // true by default for position moves
    private Vector3 originalDestination;           // stored for position moves
    private readonly bool wasOriginallyAttackMoveToPosition;

    // Shared
    private WeaponsAspect _weaponsAspect;
    private readonly float basePathUpdateCooldown;
    private float timeSinceLastPathUpdate = 0f;
    private const float SignificantMovementThresholdSq = 1.0f;

    // Threat detection (used only when acquireTargetsOnWay is true)
    private const float threatCheckCooldown = 0.5f;
    private float timeSinceLastThreatCheck = 0f;
    private Entity opportunisticTarget;            // fired upon while moving, never followed

    // Constants
    private const float DefaultPathUpdateCooldown = 0.3f;
    private const float MovingTargetPathUpdateCooldown = 0.2f;
    private const float TargetMovingSpeedThreshold = 1.0f;

    /// <summary>
    /// Attack‑move to a position. By default, acquireTargetsOnWay = true,
    /// so the unit will stop and fire at enemies in range, then resume moving.
    /// </summary>
    public AttackMove(Entity ent, Vector3 pos, bool maxSpeed = false,
                      float doneDistanceSq = 100000f, bool isWaypoint = false)
        : base(ent, pos, maxSpeed, doneDistanceSq, isWaypoint)
    {
        InitializeWeaponsAspect();
        hasExplicitTarget = false;
        explicitTarget = null;
        commandedTarget = null;
        acquireTargetsOnWay = true;                 // default true for position moves
        originalDestination = pos;
        wasOriginallyAttackMoveToPosition = true;
        pathUpdateCooldown = DefaultPathUpdateCooldown;
        basePathUpdateCooldown = DefaultPathUpdateCooldown;
        opportunisticTarget = null;
    }

    /// <summary>
    /// Attack‑move focused on a specific entity. acquireTargetsOnWay is ignored.
    /// The unit will only engage this target (focus fire).
    /// </summary>
    public AttackMove(Entity ent, Entity target, bool maxSpeed = false,
                      float doneDistanceSq = 100000f, bool isWaypoint = false)
        : base(ent, target != null ? target.position : ent.position, maxSpeed, doneDistanceSq, isWaypoint)
    {
        InitializeWeaponsAspect();
        explicitTarget = target;
        hasExplicitTarget = (target != null);
        commandedTarget = target;
        acquireTargetsOnWay = false;                // not used in this mode
        wasOriginallyAttackMoveToPosition = false;

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
        opportunisticTarget = null;
    }

    private void InitializeWeaponsAspect()
    {
        if (entity != null)
            _weaponsAspect = entity.GetComponentInChildren<WeaponsAspect>();
        else
            _weaponsAspect = null;
    }

    public override void Init()
    {
        base.Init();
        if (!FogWarMgr.inst.nonRevelers.Contains(entity))
        {
            line = LineMgr.inst.CreateAttackMoveLine(entity.position, movePosition, entity.isAI);
            if (line != null)
                line.gameObject.SetActive(false);
        }
    }

    public override void Tick()
    {
        timeSinceLastThreatCheck += Time.deltaTime;
        timeSinceLastPathUpdate += Time.deltaTime;

        // --- FOCUS‑FIRE MODE (explicit target) ---
        if (hasExplicitTarget)
        {
            // Update last known commanded target position (for after death)
            if (commandedTarget != null && _weaponsAspect != null && _weaponsAspect.IsTargetValid(commandedTarget))
                lastKnownCommandedTargetPosition = commandedTarget.position;

            // Check if explicit target is still valid
            if (explicitTarget == null || (_weaponsAspect != null && !_weaponsAspect.IsTargetValid(explicitTarget)))
            {
                // Target lost – switch to moving to last known position
                hasExplicitTarget = false;
                movePosition = lastKnownCommandedTargetPosition;
                pathUpdateCooldown = basePathUpdateCooldown;
            }
            else
            {
                // Follow the target
                bool needsPathUpdate = timeSinceLastPathUpdate >= pathUpdateCooldown;
                bool targetMovedSignificantly = (explicitTarget.position - movePosition).sqrMagnitude > SignificantMovementThresholdSq;

                if (needsPathUpdate || targetMovedSignificantly)
                {
                    movePosition = explicitTarget.position;
                    pathUpdateCooldown = (explicitTarget.speed > TargetMovingSpeedThreshold)
                        ? MovingTargetPathUpdateCooldown
                        : basePathUpdateCooldown;
                    timeSinceLastPathUpdate = 0f;
                }

                // Engage if in range
                if (_weaponsAspect != null && _weaponsAspect.weapon != null)
                {
                    float rangeSq = _weaponsAspect.weapon.range * _weaponsAspect.weapon.range;
                    if ((explicitTarget.position - entity.position).sqrMagnitude <= rangeSq)
                    {
                        AimAndFireAtTarget(explicitTarget);
                        // Optionally slow down to match target speed
                        entity.desiredSpeed = explicitTarget.speed;
                    }
                }
                // Continue moving
                base.Tick();
                UpdateAttackLineRenderer();
                return;
            }
        }

        // --- POSITION‑BASED MODE (with opportunistic firing) ---
        // Destination is always the original position (or last known commanded position after target death)
        if (wasOriginallyAttackMoveToPosition)
            movePosition = originalDestination;
        else
            movePosition = lastKnownCommandedTargetPosition;

        // Simple path cooldown – destination does not change
        if (timeSinceLastPathUpdate >= pathUpdateCooldown)
        {
            timeSinceLastPathUpdate = 0f;
        }

        // Opportunistic threat detection
        if (acquireTargetsOnWay && timeSinceLastThreatCheck >= threatCheckCooldown && _weaponsAspect != null)
        {
            opportunisticTarget = _weaponsAspect.FindImmediateThreatInRange();
            timeSinceLastThreatCheck = 0f;
        }

        bool isFiring = false;
        if (opportunisticTarget != null && _weaponsAspect != null && _weaponsAspect.weapon != null)
        {
            if (!_weaponsAspect.IsTargetValid(opportunisticTarget))
            {
                opportunisticTarget = null;
            }
            else
            {
                float rangeSq = _weaponsAspect.weapon.range * _weaponsAspect.weapon.range;
                if ((opportunisticTarget.position - entity.position).sqrMagnitude <= rangeSq)
                {
                    // Stop moving and fire at the opportunistic target
                    AimAndFireAtTarget(opportunisticTarget);
                    entity.desiredSpeed = 0f;
                    // Face the target
                    Vector3 dirToTarget = (opportunisticTarget.position - entity.position).normalized;
                    entity.desiredHeading = Mathf.Atan2(dirToTarget.x, dirToTarget.z) * Mathf.Rad2Deg;
                    isFiring = true;
                }
                // else: target out of range – will resume moving below
            }
        }

        if (!isFiring)
        {
            // Resume moving toward the destination
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
            return false;
        return base.IsDone();
    }

    public override void Stop()
    {
        base.Stop();
        if (line != null)
            line.gameObject.SetActive(false);
    }
}