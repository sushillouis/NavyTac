using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class Move : Command
{
    private PathfindingState currentState = PathfindingState.RequestingPath;

    public Vector3 movePosition;
    public bool maxSpeedMovement;
    public float range;
    public float timeOnTarget;
    public LineRenderer potentialLine;
    public Vector3 diffToMovePosition = Vector3.positiveInfinity;
    public float dhRadians;
    public float dhDegrees;
    public Vector3 attractivePotential = Vector3.zero;
    public Vector3 potentialSum = Vector3.zero;
    public Vector3 repulsivePotential = Vector3.zero;
    public float dh;
    public float angleDiff;
    public float cosValue;
    public float ds;
    public float doneDistanceSq = 100000f;
    private int stuckFrames = 0;
    private const int maxStuckFrames = 60;
    private const float MinVelocityThresholdSq = 0.25f * 0.25f;
    private float previousDistanceToWaypoint = Mathf.Infinity;
    private List<Vector3> pathWaypoints = new List<Vector3>();
    private int currentWaypointIndex = 0;
    private const float waypointThreshold = 100f;
    public float pathUpdateCooldown = 1f;
    private float lastPathUpdateTime = -Mathf.Infinity;

    private Collider[] combinedQueryResults = new Collider[64];

    private static readonly int terrainMask;
    private static readonly int entityLayerMask;

    private Vector3 previousPotentialSum = Vector3.zero;
    public static float MaxTotalRepulsiveForceMagnitude = 20.0f;
    public static float PotentialSumDampingFactor = 0.5f;

    // RVO related parameters (can be moved to AIMgr or entity properties)
    public static float RVO_TimeHorizon = 2.0f;
    private static float RVO_AgentRadius = 200f; // Example radius, should be entity-specific
    public static float RVO_MaxNeighbors = 10;
    public static float RVO_NeighborDist = 50.0f;
    private float smoothedHeading;
    private float smoothedSpeed;
    private const float HeadingLerp = 0.18f;   // 0.0 → no smoothing, 1.0 → instant
    private const float SpeedLerp = 0.25f;
    private Vector3 _prevTerrainRepulsion = Vector3.zero;
    private const float TerrainDamping = 0.65f;

    // --- Clearance waiting state ---
    private bool waitingForClearance = false;
    private float clearanceCheckInterval = 0.25f;
    private float lastClearanceCheckTime = -Mathf.Infinity;

    static Move()
    {
        terrainMask = 1 << LayerMask.NameToLayer("Terrain");
        int mask = LayerMask.GetMask("Entities");
        if (mask == 0)
        {
            entityLayerMask = ~(terrainMask | (1 << LayerMask.NameToLayer("Ignore Raycast")) | (1 << LayerMask.NameToLayer("Water")));
        }
        else
        {
            entityLayerMask = mask;
        }
    }

    public Move(Entity ent, Vector3 pos, bool maxSpeedMovement = false, float doneDistanceSq = 100000f) : base(ent)
    {
        movePosition = pos;
        this.maxSpeedMovement = maxSpeedMovement;
        this.doneDistanceSq = doneDistanceSq;
    }
    public override void Init()
    {
        currentState = PathfindingState.RequestingPath;
        stuckFrames = 0;
        previousDistanceToWaypoint = Mathf.Infinity;
        currentWaypointIndex = 0;
        pathWaypoints.Clear();
        lastPathUpdateTime = -Mathf.Infinity;
        previousPotentialSum = Vector3.zero;
        waitingForClearance = false;

        if (!FogWarMgr.inst.nonRevelers.Contains(entity))
        {
            line = LineMgr.inst.CreateMoveLine(entity.position, movePosition);
            line.gameObject.SetActive(false);
            potentialLine = LineMgr.inst.CreatePotentialLine(entity.position);
            if (potentialLine != null)
                potentialLine.gameObject.SetActive(false);
        }
    }

    private void RequestNewPath()
    {
        if (Time.time > lastPathUpdateTime + pathUpdateCooldown)
        {
            PathRequestManager.RequestPath(entity.position, movePosition, OnPathReceived);
            lastPathUpdateTime = Time.time;
        }
    }

    private List<Vector3> PrunePath(List<Vector3> originalPath)
    {
        if (originalPath == null || originalPath.Count <= 1)
        {
            return new List<Vector3>(originalPath ?? new List<Vector3>());
        }

        List<Vector3> prunedPath = new List<Vector3>();
        prunedPath.Add(originalPath[0]);

        int currentAnchorIndexInOriginal = 0;
        while (currentAnchorIndexInOriginal < originalPath.Count - 1)
        {
            int furthestReachableIndex = currentAnchorIndexInOriginal + 1;
            for (int i = originalPath.Count - 1; i > currentAnchorIndexInOriginal; i--)
            {
                if (!Physics.Linecast(originalPath[currentAnchorIndexInOriginal], originalPath[i], terrainMask))
                {
                    furthestReachableIndex = i;
                    break;
                }
            }
            prunedPath.Add(originalPath[furthestReachableIndex]);
            currentAnchorIndexInOriginal = furthestReachableIndex;

            if (currentAnchorIndexInOriginal == originalPath.Count - 1)
            {
                break;
            }
        }
        return prunedPath;
    }

    private void OnPathReceived(Vector3[] path, bool success)
    {
        if (success && path.Length > 0)
        {
            List<Vector3> rawPath = new List<Vector3>(path);
            pathWaypoints = PrunePath(rawPath);

            if (pathWaypoints.Count == 0 && rawPath.Count > 0) {
                pathWaypoints.Add(rawPath[0]);
            }

            currentWaypointIndex = 0;
            stuckFrames = 0;
            previousDistanceToWaypoint = Mathf.Infinity;
            if (pathWaypoints.Count > 0) {
                 currentState = PathfindingState.FollowingPath;
            }
            currentState = PathfindingState.PotentialFieldsOnly;
        }
    }

    public override void Tick()
    {
        if (entity == null || currentState == PathfindingState.Finished) return;

        if (line != null && line.gameObject.activeInHierarchy) {
            line.SetPosition(0, entity.position);
                line.SetPosition(1, movePosition);
        }

        Vector3 desiredVelocityBeforeRVO;
        DHDS dhds;

        switch (currentState)
        {
            case PathfindingState.RequestingPath:
                RequestNewPath();
                dhds = UsePotentialFields(movePosition);
                break;
            case PathfindingState.FollowingPath:
                if (pathWaypoints.Count == 0 || currentWaypointIndex >= pathWaypoints.Count) {
                    currentState = PathfindingState.RequestingPath;
                    dhds = UsePotentialFields(movePosition);
                } else {
                    dhds = FollowPath();
                }
                break;
            case PathfindingState.PotentialFieldsOnly:
                dhds = UsePotentialFields(movePosition);
                break;
            default:
                dhds = new DHDS(entity.heading, 0); // Should not happen
                break;
        }

        // Convert DHDS to a velocity vector
        float speed = dhds.ds;
        float headingRad = dhds.dh * Mathf.Deg2Rad;
        desiredVelocityBeforeRVO = new Vector3(Mathf.Sin(headingRad), 0, Mathf.Cos(headingRad)) * speed;

        // Apply RVO
        Vector3 rvoAdjustedVelocity = ComputeRVOAdjustedVelocity(desiredVelocityBeforeRVO);

        // Convert RVO-adjusted velocity back to DHDS
        entity.desiredSpeed = rvoAdjustedVelocity.magnitude;
        if (entity.desiredSpeed > 0.01f) // Avoid Atan2(0,0)
        {
            entity.desiredHeading = Utils.Degrees360(Mathf.Rad2Deg * Mathf.Atan2(rvoAdjustedVelocity.x, rvoAdjustedVelocity.z));
        }
        // If speed is near zero, maintain current heading or heading towards target
        else if (desiredVelocityBeforeRVO.sqrMagnitude > 0.01f)
        {
             entity.desiredHeading = dhds.dh;
        }

        smoothedHeading = Mathf.LerpAngle(smoothedHeading, entity.desiredHeading, HeadingLerp);
        smoothedSpeed   = Mathf.Lerp(smoothedSpeed,   entity.desiredSpeed,   SpeedLerp);

        entity.desiredHeading = smoothedHeading;
        entity.desiredSpeed   = smoothedSpeed;
    }

 
    private Vector3 ComputeRVOAdjustedVelocity(Vector3 preferredVelocity)
    {
        if (entity == null )
            return preferredVelocity;

        List<Entity> neighbors = GetNearbyEntities(RVO_NeighborDist);
        if (neighbors.Count == 0)
            return preferredVelocity;

        // Step 1: Gather ORCA half-planes (lines) from neighbors
        List<ORCALine> orcaLines = new List<ORCALine>();
        Vector3 pos = entity.position;
        Vector3 vel = entity.GetComponent<Rigidbody>() ? entity.GetComponent<Rigidbody>().velocity : Vector3.zero;
        float radius = RVO_AgentRadius;
        float invTimeHorizon = 1.0f / RVO_TimeHorizon;

        foreach (var neighbor in neighbors)
        {
            if (neighbor == entity) continue;
            Vector3 neighborPos = neighbor.position;
            Vector3 neighborVel = neighbor.GetComponent<Rigidbody>() ? neighbor.GetComponent<Rigidbody>().velocity : Vector3.zero;

            Vector3 relPos = neighborPos - pos;
            Vector3 relVel = vel - neighborVel;
            float distSq = relPos.sqrMagnitude;
            float combinedRadius = radius + RVO_AgentRadius;
            float combinedRadiusSq = combinedRadius * combinedRadius;

            Vector3 u;
            if (distSq > combinedRadiusSq)
            {
                // No collision, create ORCA line
                Vector3 w = relVel - invTimeHorizon * relPos;
                float wLength = w.magnitude;
                Vector3 unitW = w / (wLength + 1e-6f);
                Vector3 lineDir = new Vector3(-unitW.z, 0, unitW.x);
                u = (combinedRadius * invTimeHorizon - wLength) * unitW;
                orcaLines.Add(new ORCALine
                {
                    point = vel + 0.5f * u,
                    direction = lineDir
                });
            }
            else
            {
                // Agents are colliding, create separating line
                Vector3 unitRelPos = relPos.normalized;
                Vector3 lineDir = new Vector3(-unitRelPos.z, 0, unitRelPos.x);
                u = (combinedRadius - Mathf.Sqrt(distSq)) * unitRelPos;
                orcaLines.Add(new ORCALine
                {
                    point = vel + 0.5f * u,
                    direction = lineDir
                });
            }
        }

        // Step 2: Linear program to find the closest velocity outside all ORCA lines
        Vector3 result = preferredVelocity;
        float maxSpeed = entity.maxSpeed;
        for (int i = 0; i < orcaLines.Count; ++i)
        {
            if (IsOnOrcaSide(result, orcaLines[i])) continue;

            // Project result onto the ORCA line
            result = ProjectOnOrcaLine(result, orcaLines[i], maxSpeed);

            // After projection, check all previous lines
            for (int j = 0; j < i; ++j)
            {
                if (!IsOnOrcaSide(result, orcaLines[j]))
                {
                    // If not feasible, project onto intersection
                    result = ProjectOnOrcaLine(result, orcaLines[j], maxSpeed);
                }
            }
        }

        // Clamp to max speed
        if (result.sqrMagnitude > maxSpeed * maxSpeed)
            result = result.normalized * maxSpeed;

        return result;
    }

    // ORCA line structure
    private struct ORCALine
    {
        public Vector3 point;     // A point on the line (in velocity space)
        public Vector3 direction; // The direction of the line (normalized)
    }

    // Returns true if velocity is on the allowed side of the ORCA line
    private bool IsOnOrcaSide(Vector3 velocity, ORCALine line)
    {
        Vector3 rel = velocity - line.point;
        return Vector3.Dot(line.direction, rel) >= 0;
    }

    // Projects velocity onto the allowed side of the ORCA line, clamped to maxSpeed
    private Vector3 ProjectOnOrcaLine(Vector3 velocity, ORCALine line, float maxSpeed)
    {
        Vector3 rel = velocity - line.point;
        float dot = Vector3.Dot(rel, line.direction);
        Vector3 proj = line.point + line.direction * dot;
        if (proj.sqrMagnitude > maxSpeed * maxSpeed)
            proj = proj.normalized * maxSpeed;
        return proj;
    }

    private List<Entity> GetNearbyEntities(float radius)
    {
        List<Entity> nearbyEntities = new List<Entity>();
        Collider[] hitColliders = Physics.OverlapSphere(entity.position, radius, entityLayerMask);
        foreach (var hitCollider in hitColliders)
        {
            Entity otherEntity = hitCollider.GetComponent<Entity>();
            if (otherEntity != null && otherEntity != entity)
            {
                nearbyEntities.Add(otherEntity);
            }
        }
        return nearbyEntities;
    }


    private DHDS FollowPath()
    {
        if (currentWaypointIndex >= pathWaypoints.Count)
        {
            currentState = PathfindingState.RequestingPath;
            return ComputePotentialDHDS(movePosition); // Fallback
        }

        Vector3 currentWaypoint = pathWaypoints[currentWaypointIndex];

        DHDS dhds = ComputePotentialDHDS(currentWaypoint);
        // entity.desiredHeading = dhds.dh; // Will be set in Tick after RVO
        // entity.desiredSpeed = dhds.ds;   // Will be set in Tick after RVO

        float distanceToWaypointSq = (entity.position - currentWaypoint).sqrMagnitude;
        float waypointThresholdSq = waypointThreshold * waypointThreshold;

        bool isEffectivelyStuck = false;
        Rigidbody rb = entity.GetComponent<Rigidbody>();

        if (rb != null) {
            if (rb.velocity.sqrMagnitude < MinVelocityThresholdSq &&
                distanceToWaypointSq > (waypointThreshold * 0.2f) * (waypointThreshold * 0.2f)) {
                isEffectivelyStuck = true;
            }
        } else {
            float currentLinearDist = Mathf.Sqrt(distanceToWaypointSq);
            if (currentLinearDist >= previousDistanceToWaypoint - (waypointThreshold * 0.05f)) {
                 isEffectivelyStuck = true;
            }
            previousDistanceToWaypoint = currentLinearDist;
        }

        if (isEffectivelyStuck) {
            stuckFrames++;
        } else {
            stuckFrames = Mathf.Max(0, stuckFrames - 5);
        }

        if (potentialSum.sqrMagnitude > 0.01f) {
            Vector3 toWaypointDir = (currentWaypoint - entity.position).normalized;
            Vector3 potentialDir = potentialSum.normalized;
            if (Vector3.Dot(toWaypointDir, potentialDir) < 0.0f && distanceToWaypointSq > waypointThresholdSq * 0.1f) {
                stuckFrames += 3;
            }
        }

        if (stuckFrames >= maxStuckFrames)
        {
            currentWaypointIndex++;
            stuckFrames = 0;
            previousDistanceToWaypoint = Mathf.Infinity;

            if (currentWaypointIndex >= pathWaypoints.Count)
            {
                currentState = PathfindingState.RequestingPath;
            }
            return dhds; // Return current DHDS, Tick will handle RVO
        }

        if (distanceToWaypointSq < waypointThresholdSq)
        {
            currentWaypointIndex++;
            stuckFrames = 0;
            previousDistanceToWaypoint = Mathf.Infinity;
            if (currentWaypointIndex >= pathWaypoints.Count)
            {
                 currentState = PathfindingState.RequestingPath;
            }
        }

        if (currentState == PathfindingState.FollowingPath && pathWaypoints.Count > 0 && currentWaypointIndex < pathWaypoints.Count)
        {
            float timeSinceLastPathUpdate = Time.time - lastPathUpdateTime;
            if (timeSinceLastPathUpdate > pathUpdateCooldown * 3)
            {
                currentState = PathfindingState.RequestingPath;
            }
        }
        return dhds; // Return DHDS based on potential fields towards waypoint
    }

    private DHDS UsePotentialFields(Vector3 target)
    {
        DHDS dhds = AIMgr.inst.isPotentialFieldsMovement ?
                   ComputePotentialDHDS(target) :
                   ComputeDHDS(target);

        // entity.desiredHeading = dhds.dh; // Will be set in Tick after RVO
        // entity.desiredSpeed = dhds.ds;   // Will be set in Tick after RVO
        return dhds;
    }

    public virtual DHDS ComputeDHDS(Vector3 target)
    {
        diffToMovePosition = target - entity.position;
        dhRadians = Mathf.Atan2(diffToMovePosition.x, diffToMovePosition.z);
        dhDegrees = Utils.Degrees360(Mathf.Rad2Deg * dhRadians);
        return new DHDS(dhDegrees, entity.maxSpeed);
    }

    public virtual DHDS ComputeDHDS()
    {
        return ComputeDHDS(this.movePosition);
    }

    public virtual DHDS ComputePotentialDHDS(Vector3 targetPosition)
    {
        diffToMovePosition = targetPosition - entity.position;
        Vector3 currentEntityRepulsion = Vector3.zero;

        float entityDetectionRadius = AIMgr.inst.potentialDistanceThreshold;
        int numFoundEntities = Physics.OverlapSphereNonAlloc(entity.position, entityDetectionRadius, combinedQueryResults, entityLayerMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < numFoundEntities; i++)
        {
        Collider col = combinedQueryResults[i];
        Vector3 diffToOtherEntity = col.transform.position - entity.position;
        float sqrDistToOtherEntity = diffToOtherEntity.sqrMagnitude;

        if (sqrDistToOtherEntity > 0.0001f)
        {
            Entity ent = col.GetComponent<Entity>();
            if (ent == null || ent == entity || ent.entityClass == EntityClass.Missile) continue;

            Potential p = DistanceMgr.inst.GetPotential(entity, ent);
            if (p == null || p.distance > AIMgr.inst.potentialDistanceThreshold) continue;

            float distToEnt = Mathf.Sqrt(sqrDistToOtherEntity);

            currentEntityRepulsion += -p.direction * ent.mass *
            AIMgr.inst.repulsiveCoefficient *
            Mathf.Pow(distToEnt, AIMgr.inst.repulsiveExponent);
        }
        }

        Vector3 currentTerrainRepulsion = ComputeTerrainRepulsion(entity.position);
        repulsivePotential = currentEntityRepulsion + currentTerrainRepulsion;

        if (repulsivePotential.sqrMagnitude > MaxTotalRepulsiveForceMagnitude * MaxTotalRepulsiveForceMagnitude)
        {
        repulsivePotential = repulsivePotential.normalized * MaxTotalRepulsiveForceMagnitude;
        }

        Vector3 rawAttraction = targetPosition - entity.position;
        float distToTarget = rawAttraction.magnitude;
        if (distToTarget > 0.001f)
        {
        attractivePotential = (rawAttraction / distToTarget) *
            AIMgr.inst.attractionCoefficient *
            Mathf.Pow(distToTarget, AIMgr.inst.attractiveExponent);
        }
        else
        {
        attractivePotential = Vector3.zero;
        }

        Vector3 currentFramePotentialSum = attractivePotential + repulsivePotential;
        potentialSum = Vector3.Lerp(previousPotentialSum, currentFramePotentialSum, PotentialSumDampingFactor);
        previousPotentialSum = potentialSum;

        dh = Utils.Degrees360(Mathf.Rad2Deg * Mathf.Atan2(potentialSum.x, potentialSum.z));
        angleDiff = Utils.Degrees360(Utils.AngleDiffPosNeg(dh, entity.heading));
        cosValue = (Mathf.Cos(angleDiff * Mathf.Deg2Rad) + 1) / 2.0f;
        ds = entity.maxSpeed * cosValue;

        if (potentialLine != null && potentialLine.gameObject.activeInHierarchy)
        {
        potentialLine.SetPosition(0, entity.position);
        potentialLine.SetPosition(1, entity.position + potentialSum.normalized * 100f);
        }

        return new DHDS(dh, ds);
    }

   private Vector3 ComputeTerrainRepulsion(Vector3 shipPosition)
{
    Vector3 totalRepulsion = Vector3.zero;
    float detectionRadius = AIMgr.inst.terrainDetectionRadius;
    float maxRepel = AIMgr.inst.maxTerrainRepulsion;
    int numRays = 32; 

    for (int i = 0; i < numRays; i++)
    {
        float angle = i * (360f / numRays);
        Vector3 rayDir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
        rayDir.y = 0;

        RaycastHit hit;
        if (Physics.Raycast(
            shipPosition,
            rayDir,
            out hit,
            detectionRadius,
            AIMgr.inst.terrainLayerMask
        ))
        {
            if (hit.point.y < 0)
                continue;

            float proximity = 1 - (hit.distance / detectionRadius);
            float strength = Mathf.Pow(proximity, 2) * maxRepel; 

            Vector3 repelDir = -rayDir.normalized;
            totalRepulsion += repelDir * strength;
        }
    }
    Collider[] hits = Physics.OverlapSphere(shipPosition, 2.0f, AIMgr.inst.terrainLayerMask);
    if (hits.Length > 0)
    {
        foreach (var col in hits)
        {
            Vector3 away = (shipPosition - col.ClosestPoint(shipPosition)).normalized;
            totalRepulsion += away * maxRepel * 2f;
        }
    }

    totalRepulsion = Vector3.Lerp(_prevTerrainRepulsion, totalRepulsion, TerrainDamping);
    _prevTerrainRepulsion = totalRepulsion;

    return totalRepulsion;
}
    public DHDS ComputePF2()
    {
        Collider[] tempEntityColliders = new Collider[32];

        diffToMovePosition = movePosition - entity.position;
        Vector3 tmp = (movePosition - entity.position);

        Vector3 currentRepulsivePotential = Vector3.one;
        currentRepulsivePotential.y = 0;

        float detectionRadius = Mathf.Sqrt(AIMgr.inst.potentialDistanceThresholdSq);
        int numFoundEntities = Physics.OverlapSphereNonAlloc(entity.position, detectionRadius, tempEntityColliders, entityLayerMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < numFoundEntities; i++)
        {
            Entity otherEnt = tempEntityColliders[i].GetComponent<Entity>();
            if (otherEnt == null || otherEnt == entity) continue;

            if ((entity.position - otherEnt.position).sqrMagnitude < AIMgr.inst.potentialDistanceThresholdSq)
            {
                Potential pot;
                if(entity.ai.potentialsD.ContainsKey(otherEnt))
                {
                    pot = entity.ai.potentialsD[otherEnt];
                }
                else
                {
                    pot = new Potential(entity, otherEnt);
                    entity.ai.potentialsD.Add(otherEnt, pot);
                    entity.ai.potentialsL.Add(new EntityPotential { entity = otherEnt, potential = pot });
                }
                pot.ReCompute();
                foreach(SubPotential subPot in pot.subPotentials)
                {
                    currentRepulsivePotential += subPot.direction * otherEnt.mass * 0.05f
                        * AIMgr.inst.repulsive2Coefficient * Mathf.Pow(subPot.distance, AIMgr.inst.repulsiveExponent);
                }
            }
        }

        Vector3 currentAttractivePotential;
        float magDiffToMovePos = tmp.magnitude;
        if (magDiffToMovePos > 0.001f) {
             currentAttractivePotential = (tmp / magDiffToMovePos)
                * AIMgr.inst.attraction2Coefficient * entity.mass * Mathf.Pow(magDiffToMovePos, AIMgr.inst.attractiveExponent);
        } else {
            currentAttractivePotential = Vector3.zero;
        }

        potentialSum = currentAttractivePotential + currentRepulsivePotential;

        dh = Utils.Degrees360(Mathf.Rad2Deg * Mathf.Atan2(potentialSum.x, potentialSum.z));
        angleDiff = Utils.Degrees360(Utils.AngleDiffPosNeg(dh, entity.heading));
        cosValue = (Mathf.Cos(angleDiff * Mathf.Deg2Rad) + 1) / 2.0f;
        ds = entity.maxSpeed * cosValue;
        return new DHDS(dh, ds);
    }
    public override bool IsDone()
{
    // 0. Null / already-finished guard
    if (entity == null)
    {
        currentState = PathfindingState.Finished;
        return true;
    }

    // 1. Close enough to the commanded position?
    bool reached = (entity.position - movePosition).sqrMagnitude < doneDistanceSq;

    if (!reached) return false;

    // 2. Make sure no *other* agent is within my safety bubble.
    float clearanceRadius = entity.length;          // ← uses the ship’s own length
    Collider[] hits = Physics.OverlapSphere(
        entity.position,
        clearanceRadius,
        entityLayerMask,
        QueryTriggerInteraction.Ignore);

    foreach (var hit in hits)
    {
        Entity other = hit.GetComponent<Entity>();
        if (other != null && other != entity)
            return false;                           // spot occupied – keep moving
    }

    return true;                                    // safe: goal reached *and* clear
}

    public override void Stop()
    {
        Vector3 directionToTarget = Vector3.zero;
        bool shouldStop = true;

        if (entity != null)
        {
            directionToTarget = movePosition - entity.position;

            // Check for entity at entity.length distance in the direction to target
            if (directionToTarget.sqrMagnitude > 0.001f)
            {
                Vector3 checkDir = directionToTarget.normalized;
                float checkDist = entity.length;
                RaycastHit hit;
                if (Physics.Raycast(entity.position, checkDir, out hit, checkDist, entityLayerMask))
                {
                    Entity hitEntity = hit.collider.GetComponent<Entity>();
                    if (hitEntity != null && hitEntity != entity)
                    {
                        shouldStop = false;
                    }
                }
            }

            if (shouldStop)
            {
                entity.desiredSpeed = 0;
            }
        }

        if (shouldStop)
        {
            if (line != null) LineMgr.inst.DestroyLR(line);
            if (potentialLine != null) LineMgr.inst.DestroyLR(potentialLine);
            line = null;
            potentialLine = null;

            pathWaypoints.Clear();
            currentWaypointIndex = 0;
            lastPathUpdateTime = -Mathf.Infinity;
            stuckFrames = 0;
            previousDistanceToWaypoint = Mathf.Infinity;
            currentState = PathfindingState.Finished;
            previousPotentialSum = Vector3.zero;

            base.Stop();
            if (directionToTarget.sqrMagnitude > 0.001f && entity != null)
            {
                float heading = Utils.Degrees360(Mathf.Rad2Deg * Mathf.Atan2(directionToTarget.normalized.x, directionToTarget.normalized.z));
                entity.desiredHeading = heading;
            }
        }
    }
}
