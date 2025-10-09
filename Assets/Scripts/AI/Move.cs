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

    // (RVO removed) Keep smoothing for heading/speed to avoid jitter
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

    private Vector3 lastPathRequestStart;
private Vector3 lastPathRequestTarget;
private float pathSimilarityThreshold = 50f; // If target moved less than this, reuse path

private void RequestNewPath()
{
    float startDistance = (lastPathRequestStart - entity.position).sqrMagnitude;
    float targetDistance = (lastPathRequestTarget - movePosition).sqrMagnitude;
    
    // Reuse path if we haven't moved much and target hasn't moved much
    if (pathWaypoints.Count > 0 && 
        startDistance < pathSimilarityThreshold && 
        targetDistance < pathSimilarityThreshold &&
        Time.time < lastPathUpdateTime + pathUpdateCooldown * 2f)
    {
        return;
    }

    if (Time.time > lastPathUpdateTime + pathUpdateCooldown)
    {
        PathRequestManager.RequestPath(entity.position, movePosition, OnPathReceived);
        lastPathUpdateTime = Time.time;
        lastPathRequestStart = entity.position;
        lastPathRequestTarget = movePosition;
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

    int currentAnchorIndex = 0;
    
    while (currentAnchorIndex < originalPath.Count - 1)
    {
        int furthestReachableIndex = currentAnchorIndex + 1;
        Vector3 bestWaypoint = originalPath[currentAnchorIndex + 1];
        
        for (int i = originalPath.Count - 1; i > currentAnchorIndex; i--)
        {
            if (!Physics.Linecast(originalPath[currentAnchorIndex], originalPath[i], terrainMask))
            {
                // Found a direct path, now find the best center position
                furthestReachableIndex = i;
                
                // Try to find a center position between anchor and furthest
                if (i - currentAnchorIndex > 2)
                {
                    int midIndex = (currentAnchorIndex + i) / 2;
                    Vector3 candidate = originalPath[midIndex];
                    
                    // Check if we can use this center position
                    if (!Physics.Linecast(originalPath[currentAnchorIndex], candidate, terrainMask) &&
                        !Physics.Linecast(candidate, originalPath[i], terrainMask))
                    {
                        bestWaypoint = candidate;
                        furthestReachableIndex = midIndex;
                    }
                    else
                    {
                        bestWaypoint = originalPath[i];
                    }
                }
                else
                {
                    bestWaypoint = originalPath[i];
                }
                break;
            }
        }
        
        prunedPath.Add(bestWaypoint);
        currentAnchorIndex = furthestReachableIndex;

        if (currentAnchorIndex == originalPath.Count - 1)
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

        // Use DHDS directly (RVO removed)
        entity.desiredSpeed = dhds.ds;
        entity.desiredHeading = dhds.dh;

        smoothedHeading = Mathf.LerpAngle(smoothedHeading, entity.desiredHeading, HeadingLerp);
        smoothedSpeed   = Mathf.Lerp(smoothedSpeed,   entity.desiredSpeed,   SpeedLerp);

        entity.desiredHeading = smoothedHeading;
        entity.desiredSpeed   = smoothedSpeed;
    }




    private DHDS FollowPath()
    {
        if (currentWaypointIndex >= pathWaypoints.Count)
        {
            currentState = PathfindingState.RequestingPath;
            return ComputePotentialDHDS(movePosition);
        }

        Vector3 currentWaypoint = pathWaypoints[currentWaypointIndex];

        // Look ahead in the path to smooth movement
        Vector3 lookAheadWaypoint = currentWaypoint;
        if (currentWaypointIndex < pathWaypoints.Count - 1)
        {
            // Blend current and next waypoint for smoother turns
            Vector3 nextWaypoint = pathWaypoints[currentWaypointIndex + 1];
            float lookAheadFactor = Mathf.Clamp01((entity.position - currentWaypoint).magnitude / waypointThreshold);
            lookAheadWaypoint = Vector3.Lerp(currentWaypoint, nextWaypoint, lookAheadFactor * 0.3f);
        }

        DHDS dhds = ComputePotentialDHDS(lookAheadWaypoint);

        float distanceToWaypointSq = (entity.position - currentWaypoint).sqrMagnitude;
        float waypointThresholdSq = waypointThreshold * waypointThreshold;

        // Existing stuck detection logic...
        // [Keep your existing stuck detection code here]

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

        return dhds;
    }


    private DHDS UsePotentialFields(Vector3 target)
    {
        DHDS dhds = AIMgr.inst.isPotentialFieldsMovement ?
                   ComputePotentialDHDS(target) :
                   ComputeDHDS(target);

        if (AIMgr.inst.isPotentialFieldsMovement)
        {
            entity.desiredHeading = dhds.dh;
            entity.desiredSpeed = dhds.ds;
        }
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
    // Add path following influence to potential fields
    Vector3 pathInfluence = Vector3.zero;
    
    if (currentState == PathfindingState.FollowingPath && pathWaypoints.Count > 0 && currentWaypointIndex < pathWaypoints.Count)
    {
        Vector3 pathDirection = (pathWaypoints[currentWaypointIndex] - entity.position).normalized;
        pathInfluence = pathDirection * AIMgr.inst.attractionCoefficient * 0.5f; // Reduced influence
    }

    // Your existing potential field code...
    diffToMovePosition = targetPosition - entity.position;
    Vector3 currentEntityRepulsion = Vector3.zero;
    
    // [Keep your existing entity repulsion code]
    
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

    // Combine with path influence
    attractivePotential += pathInfluence;

    Vector3 currentFramePotentialSum = attractivePotential + repulsivePotential;
    potentialSum = Vector3.Lerp(previousPotentialSum, currentFramePotentialSum, PotentialSumDampingFactor);
    previousPotentialSum = potentialSum;

    dh = Utils.Degrees360(Mathf.Rad2Deg * Mathf.Atan2(potentialSum.x, potentialSum.z));
    angleDiff = Utils.Degrees360(Utils.AngleDiffPosNeg(dh, entity.heading));
    cosValue = (Mathf.Cos(angleDiff * Mathf.Deg2Rad) + 1) / 2.0f;
    ds = entity.maxSpeed * cosValue;

    // [Keep your existing visualization code]
    
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
