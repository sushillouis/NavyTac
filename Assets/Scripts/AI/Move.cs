
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class Move : Command
{
    // Existing potential field variables
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
    private const int maxStuckFrames = 30; // Adjust based on testing (e.g., ~1 second at 30 FPS)
    private float previousDistanceToWaypoint = Mathf.Infinity;
    // A* Pathfinding integration
    private List<Vector3> pathWaypoints = new List<Vector3>();
    private int currentWaypointIndex = 0;
    private bool hasPath = false;
    private const float waypointThreshold = 100f; // Distance to consider waypoint reached
    public float pathUpdateCooldown = 1f;
    private float lastPathUpdateTime = -Mathf.Infinity;

    public Move(Entity ent, Vector3 pos, bool maxSpeedMovement = false, float doneDistanceSq = 100000f) : base(ent)
    {
        movePosition = pos;
        this.maxSpeedMovement = maxSpeedMovement;
        
        if (ent.GetComponentInChildren<WeaponsAspect>() != null)
        {
            this.doneDistanceSq = ent.GetComponentInChildren<WeaponsAspect>().weapon.range * 
                                 ent.GetComponentInChildren<WeaponsAspect>().weapon.range;
        }
    }

    public override void Init()
    {     
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

    private void OnPathReceived(Vector3[] path, bool success)
    {
        if (success && path.Length > 0)
        {
            pathWaypoints = new List<Vector3>(path);
            currentWaypointIndex = 0;
            hasPath = true;
        }
        else
        {
            // Fallback to direct potential field movement
            hasPath = false;
        }
    }

    public override void Tick()
{
    // Request path only when starting to process this command
    if (!hasPath)
    {
        RequestNewPath();
    }

    if (hasPath && currentWaypointIndex < pathWaypoints.Count)
    {
        FollowPath();
    }
    else
    {
        UsePotentialFields();
    }
}

    private void FollowPath()
    {
    Vector3 currentWaypoint = pathWaypoints[currentWaypointIndex];
    DHDS dhds = ComputePotentialDHDS(currentWaypoint);

    entity.desiredHeading = dhds.dh;
    entity.desiredSpeed = dhds.ds;

    // Calculate current distance and direction to waypoint
    float currentDistance = Vector3.Distance(entity.position, currentWaypoint);
    Vector3 toWaypointDir = (currentWaypoint - entity.position).normalized;

    // Directional alignment check using potential sum
    Vector3 potentialDir = potentialSum.normalized;
    float directionDot = Vector3.Dot(toWaypointDir, potentialDir);

    // Progress check: compare with previous distance
    if (currentDistance >= previousDistanceToWaypoint - waypointThreshold * 0.1f)
    {
        stuckFrames++;
    }
    else
    {
        stuckFrames = Mathf.Max(0, stuckFrames - 1);
    }

    // Update previous distance
    previousDistanceToWaypoint = currentDistance;

    // Check if potential is working against reaching the waypoint
    if (directionDot < 0.3f) // Threshold for directional misalignment
    {
        stuckFrames += 2; // Accelerate stuck count
    }

    // Skip waypoint if stuck
    if (stuckFrames >= maxStuckFrames)
    {
        currentWaypointIndex++;
        stuckFrames = 0;
        previousDistanceToWaypoint = Mathf.Infinity;
        
        // Immediately request new path if skipping waypoints
        RequestNewPath();
        return;
    }

    // Existing waypoint distance check
    if (currentDistance < waypointThreshold)
    {
        currentWaypointIndex++;
        stuckFrames = 0;
        previousDistanceToWaypoint = Mathf.Infinity;
    }

        // Check waypoint progression
        float distanceToWaypoint = Vector3.Distance(entity.position, currentWaypoint);
        if (distanceToWaypoint < waypointThreshold)
        {
            currentWaypointIndex++;
            
            // Request new path if final waypoint is obsolete
            if (currentWaypointIndex >= pathWaypoints.Count)
            {
                RequestNewPath();
            }
        }

        // Periodic path validation
        if (currentWaypointIndex > 0 && currentWaypointIndex < pathWaypoints.Count)
        {
            float segmentProgress = (float)currentWaypointIndex / pathWaypoints.Count;
            if (segmentProgress > 0.7f) // Refresh path when 70% through current path
            {
                RequestNewPath();
            }
        }
    }

    private void UsePotentialFields()
    {
        // Original potential field implementation
        DHDS dhds = AIMgr.inst.isPotentialFieldsMovement ? 
                   ComputePotentialDHDS(movePosition) : 
                   ComputeDHDS();

        entity.desiredHeading = dhds.dh;
        entity.desiredSpeed = dhds.ds;
    }

    public virtual DHDS ComputeDHDS()
    {
        diffToMovePosition = movePosition - entity.position;
        dhRadians = Mathf.Atan2(diffToMovePosition.x, diffToMovePosition.z);
        dhDegrees = Utils.Degrees360(Mathf.Rad2Deg * dhRadians);
        return new DHDS(dhDegrees, entity.maxSpeed);
    }

    public virtual DHDS ComputePotentialDHDS(Vector3 movePosition)
    {
        diffToMovePosition = movePosition - entity.position;
        repulsivePotential = Vector3.zero;
        attractivePotential = Vector3.zero;
        potentialSum = Vector3.zero;

        // Entity repulsion
        foreach (Entity ent in EntityMgr.inst.entities)
        {
            if (ent == entity) continue;

            Potential p = DistanceMgr.inst.GetPotential(entity, ent);
            if (p.distance < AIMgr.inst.potentialDistanceThreshold)
            {
                repulsivePotential += p.direction * ent.mass *
                    AIMgr.inst.repulsiveCoefficient * 
                    Mathf.Pow(p.diff.magnitude, AIMgr.inst.repulsiveExponent);
            }
        }

        // No-Go Zone repulsion
        foreach (NoGoZoneBounds zone in NoGoZoneManager.Zones)
        {
            repulsivePotential += ComputeNoGoZoneContribution(zone, entity.position);
        }
        repulsivePotential += ComputeTerrainRepulsion(entity.position);
        // Attraction to target
        Vector3 rawAttraction = movePosition - entity.position;
        attractivePotential = rawAttraction.normalized *
            AIMgr.inst.attractionCoefficient *
            Mathf.Pow(rawAttraction.magnitude, AIMgr.inst.attractiveExponent);

        potentialSum = attractivePotential - repulsivePotential;
        
        // Calculate final DHDS
        dh = Utils.Degrees360(Mathf.Rad2Deg * Mathf.Atan2(potentialSum.x, potentialSum.z));
        angleDiff = Utils.Degrees360(Utils.AngleDiffPosNeg(dh, entity.heading));
        cosValue = (Mathf.Cos(angleDiff * Mathf.Deg2Rad) + 1) / 2.0f;
        ds = entity.maxSpeed * cosValue;

        if (potentialLine != null)
        {
            potentialLine.SetPosition(0, entity.position);
            potentialLine.SetPosition(1, entity.position + potentialSum);
        }
        
        return new DHDS(dh, ds);
    }

    public DHDS ComputePF2() 
    {
        diffToMovePosition = movePosition - entity.position;
        repulsivePotential = Vector3.one;
        repulsivePotential.y = 0;

        // Entity repulsion
        foreach(Entity otherEnt in EntityMgr.inst.entities) 
        {
            if(otherEnt != entity && (entity.position - otherEnt.position).sqrMagnitude < AIMgr.inst.potentialDistanceThresholdSq) 
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
                    repulsivePotential += subPot.direction * otherEnt.mass * 0.05f
                        * AIMgr.inst.repulsive2Coefficient * Mathf.Pow(subPot.distance, AIMgr.inst.repulsiveExponent);
                }
            }
        }

        // No-Go Zone repulsion
        foreach (NoGoZoneBounds zone in NoGoZoneManager.Zones)
        {
            repulsivePotential += ComputeNoGoZoneContribution(zone, entity.position);
        }

        // Attraction to target
        Vector3 tmp = (movePosition - entity.position).normalized;
        attractivePotential = tmp 
            * AIMgr.inst.attraction2Coefficient * entity.mass * Mathf.Pow(diffToMovePosition.magnitude, AIMgr.inst.attractiveExponent);

        potentialSum = attractivePotential - repulsivePotential;
        dh = Utils.Degrees360(Mathf.Rad2Deg * Mathf.Atan2(potentialSum.x, potentialSum.z)); 
        angleDiff = Utils.Degrees360(Utils.AngleDiffPosNeg(dh, entity.heading));
        cosValue = (Mathf.Cos(angleDiff * Mathf.Deg2Rad) + 1) / 2.0f;
        ds = entity.maxSpeed * cosValue;
        
        return new DHDS(dh, ds);
    }
   // In Move.cs - Replace ComputeTerrainRepulsion
