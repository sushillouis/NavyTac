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

    // Optimization for ComputeTerrainRepulsion
    private static readonly int terrainMask;
    private Collider[] nearbyColliders = new Collider[32]; // Pre-allocate for terrain

    // Optimizations for entity repulsion
    private static readonly int entityLayerMask; // Layer mask for entities
    private Collider[] nearbyEntityColliders = new Collider[32]; // Pre-allocate for entities

    static Move()
    {
        terrainMask = 1 << LayerMask.NameToLayer("Terrain");
        // TODO: Ensure your entities are on a layer named "Entities" or change the layer name here.
        // If entities are on multiple layers, or you need a more complex mask, adjust accordingly.
        // Example: entityLayerMask = LayerMask.GetMask("Entities", "LargeEntities");
        // Example: entityLayerMask = ~(terrainMask); // All layers except terrain
        int mask = LayerMask.GetMask("Entities");
        if (mask == 0) {
            Debug.LogWarning("Move.cs: 'Entities' layer not found or empty. Entity repulsion might not work correctly. Please configure layers.");
            // Fallback to default layer or all layers if appropriate, e.g., LayerMask.NameToLayer("Default") or -1
            // For safety, using a mask that likely includes entities if "Entities" layer is not set up:
            entityLayerMask = ~(terrainMask | (1 << LayerMask.NameToLayer("Ignore Raycast")) | (1 << LayerMask.NameToLayer("Water"))); // Example: Exclude terrain, ignore raycast, water
        } else {
            entityLayerMask = mask;
        }

    }

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
            pathWaypoints.Clear(); // Reuse existing list
            pathWaypoints.AddRange(path); // Add new path waypoints
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
        if (!hasPath && (pathWaypoints == null || pathWaypoints.Count == 0)) // Ensure path is requested if empty
        {
            RequestNewPath();
        }

        if (hasPath && currentWaypointIndex < pathWaypoints.Count)
        {
            FollowPath();
        }
        else
        {
            // If path becomes invalid or finishes, can request new or switch to potential fields
            if (hasPath && currentWaypointIndex >= pathWaypoints.Count) {
                 hasPath = false; // Path finished or invalid
                 RequestNewPath(); // Attempt to get a new path to final destination
            }
            UsePotentialFields();
        }
    }

    private void FollowPath()
    {
        Vector3 currentWaypoint = pathWaypoints[currentWaypointIndex];
        
        // The A* path provides global guidance (the "flow field").
        // ComputePotentialDHDS uses the current A* waypoint as the attractive target.
        // The repulsive forces within ComputePotentialDHDS act as the "collision-avoidance delta."
        // This combination helps avoid local minima common in pure potential field navigation.
        DHDS dhds = ComputePotentialDHDS(currentWaypoint); 

        entity.desiredHeading = dhds.dh;
        entity.desiredSpeed = dhds.ds;

        float distanceToWaypointSq = (entity.position - currentWaypoint).sqrMagnitude;
        float waypointThresholdSq = waypointThreshold * waypointThreshold;

        // Progress check: compare with previous distance
        // Note: Using sqrMagnitude for previousDistanceToWaypoint would complicate this check.
        // Sticking to linear distance for clarity here as it's not the primary N^2 bottleneck.
        float currentLinearDistanceToWaypoint = Mathf.Sqrt(distanceToWaypointSq);

        if (currentLinearDistanceToWaypoint >= previousDistanceToWaypoint - waypointThreshold * 0.1f)
        {
            stuckFrames++;
        }
        else
        {
            stuckFrames = Mathf.Max(0, stuckFrames - 1); // Decrease stuck frames if making progress
        }
        previousDistanceToWaypoint = currentLinearDistanceToWaypoint;
        
        Vector3 toWaypointDir = (currentWaypoint - entity.position).normalized;
        Vector3 potentialDir = potentialSum.normalized; // potentialSum is calculated in ComputePotentialDHDS
        float directionDot = Vector3.Dot(toWaypointDir, potentialDir);

        if (directionDot < 0.3f) // Potential fields are strongly opposing path direction
        {
            stuckFrames += 2; 
        }

        if (stuckFrames >= maxStuckFrames)
        {
            currentWaypointIndex++;
            stuckFrames = 0;
            previousDistanceToWaypoint = Mathf.Infinity;
            if (currentWaypointIndex < pathWaypoints.Count)
            {
                // Still more waypoints, continue
            }
            else
            {
                hasPath = false; // Reached end of path or path became too short
                RequestNewPath(); // Request new path to final destination
            }
            return;
        }

        if (distanceToWaypointSq < waypointThresholdSq)
        {
            currentWaypointIndex++;
            stuckFrames = 0;
            previousDistanceToWaypoint = Mathf.Infinity;
            if (currentWaypointIndex >= pathWaypoints.Count)
            {
                 hasPath = false; // Reached end of path
                 RequestNewPath(); // Request new path to final destination
            }
        }

        // Periodic path validation/refresh (optional, can be tuned)
        if (hasPath && currentWaypointIndex > 0 && currentWaypointIndex < pathWaypoints.Count)
        {
            float segmentProgress = (float)currentWaypointIndex / pathWaypoints.Count;
            if (segmentProgress > 0.7f && Time.time > lastPathUpdateTime + pathUpdateCooldown * 2) // More aggressive refresh if near end
            {
                RequestNewPath();
            }
        }
    }

    private void UsePotentialFields()
    {
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

    public virtual DHDS ComputePotentialDHDS(Vector3 targetPosition) // Renamed movePosition to targetPosition for clarity
    {
        diffToMovePosition = targetPosition - entity.position; // Use targetPosition
        repulsivePotential = Vector3.zero;
        attractivePotential = Vector3.zero;
        potentialSum = Vector3.zero;

        // Entity repulsion using spatial query
        float actualPotentialDistanceThreshold = AIMgr.inst.potentialDistanceThreshold;
        float potentialDistanceThresholdSq = actualPotentialDistanceThreshold * actualPotentialDistanceThreshold;
        
        int numFoundEntities = Physics.OverlapSphereNonAlloc(entity.position, actualPotentialDistanceThreshold, nearbyEntityColliders, entityLayerMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < numFoundEntities; i++)
        {
            Entity ent = nearbyEntityColliders[i].GetComponent<Entity>(); // Assuming Entity component is on the collider's GameObject

            if (ent == null || ent == entity || ent.entityClass == EntityClass.Missile) continue;
            
            Vector3 diffToEnt = ent.position - entity.position;
            float sqrDistToEnt = diffToEnt.sqrMagnitude;

            if (sqrDistToEnt < potentialDistanceThresholdSq && sqrDistToEnt > 0.0001f) 
            {
                float distToEnt = Mathf.Sqrt(sqrDistToEnt);
                Vector3 dirToEnt = diffToEnt / distToEnt; 

                // -dirToEnt results in a vector pointing from 'ent' towards 'entity', i.e., a push-away direction.
                repulsivePotential += -dirToEnt * ent.mass * 
                    AIMgr.inst.repulsiveCoefficient * 
                    Mathf.Pow(distToEnt, AIMgr.inst.repulsiveExponent);
            }
        }

        // No-Go Zone repulsion
        foreach (NoGoZoneBounds zone in NoGoZoneManager.Zones)
        {
            repulsivePotential += ComputeNoGoZoneContribution(zone, entity.position);
        }
        repulsivePotential += ComputeTerrainRepulsion(entity.position);

        // Attraction to target
        Vector3 rawAttraction = targetPosition - entity.position; // Use targetPosition
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
        
        // Repulsive potential is calculated as a push-away vector.
        // So, it should be added to the attractive potential.
        potentialSum = attractivePotential + repulsivePotential; 
        
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

    public DHDS ComputePF2() 
    {
        diffToMovePosition = movePosition - entity.position;
        repulsivePotential = Vector3.one; 
        repulsivePotential.y = 0; 

        float detectionRadius = Mathf.Sqrt(AIMgr.inst.potentialDistanceThresholdSq); 
        int numFoundEntities = Physics.OverlapSphereNonAlloc(entity.position, detectionRadius, nearbyEntityColliders, entityLayerMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < numFoundEntities; i++)
        {
            Entity otherEnt = nearbyEntityColliders[i].GetComponent<Entity>();
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
                    // Assuming subPot.direction is a push-away vector (oriented to push 'entity' away from 'otherEnt').
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
        Vector3 tmp = (movePosition - entity.position); 
        float magDiffToMovePos = tmp.magnitude;
        if (magDiffToMovePos > 0.001f)
        {
             attractivePotential = (tmp / magDiffToMovePos) 
                * AIMgr.inst.attraction2Coefficient * entity.mass * Mathf.Pow(magDiffToMovePos, AIMgr.inst.attractiveExponent);
        } else {
            attractivePotential = Vector3.zero;
        }
       
        // If repulsivePotential accumulates push-away vectors (as assumed from subPot.direction's role),
        // it should be added to attractivePotential, not subtracted.
        potentialSum = attractivePotential + repulsivePotential; // Changed from subtraction to addition
        dh = Utils.Degrees360(Mathf.Rad2Deg * Mathf.Atan2(potentialSum.x, potentialSum.z)); 
        angleDiff = Utils.Degrees360(Utils.AngleDiffPosNeg(dh, entity.heading));
        cosValue = (Mathf.Cos(angleDiff * Mathf.Deg2Rad) + 1) / 2.0f;
        ds = entity.maxSpeed * cosValue;
        
        return new DHDS(dh, ds);
    }

    private Vector3 ComputeTerrainRepulsion(Vector3 shipPosition)
    {
        Vector3 totalRepulsion = Vector3.zero;
        float detectionRadius = AIMgr.inst.terrainDetectionRadius;
        float maxRepel = AIMgr.inst.maxTerrainRepulsion;

        int numColliders = Physics.OverlapSphereNonAlloc(shipPosition, detectionRadius, nearbyColliders, terrainMask, QueryTriggerInteraction.Collide);
        
        for (int i = 0; i < numColliders; i++)
        {
            Collider col = nearbyColliders[i];
            Vector3 closestBoundsPoint = col.bounds.ClosestPoint(shipPosition);
            Vector3 toShip = shipPosition - closestBoundsPoint;
            float distanceToBounds = toShip.magnitude; 

            if (distanceToBounds > detectionRadius || distanceToBounds < 0.01f) continue; 

            RaycastHit hit;
            if (Physics.Raycast(shipPosition, (closestBoundsPoint - shipPosition).normalized, 
                               out hit, detectionRadius, terrainMask))
            {
                float strength = Mathf.Clamp01(1 - (hit.distance / detectionRadius)) * maxRepel;
                
                Vector3 repelDir = hit.normal; 
                repelDir.y = 0; 
                                
                if (repelDir.sqrMagnitude < 0.001f && hit.normal.y > 0.99f) { 
                    Vector3 entityForwardPlanar = entity.transform.forward;
                    entityForwardPlanar.y = 0;
                    repelDir = Vector3.Reflect(entityForwardPlanar.normalized, hit.normal);
                    repelDir.y = 0;
                }
                
                if (repelDir.sqrMagnitude > 0.001f) {
                    totalRepulsion += repelDir.normalized * strength;
                }
            }
        }

        if (totalRepulsion.sqrMagnitude > maxRepel * maxRepel)
        {
            totalRepulsion = totalRepulsion.normalized * maxRepel;
        }
        return totalRepulsion;
    }

    private Vector3 ComputeNoGoZoneContribution(NoGoZoneBounds zone, Vector3 entityPosition)
    {
        Vector3 contribution = Vector3.zero;
        Vector3 closestPoint = zone.GetClosestPoint(entityPosition);
        Vector3 diff = entityPosition - closestPoint; 
        
        if (zone.Contains(entityPosition))
        {
            Vector3 dirFromCenter = (entityPosition - zone.transform.position).normalized;
            dirFromCenter.y = 0; 
            if (dirFromCenter.sqrMagnitude < 0.001f) dirFromCenter = Vector3.forward; 
            contribution += dirFromCenter.normalized * zone.repulsionStrength; 
        }
        else
        {
            float sqrDistance = diff.sqrMagnitude;
            float repulsionRadiusSq = zone.repulsionRadius * zone.repulsionRadius;
            if (sqrDistance <= repulsionRadiusSq && sqrDistance > 0.0001f) 
            {
                float distance = Mathf.Sqrt(sqrDistance);
                Vector3 dir = diff / distance; 
                dir.y = 0; 
                if (dir.sqrMagnitude < 0.001f) dir = (entityPosition - zone.transform.position).normalized; 
                dir.y = 0;
                if (dir.sqrMagnitude < 0.001f) dir = Vector3.forward;


                float force = zone.repulsionStrength * (1 - (distance / zone.repulsionRadius));
                contribution += dir.normalized * force;
            }
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
        
        Vector3 directionToTarget = movePosition - entity.position;
        if (directionToTarget.sqrMagnitude > 0.001f) 
        {
            float targetDhRadians = Mathf.Atan2(directionToTarget.x, directionToTarget.z);
            entity.desiredHeading = Utils.Degrees360(Mathf.Rad2Deg * targetDhRadians);
        }

        if (line != null) LineMgr.inst.DestroyLR(line);
        if (potentialLine != null) LineMgr.inst.DestroyLR(potentialLine);
        line = null;
        potentialLine = null;
        
        pathWaypoints.Clear();
        currentWaypointIndex = 0;
        hasPath = false;
        lastPathUpdateTime = -Mathf.Infinity; 
        stuckFrames = 0;
        previousDistanceToWaypoint = Mathf.Infinity;

        base.Stop(); 
    }
}
