
    using UnityEngine;
    using System.Collections.Generic;
    using System.Linq; // Required for List.LastOrDefault

    [System.Serializable]
    public class Move : Command
    {
        // Pathfinding State Enum
        
        private PathfindingState currentState = PathfindingState.RequestingPath;

        // Existing potential field variables
        public Vector3 movePosition;
        public bool maxSpeedMovement;
        public float range; // Not explicitly used in provided snippet, but kept
        public float timeOnTarget; // Not explicitly used, but kept
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

        // Stuck detection
        private int stuckFrames = 0;
        private const int maxStuckFrames = 60; 
        private const float MinVelocityThresholdSq = 0.25f * 0.25f; 
        private float previousDistanceToWaypoint = Mathf.Infinity; 

        // A* Pathfinding integration
        private List<Vector3> pathWaypoints = new List<Vector3>();
        private int currentWaypointIndex = 0;
        private const float waypointThreshold = 100f; 
        public float pathUpdateCooldown = 1f;
        private float lastPathUpdateTime = -Mathf.Infinity;

        // Optimization for combined physics queries
        private Collider[] combinedQueryResults = new Collider[64]; // Buffer for combined entity and terrain queries

        // Optimizations for entity repulsion (original, can be removed if combinedQueryResults is always used)
        // private Collider[] nearbyEntityColliders = new Collider[32]; 

        // Static layer masks, initialized in static constructor
        private static readonly int terrainMask; // Still used by PrunePath and potentially ComputeSingleTerrainColliderRepulsion if it's used elsewhere
        private static readonly int entityLayerMask; 

        // --- Potential Field Tuning Parameters ---
        private Vector3 previousPotentialSum = Vector3.zero;
        public static float MaxTotalRepulsiveForceMagnitude = 20.0f; 
        public static float PotentialSumDampingFactor = 0.5f;     

        static Move()
        {
            terrainMask = 1 << LayerMask.NameToLayer("Terrain");
            int mask = LayerMask.GetMask("Entities");
            if (mask == 0) {
                //Debug.LogWarning("Move.cs: 'Entities' layer not found or empty. Entity repulsion might not work correctly. Please configure layers.");
                // Fallback: repel anything not terrain, ignore raycast, or water. This might be too broad.
                entityLayerMask = ~(terrainMask | (1 << LayerMask.NameToLayer("Ignore Raycast")) | (1 << LayerMask.NameToLayer("Water")));
            } else {
                entityLayerMask = mask;
            }
        }

        public Move(Entity ent, Vector3 pos, bool maxSpeedMovement = false, float doneDistanceSq = 100000f) : base(ent)
        {
            movePosition = pos;
            this.maxSpeedMovement = maxSpeedMovement;
            this.doneDistanceSq = doneDistanceSq;
            
            //Debug.Log("Move.cs: doneDistanceSq set to " + this.doneDistanceSq);
            // if (ent.GetComponentInChildren<WeaponsAspect>() != null)
            // {
            //     this.doneDistanceSq = AIMgr.inst.StoppingDistanceSq(ent.entityType);
            //     //Debug.Log("Move.cs: doneDistanceSq set to " + this.doneDistanceSq);
            // }
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

            if (!FogWarMgr.inst.nonRevelers.Contains(entity))
            {
                // Assuming LineMgr handles pooling. If not, it should be modified to do so.
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
                } else {
                    currentState = PathfindingState.PotentialFieldsOnly;
                }
            }
            else
            {
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

            switch (currentState)
            {
                case PathfindingState.RequestingPath:
                    RequestNewPath();
                    UsePotentialFields(movePosition); 
                    break;
                case PathfindingState.FollowingPath:
                    if (pathWaypoints.Count == 0 || currentWaypointIndex >= pathWaypoints.Count) {
                        currentState = PathfindingState.RequestingPath;
                        UsePotentialFields(movePosition);
                    } else {
                        FollowPath();
                    }
                    break;
                case PathfindingState.PotentialFieldsOnly:
                    UsePotentialFields(movePosition);
                    break;
            }
        }

        private void FollowPath()
        {
            if (currentWaypointIndex >= pathWaypoints.Count)
            {
                currentState = PathfindingState.RequestingPath; 
                return;
            }

            Vector3 currentWaypoint = pathWaypoints[currentWaypointIndex];
            
            DHDS dhds = ComputePotentialDHDS(currentWaypoint); 
            entity.desiredHeading = dhds.dh;
            entity.desiredSpeed = dhds.ds;

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
                float currentLinearDist = Mathf.Sqrt(distanceToWaypointSq); // Sqrt kept for specific stuck logic
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
                return; 
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
        }

        private void UsePotentialFields(Vector3 target) 
        {
            DHDS dhds = AIMgr.inst.isPotentialFieldsMovement ? 
                       ComputePotentialDHDS(target) : 
                       ComputeDHDS(target); 

            entity.desiredHeading = dhds.dh;
            entity.desiredSpeed = dhds.ds;
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
                float jitterStrength = 50.0f; 
                Vector3 jitterOffset = new Vector3(
                Random.Range(-jitterStrength, jitterStrength),
                0f, 
                Random.Range(-jitterStrength, jitterStrength)
                );
                targetPosition += jitterOffset;
                
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

                foreach (NoGoZoneBounds zone in NoGoZoneManager.Zones)
                {
                repulsivePotential += ComputeNoGoZoneContribution(zone, entity.position);
                }

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
            int numRays = 16; // Increase for better coverage

            for (int i = 0; i < numRays; i++)
            {
                float angle = i * (360f / numRays);
                Vector3 rayDir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                rayDir.y = 0; // Ensure rays are horizontal

                RaycastHit hit;
                // Assuming AIMgr.inst.terrainLayerMask is correctly defined and accessible
                if (Physics.Raycast(
                    shipPosition, 
                    rayDir, 
                    out hit, 
                    detectionRadius, 
                    AIMgr.inst.terrainLayerMask // Using the layer mask from AIMgr as per your method
                ))
                {
                    if (hit.point.y < 0) // Assuming y < 0 is below sea level
                    {
                        // //Debug.Log($"Skipping ray {i}: terrain below sea level."); // Optional //Debug
                        continue;
                    }

                    // Skip flat terrain (adjust threshold as needed)
                    // if (hit.normal.y > 0.9f)
                    // {
                    //     //Debug.Log($"Skipping ray {i}: flat terrain."); // Optional //Debug
                    //     continue;
                    // }

                    float strength = Mathf.Clamp01(1 - (hit.distance / detectionRadius)) * maxRepel;

                    // Directly use inverse of ray direction for more accuracy
                    Vector3 repelDir = -rayDir.normalized; // Repel away from the terrain hit
                
                    totalRepulsion += repelDir * strength;
                
                    // Visualize both the ray and repel direction (optional)
                    //Debug.DrawRay(shipPosition, rayDir * hit.distance, Color.red, 1f);
                    //Debug.DrawRay(shipPosition + repelDir * strength, -repelDir * strength, Color.green, 1f); // Corrected visualization for repel vector
                }
            }

            return totalRepulsion; 
        }
        
        public DHDS ComputePF2() 
        {
            // This method is not being refactored as part of this request,
            // but if it becomes performance critical, it could benefit from similar combined queries
            // if it also needed to consider terrain or other physics layers simultaneously.
            // For now, it retains its original Physics.OverlapSphereNonAlloc call for entities.
            // It uses 'nearbyEntityColliders' which is now commented out as 'combinedQueryResults' is preferred for the main logic.
            // If ComputePF2 is actively used, 'nearbyEntityColliders' should be reinstated or ComputePF2 adapted.
            // For this exercise, I'm assuming ComputePotentialDHDS is the primary focus.
            // To make this compile, I'll temporarily use combinedQueryResults for ComputePF2 as well,
            // but this might not be its intended optimal usage without further context on PF2.
            Collider[] tempEntityColliders = new Collider[32]; // Temporary for ComputePF2 if it's still used

            diffToMovePosition = movePosition - entity.position; 
            Vector3 tmp = (movePosition - entity.position); 
            
            Vector3 currentRepulsivePotential = Vector3.one; 
            currentRepulsivePotential.y = 0; 

            float detectionRadius = Mathf.Sqrt(AIMgr.inst.potentialDistanceThresholdSq); 
            int numFoundEntities = Physics.OverlapSphereNonAlloc(entity.position, detectionRadius, tempEntityColliders /*nearbyEntityColliders*/, entityLayerMask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < numFoundEntities; i++)
            {
                Entity otherEnt = tempEntityColliders[i].GetComponent<Entity>(); // nearbyEntityColliders[i]
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
            foreach (NoGoZoneBounds zone in NoGoZoneManager.Zones) {
                currentRepulsivePotential += ComputeNoGoZoneContribution(zone, entity.position);
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


        private Vector3 ComputeNoGoZoneContribution(NoGoZoneBounds zone, Vector3 entityPosition)
        {
            Vector3 contribution = Vector3.zero;
            Vector3 closestPoint = zone.GetClosestPoint(entityPosition);
            Vector3 diff = entityPosition - closestPoint; 
            if (zone.Contains(entityPosition)) {
                Vector3 dirFromCenter = (entityPosition - zone.transform.position).normalized;
                dirFromCenter.y = 0; 
                if (dirFromCenter.sqrMagnitude < 0.001f) dirFromCenter = Vector3.forward; 
                contribution += dirFromCenter.normalized * zone.repulsionStrength; 
            } else {
                float sqrDistance = diff.sqrMagnitude;
                float repulsionRadiusSq = zone.repulsionRadius * zone.repulsionRadius;
                if (sqrDistance <= repulsionRadiusSq && sqrDistance > 0.0001f) {
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
            if (entity == null ) {
                if(currentState != PathfindingState.Finished) currentState = PathfindingState.Finished;
                return true;
            }
            bool done = (entity.position - movePosition).sqrMagnitude < doneDistanceSq;
            return done;
        }

        public override void Stop()
            {
                if (entity != null) {
                // Make the entity face the movePosition
                Vector3 directionToTarget = movePosition - entity.position;
                if (directionToTarget.sqrMagnitude > 0.001f) // Check to avoid issues if already at the target
                {
                    float desiredHeadingDegrees = Utils.Degrees360(Mathf.Atan2(directionToTarget.x, directionToTarget.z) * Mathf.Rad2Deg);
                    entity.desiredHeading = desiredHeadingDegrees;
                }
                entity.desiredSpeed = 0;
                }

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
            }
    }
    
     