// In Move.cs
private Vector3 ComputeTerrainRepulsion(Vector3 shipPosition)
{
    Vector3 totalRepulsion = Vector3.zero;
    LayerMask terrainLayer = LayerMask.GetMask("Terrain");
    float detectionRadius = AIMgr.inst.terrainDetectionRadius;
    float maxRepel = AIMgr.inst.maxTerrainRepulsion;

    // 1. Broad-phase detection using sphere overlap
    Collider[] colliders = Physics.OverlapSphere(shipPosition, detectionRadius, terrainLayer);
    
    foreach (Collider col in colliders)
    {
        // 2. Narrow-phase using bounds approximation
        Vector3 closestBoundsPoint = col.bounds.ClosestPoint(shipPosition);
        Vector3 toShip = shipPosition - closestBoundsPoint;
        float distance = toShip.magnitude;

        if (distance > detectionRadius || distance < 0.1f) continue;

        // 3. Fine-tune with raycast to actual surface
        RaycastHit hit;
        if (Physics.Raycast(shipPosition, (closestBoundsPoint - shipPosition).normalized, 
                           out hit, detectionRadius, terrainLayer))
        {
            Vector3 surfaceNormal = hit.normal;
            float surfaceDistance = hit.distance;
            
            // 4. Calculate repulsion force
            float strength = Mathf.Clamp01(1 - (surfaceDistance / detectionRadius)) * maxRepel;
            Vector3 repelDir = Vector3.Reflect(-hit.normal, Vector3.up).normalized;
            repelDir.y = 0;

            totalRepulsion += repelDir * strength;
        }
    }

    // 5. Critical forward-looking rays
    // Vector3[] criticalDirections = {
    //     entity.transform.forward,
    //     entity.transform.forward + entity.transform.right * 0.5f,
    //     entity.transform.forward - entity.transform.right * 0.5f
    // };

    // foreach (Vector3 dir in criticalDirections)
    // {
    //     RaycastHit hit;
    //     if (Physics.Raycast(shipPosition, dir, out hit, detectionRadius, terrainLayer))
    //     {
    //         float strength = Mathf.Lerp(maxRepel, 0, hit.distance / detectionRadius);
    //         Vector3 evadeDir = Vector3.Cross(hit.normal, Vector3.up).normalized * Mathf.Sign(Vector3.Dot(-dir, hit.normal));
    //         totalRepulsion += evadeDir * strength;
    //     }
    // }

    return totalRepulsion.normalized * Mathf.Min(totalRepulsion.magnitude, maxRepel);
}
    private Vector3 ComputeNoGoZoneContribution(NoGoZoneBounds zone, Vector3 entityPosition)
    {
        Vector3 contribution = Vector3.zero;
        Vector3 closestPoint = zone.GetClosestPoint(entityPosition);
        Vector3 diff = closestPoint - entityPosition;
        float distance = diff.magnitude;

        if (zone.Contains(entityPosition))
        {
            Vector3 dirFromCenter = (entityPosition - zone.transform.position).normalized;
            contribution += dirFromCenter * zone.repulsionStrength;
        }
        else if (distance <= zone.repulsionRadius)
        {
            Vector3 dir = diff.normalized;
            float force = zone.repulsionStrength * (1 - (distance / zone.repulsionRadius));
            contribution += dir * force;
        }

        return contribution;
    }

    public override bool IsDone()
    {
        if (entity == null ) return true;
        return (entity.position - movePosition).sqrMagnitude < doneDistanceSq;
        
    }

    public override void Stop()
{
    entity.desiredSpeed = 0;
    pathWaypoints.Clear();
    currentWaypointIndex = 0;
    hasPath = false;
    // Calculate direction to face the target when stopping
    Vector3 direction = movePosition - entity.position;
    if (direction.sqrMagnitude > 0.001f) // Check to avoid zero direction
    {
        float dhRadians = Mathf.Atan2(direction.x, direction.z);
        float dhDegrees = Utils.Degrees360(Mathf.Rad2Deg * dhRadians);
        entity.desiredHeading = dhDegrees;
    }
    else
    {
        entity.desiredHeading = entity.heading;
    }

    LineMgr.inst.DestroyLR(line);
    LineMgr.inst.DestroyLR(potentialLine);
    line = null;
    potentialLine = null;
    base.Stop();
    pathWaypoints.Clear();
    currentWaypointIndex = 0;
    hasPath = false;
}
}