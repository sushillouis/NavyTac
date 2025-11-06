using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
[System.Serializable]
public class Move : Command
{
    public Vector3 movePosition;
    public float range;
    public float timeOnTarget;
    private readonly bool useMaxSpeedMovement;
    private readonly bool isWaypoint;
    protected float pathUpdateCooldown = 0.25f;
    private float pathUpdateTimer = 0f;
    private readonly float groupSpeed = -1f;
    public float doneDistanceSq = 100f * 100f;
    public Move(Entity ent, Vector3 pos, bool maxSpeedMovement = false, float doneDistanceSq = 1000f, bool isWaypoint = false, float groupSpeed = -1f) : base(ent)
    {
        movePosition = pos;
        useMaxSpeedMovement = maxSpeedMovement;
        this.isWaypoint = isWaypoint;
        this.groupSpeed = groupSpeed;
        if (doneDistanceSq > 0f)
        {
            this.doneDistanceSq = doneDistanceSq;
        }
    }
    public LineRenderer potentialLine;
   
    // Terrain avoidance parameters
    private float emergencyAvoidanceTimer = 0f;
    private const float EMERGENCY_AVOIDANCE_COOLDOWN = 0.05f;
    private Vector3 lastSafeDirection = Vector3.zero;
    public override void Init()
    {
        pathUpdateTimer = 0f;
        emergencyAvoidanceTimer = 0f;
        line = LineMgr.inst.CreateMoveLine(entity.position, movePosition, entity.isAI);
        if (line != null)
        {
            line.gameObject.SetActive(false);
        }
        potentialLine = LineMgr.inst.CreatePotentialLine(entity.position, entity.isAI);
        if (potentialLine != null)
        {
            potentialLine.gameObject.SetActive(false);
        }
    }
    public override void Tick()
    {
        pathUpdateTimer -= Time.deltaTime;
        emergencyAvoidanceTimer -= Time.deltaTime;
        // Emergency terrain avoidance (runs every frame)
        bool emergencyAvoidanceActive = CheckImmediateTerrainDanger();
        if (pathUpdateTimer <= 0f && !emergencyAvoidanceActive)
        {
            pathUpdateTimer = pathUpdateCooldown;
            DHDS dhds;
            if (AIMgr.inst.isPotentialFieldsMovement)
                dhds = ComputePF2(movePosition);
            else
                dhds = ComputeDHDS();
            entity.desiredHeading = dhds.dh;
            entity.desiredSpeed = dhds.ds;
        }
        if (line != null)
        {
            line.SetPosition(0, entity.position);
            line.SetPosition(1, movePosition);
        }
        if (potentialLine != null && AIMgr.inst.isPotentialFieldsMovement)
        {
            potentialLine.SetPosition(0, entity.position);
            potentialLine.SetPosition(1, entity.position + potentialSum);
        }
        range = diffToMovePosition.magnitude;
        timeOnTarget = entity.speed > 0.001f ? range / entity.speed : float.PositiveInfinity;
    }
    public Vector3 diffToMovePosition = Vector3.positiveInfinity;
    public float dhRadians;
    public float dhDegrees;
    public DHDS ComputeDHDS()
    {
        diffToMovePosition = movePosition - entity.position;
        dhRadians = Mathf.Atan2(diffToMovePosition.x, diffToMovePosition.z);
        dhDegrees = Utils.Degrees360(Mathf.Rad2Deg * dhRadians);
        potentialSum = Vector3.zero;
        repulsivePotential = Vector3.zero;
        attractivePotential = Vector3.zero;
        float targetSpeed;
        if (groupSpeed > 0)
        {
            targetSpeed = groupSpeed;
        }
        else
        {
            targetSpeed = useMaxSpeedMovement ? entity.maxSpeed : entity.cruiseSpeed;
        }
        return new DHDS(dhDegrees, targetSpeed);
    }
    public DHDS ComputePotentialDHDS(Vector3 movePosition)
    {
        diffToMovePosition = movePosition - entity.position;
        Potential p;
        repulsivePotential = Vector3.one;
        repulsivePotential.y = 0;
        foreach (Entity ent in EntityMgr.inst.entities)
        {
            if (ent == entity) continue;
            p = DistanceMgr.inst.GetPotential(entity, ent);
            if (p.distance < AIMgr.inst.potentialDistanceThreshold)
            {
                repulsivePotential += p.direction * ent.mass *
                    AIMgr.inst.repulsiveCoefficient * Mathf.Pow(p.diff.magnitude, AIMgr.inst.repulsiveExponent);
            }
        }
        attractivePotential = movePosition - entity.position;
        Vector3 tmp = attractivePotential.normalized;
        attractivePotential = tmp *
            AIMgr.inst.attractionCoefficient * Mathf.Pow(attractivePotential.magnitude, AIMgr.inst.attractiveExponent);
        potentialSum = attractivePotential - repulsivePotential;
        dh = Utils.Degrees360(Mathf.Rad2Deg * Mathf.Atan2(potentialSum.x, potentialSum.z));
        angleDiff = Utils.Degrees360(Utils.AngleDiffPosNeg(dh, entity.heading));
        cosValue = (Mathf.Cos(angleDiff * Mathf.Deg2Rad) + 1) / 2.0f;
        float baseSpeed = useMaxSpeedMovement ? entity.maxSpeed : entity.cruiseSpeed;
        ds = isWaypoint ? baseSpeed : baseSpeed * cosValue;
        return new DHDS(dh, ds);
    }
    public Vector3 attractivePotential = Vector3.zero;
    public Vector3 potentialSum = Vector3.zero;
    public Vector3 repulsivePotential = Vector3.zero;
    public float dh;
    public float angleDiff;
    public float cosValue;
    public float ds;
    public DHDS ComputePF2(Vector3 pos)
    {
        diffToMovePosition = pos - entity.position;
        repulsivePotential = Vector3.zero;
        Potential pot;
        // Enhanced terrain repulsion - call this FIRST for highest priority
        ApplyEnhancedTerrainRepulsion();
       
        // Entity Repulsion
        foreach (Entity otherEnt in EntityMgr.inst.entities)
        {
            if (otherEnt != entity && (entity.position - otherEnt.position).sqrMagnitude < AIMgr.inst.potentialDistanceThresholdSq)
            {
                if (entity.ai.potentialsD.ContainsKey(otherEnt))
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
                foreach (SubPotential subPot in pot.subPotentials)
                {
                    repulsivePotential += subPot.direction * otherEnt.mass
                        * AIMgr.inst.repulsive2Coefficient * Mathf.Pow(subPot.distance, AIMgr.inst.repulsiveExponent);
                }
            }
        }
        // Apply obstacle repulsion
        ApplyObstacleRepulsion();
       
        // Boundary object repulsion computed via DistanceMgr Burst job
        if (DistanceMgr.inst != null)
        {
            Vector3 boundaryRepulsion = DistanceMgr.inst.GetBoundaryRepulsion(entity);
            // Apply boundary repulsion with higher priority
            repulsivePotential += boundaryRepulsion * 2f;
        }
        Vector3 tmp = diffToMovePosition.sqrMagnitude > 0.0001f
            ? diffToMovePosition.normalized
            : Vector3.zero;
        attractivePotential = tmp
            * AIMgr.inst.attraction2Coefficient * entity.mass * Mathf.Pow(diffToMovePosition.magnitude, AIMgr.inst.attractiveExponent);
        potentialSum = attractivePotential - repulsivePotential;
        // Apply minimum repulsion force away from terrain
        ApplyMinimumTerrainAvoidance(ref potentialSum);

        // Safety correction loop to ensure direction doesn't lead into terrain
        int maxSafetyIters = 5;
        for (int safetyIter = 0; safetyIter < maxSafetyIters; safetyIter++)
        {
            if (potentialSum.sqrMagnitude < 0.0001f) break;
            Vector3 proposedDir = potentialSum.normalized;
            float safeDist = entity.speed * pathUpdateCooldown * 2f + AIMgr.inst.minSafeDistance;
            if (Physics.Raycast(entity.position, proposedDir, out RaycastHit hit, safeDist, LayerMask.GetMask("Terrain")))
            {
                if (hit.point.y > 0)
                {
                    float dangerLevel = 1f - (hit.distance / safeDist);
                    Vector3 emergencyAvoidance = -hit.normal * dangerLevel * AIMgr.inst.maxTerrainRepulsion;
                    potentialSum += emergencyAvoidance;
                    lastSafeDirection = Vector3.Cross(hit.normal, Vector3.up).normalized;
                    if (Vector3.Dot(lastSafeDirection, (movePosition - entity.position).normalized) < 0)
                        lastSafeDirection = -lastSafeDirection;
                }
                else
                {
                    break;
                }
            }
            else
            {
                break;
            }
        }

        // Final safety check
        bool isSafe = true;
        if (potentialSum.sqrMagnitude > 0.0001f)
        {
            Vector3 finalDir = potentialSum.normalized;
            float safeDist = entity.speed * pathUpdateCooldown * 2f + AIMgr.inst.minSafeDistance;
            if (Physics.Raycast(entity.position, finalDir, out RaycastHit safetyHit, safeDist, LayerMask.GetMask("Terrain")) && safetyHit.point.y > 0)
            {
                isSafe = false;
            }
        }

        if (!isSafe)
        {
            if (lastSafeDirection != Vector3.zero)
            {
                potentialSum = lastSafeDirection * attractivePotential.magnitude;
            }
            else
            {
                potentialSum = Vector3.up * AIMgr.inst.maxTerrainRepulsion;
            }
        }

        dh = Utils.Degrees360(Mathf.Rad2Deg * Mathf.Atan2(potentialSum.x, potentialSum.z));
        angleDiff = Utils.Degrees360(Utils.AngleDiffPosNeg(dh, entity.heading));
        cosValue = (Mathf.Cos(angleDiff * Mathf.Deg2Rad) + 1) / 2.0f;
        float baseSpeed = GetTerrainAwareSpeed(potentialSum.sqrMagnitude > 0.0001f ? potentialSum.normalized : (movePosition - entity.position).normalized);
       
        ds = isWaypoint ? baseSpeed : baseSpeed * cosValue;

        if (!isSafe) ds = 0f;

        return new DHDS(dh, ds);
    }
    void ApplyEnhancedTerrainRepulsion()
    {
        var aimgr = AIMgr.inst;
        Vector3 entityPos = entity.position;
       
        // Multiple proximity checks for better terrain awareness
        float[] checkDistances = { 30f, 75f, 150f, 300f, 500f }; // Close to far
        float[] weights = { 5f, 3f, 1.5f, 0.75f, 0.3f }; // Higher weight for closer terrain
       
        int terrainLayerMask = LayerMask.GetMask("Terrain");
       
        for (int i = 0; i < checkDistances.Length; i++)
        {
            float checkDistance = checkDistances[i];
            float weight = weights[i];
           
            // Check in movement direction
            Vector3 moveDirection = (movePosition - entityPos).normalized;
            if (Physics.Raycast(entityPos, moveDirection, out RaycastHit hit, checkDistance, terrainLayerMask))
            {
                if (hit.point.y > 0) // Only consider above-water terrain
                {
                    float avoidanceStrength = (1f - (hit.distance / checkDistance)) * weight;
                    Vector3 avoidanceDir = -hit.normal;
                    repulsivePotential += avoidanceDir * aimgr.repulsive2Coefficient * avoidanceStrength * entity.mass * 5f;
                }
            }
           
            // Additional check straight down for ground proximity
            if (Physics.Raycast(entityPos, Vector3.down, out RaycastHit groundHit, checkDistance, terrainLayerMask))
            {
                if (groundHit.point.y > 0 && groundHit.distance < checkDistance * 0.5f)
                {
                    float groundAvoidance = (1f - (groundHit.distance / (checkDistance * 0.5f))) * weight * 3f;
                    repulsivePotential += Vector3.up * aimgr.repulsive2Coefficient * groundAvoidance * entity.mass;
                }
            }
        }
    }
    void ApplyMinimumTerrainAvoidance(ref Vector3 potentialSum)
    {
        // Ensure there's always some minimal avoidance force
        Vector3 entityPos = entity.position;
        int terrainLayerMask = LayerMask.GetMask("Terrain");
        float emergencyDistance = AIMgr.inst.minSafeDistance * 2f;
        Vector3 checkDir = potentialSum.sqrMagnitude > 0.0001f ? potentialSum.normalized : (movePosition - entity.position).normalized;
       
        // Check for immediate terrain danger
        if (Physics.Raycast(entityPos, checkDir, out RaycastHit hit, emergencyDistance, terrainLayerMask))
        {
            if (hit.point.y > 0)
            {
                float dangerLevel = 1f - (hit.distance / emergencyDistance);
                Vector3 emergencyAvoidance = -hit.normal * dangerLevel * 20f; // Strong avoidance
                potentialSum += emergencyAvoidance;
               
                // Store safe direction for emergency use
                lastSafeDirection = Vector3.Cross(hit.normal, Vector3.up).normalized;
                if (Vector3.Dot(lastSafeDirection, (movePosition - entityPos).normalized) < 0)
                    lastSafeDirection = -lastSafeDirection;
            }
        }
    }
    float GetTerrainAwareSpeed(Vector3 proposedDir)
    {
        float baseSpeed = groupSpeed > 0 ? groupSpeed :
                         (useMaxSpeedMovement ? entity.maxSpeed : entity.cruiseSpeed);
       
        // Reduce speed when close to terrain
        Vector3 entityPos = entity.position;
        int terrainLayerMask = LayerMask.GetMask("Terrain");
        float slowDownDistance = AIMgr.inst.terrainDetectionRadius * 2f;
       
        if (Physics.Raycast(entityPos, proposedDir, out RaycastHit hit, slowDownDistance, terrainLayerMask))
        {
            if (hit.point.y > 0)
            {
                float slowFactor = Mathf.Clamp01(hit.distance / slowDownDistance);
                return baseSpeed * Mathf.Lerp(0.1f, 1f, slowFactor);
            }
        }
       
        return baseSpeed;
    }
    bool CheckImmediateTerrainDanger()
    {
        if (emergencyAvoidanceTimer > 0f) return false;
        Vector3 entityPos = entity.position;
        int terrainLayerMask = LayerMask.GetMask("Terrain");
        float immediateDangerDistance = 50f;
        bool dangerDetected = false;
       
        // Check multiple directions for immediate danger
        Vector3 velDir = entity.velocity.normalized;
        Vector3 moveDir = (movePosition - entityPos).normalized;
        Vector3[] checkDirections = {
            velDir,
            moveDir,
            Vector3.down,
            Quaternion.Euler(0, 45, 0) * velDir,
            Quaternion.Euler(0, -45, 0) * velDir,
            Quaternion.Euler(0, 45, 0) * moveDir,
            Quaternion.Euler(0, -45, 0) * moveDir
        };
       
        foreach (Vector3 dir in checkDirections)
        {
            if (Physics.Raycast(entityPos, dir, out RaycastHit hit, immediateDangerDistance, terrainLayerMask))
            {
                if (hit.point.y > 0)
                {
                    dangerDetected = true;
                   
                    // Immediate course correction
                    Vector3 escapeDirection = -hit.normal;
                   
                    // If normal is straight up, use last safe direction or calculate sideways escape
                    if (escapeDirection.y > 0.9f)
                    {
                        if (lastSafeDirection != Vector3.zero)
                            escapeDirection = lastSafeDirection;
                        else
                            escapeDirection = Vector3.Cross(hit.normal, Vector3.up).normalized;
                    }
                   
                    float escapeForce = (1f - (hit.distance / immediateDangerDistance)) * 20f;
                   
                    // Calculate emergency heading
                    float escapeHeading = Utils.Degrees360(Mathf.Rad2Deg * Mathf.Atan2(escapeDirection.x, escapeDirection.z));
                   
                    // Override desired heading and speed for immediate escape
                    entity.desiredHeading = escapeHeading;
                    entity.desiredSpeed *= 0.3f; // Slow down while escaping
                   
                    // Set cooldown and shorten path update for more responsive avoidance
                    emergencyAvoidanceTimer = EMERGENCY_AVOIDANCE_COOLDOWN;
                    pathUpdateTimer = Mathf.Min(pathUpdateTimer, 0.05f);
                   
                    // Debug visualization
                    if (potentialLine != null)
                    {
                        potentialLine.startColor = Color.red;
                        potentialLine.endColor = Color.red;
                    }
                   
                    break;
                }
            }
        }
        // Reset line color if no danger
        if (!dangerDetected && potentialLine != null)
        {
            potentialLine.startColor = entity.isAI ? Color.blue : Color.green;
            potentialLine.endColor = entity.isAI ? Color.blue : Color.green;
        }
        return dangerDetected;
    }
    void ApplyObstacleRepulsion()
    {
        var aimgr = AIMgr.inst;
        Vector3 entityPos = entity.position;
        int obstacleLayerMask = LayerMask.GetMask("Terrain");
        float rayLength = aimgr.terrainDetectionRadius;
        const int numRays = 72;
        const float angleStep = 360f / numRays;
        Vector3 movementDirection = (movePosition - entityPos).normalized;
       
        for (int i = 0; i < numRays; i++)
        {
            float currentAngle = i * angleStep;
            Vector3 rayDirection = Quaternion.Euler(0, currentAngle, 0) * Vector3.forward;
            if (Physics.Raycast(entityPos, rayDirection, out RaycastHit hit, rayLength, obstacleLayerMask))
            {
                if (hit.point.y <= 0) continue;
                float distance = hit.distance;
                if (distance > 0.01f)
                {
                    Vector3 repulsionDirection = hit.normal;
                   
                    // Weight rays in movement direction more heavily
                    float directionalWeight = Vector3.Dot(rayDirection, movementDirection);
                    directionalWeight = Mathf.Clamp01(directionalWeight + 0.5f);
                   
                    // Increase repulsion for closer obstacles
                    float proximityFactor = 1f - (distance / rayLength);
                    float magnitude = aimgr.repulsive2Coefficient *
                                     Mathf.Pow(proximityFactor, aimgr.repulsiveExponent) *
                                     entity.mass * directionalWeight * 2f;
                   
                    repulsivePotential += -repulsionDirection * magnitude;
                }
            }
        }
       
        // Additional check: if we're very close to terrain, apply strong upward force
        if (Physics.Raycast(entityPos, Vector3.down, out RaycastHit groundHit, 100f, obstacleLayerMask))
        {
            if (groundHit.point.y > 0 && groundHit.distance < 25f)
            {
                float emergencyForce = (1f - (groundHit.distance / 25f)) * 30f;
                repulsivePotential += Vector3.up * aimgr.repulsive2Coefficient * emergencyForce * entity.mass;
            }
        }
    }
    public override bool IsDone()
    {
        float thresholdSq = doneDistanceSq;
        return (entity.position - movePosition).sqrMagnitude < thresholdSq;
    }
    public override void Stop()
    {
        if (!isWaypoint)
        {
            entity.desiredSpeed = 0;
        }
       
        if (line != null)
        {
            LineMgr.inst.DestroyLR(line);
        }
        if (potentialLine != null)
        {
            LineMgr.inst.DestroyLR(potentialLine);
        }
        line = null;
        potentialLine = null;
    }
}
