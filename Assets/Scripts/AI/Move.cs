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

    public Move(Entity ent, Vector3 pos, bool maxSpeedMovement = false, float doneDistanceSq = 1000f, bool isWaypoint = false) : base(ent)
    {
        movePosition = pos;
        useMaxSpeedMovement = maxSpeedMovement;
        this.isWaypoint = isWaypoint;
        if (doneDistanceSq > 0f)
        {
            this.doneDistanceSq = doneDistanceSq;
        }
    }

    public LineRenderer potentialLine;
    // Boundary avoidance settings are stored centrally on AIMgr.inst

    public override void Init()
    {
        pathUpdateTimer = 0f; // Ensure path is calculated on first tick
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

        if (pathUpdateTimer <= 0f)
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
        float targetSpeed = useMaxSpeedMovement ? entity.maxSpeed : entity.cruiseSpeed;
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
        ApplyObstacleRepulsion();
        // Boundary object repulsion
        ApplyBoundaryRepulsion();
        Vector3 tmp = diffToMovePosition.sqrMagnitude > 0.0001f
            ? diffToMovePosition.normalized
            : Vector3.zero;
        attractivePotential = tmp
            * AIMgr.inst.attraction2Coefficient * entity.mass * Mathf.Pow(diffToMovePosition.magnitude, AIMgr.inst.attractiveExponent);

        potentialSum = attractivePotential - repulsivePotential;

        dh = Utils.Degrees360(Mathf.Rad2Deg * Mathf.Atan2(potentialSum.x, potentialSum.z));

        angleDiff = Utils.Degrees360(Utils.AngleDiffPosNeg(dh, entity.heading));
        cosValue = (Mathf.Cos(angleDiff * Mathf.Deg2Rad) + 1) / 2.0f;
        float baseSpeed = useMaxSpeedMovement ? entity.maxSpeed : entity.cruiseSpeed;
        ds = isWaypoint ? baseSpeed : baseSpeed * cosValue;

        return new DHDS(dh, ds);
    }

void ApplyBoundaryRepulsion()
{
    var aimgr = AIMgr.inst;
        if (aimgr == null || aimgr.boundaryPositions == null || aimgr.boundaryPositions.Count == 0) return;
    Vector3 pos = entity.position;

        int boundaryCount = aimgr.boundaryPositions.Count;
        if (boundaryCount == 0) return;

        // Stagger the boundary checks over 5 frames to improve performance.
        int startIndex = Time.frameCount % 5;

        for (int i = startIndex; i < boundaryCount; i += 5)
    {
            Vector3 boundaryPos = aimgr.boundaryPositions[i];
        Vector3 dir = pos - boundaryPos;
        float dist = dir.magnitude;
        if (dist < 0.001f) dist = 0.001f;

            if (dist <= aimgr.boundaryRepulsionDistance)
            {
        Vector3 repDir = dir.normalized;
        // Use same exponent as entity repulsion for consistency
        float magnitude = float.MaxValue * aimgr.repulsive2Coefficient * Mathf.Pow(dist, aimgr.repulsiveExponent);
        magnitude *= aimgr.boundaryRepulsionStrength;
        // Scale by entity mass so heavier entities respond appropriately
        magnitude *= entity.mass;
        repulsivePotential += -repDir * magnitude;
            }
    }
}

    public float doneDistanceSq = 100f;

    public override bool IsDone()
    {
        float thresholdSq = doneDistanceSq;
        var weaponsAspect = entity.GetComponentInChildren<WeaponsAspect>();
        if (weaponsAspect != null && weaponsAspect.weapon != null && weaponsAspect.weapon.range > 0f)
        {
            float weaponRange = weaponsAspect.weapon.range;
            thresholdSq = weaponRange * weaponRange;
        }
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
  void ApplyObstacleRepulsion()
{
    var aimgr = AIMgr.inst;
    Vector3 entityPos = entity.position;
    int obstacleLayerMask = LayerMask.GetMask("Terrain");
    float rayLength = 500f;
    const int numRays = 36;
    const float angleStep = 360f / numRays;

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
                 // Adjust this value as needed
                float magnitude = aimgr.repulsive2Coefficient * Mathf.Pow(distance, aimgr.repulsiveExponent) * float.MaxValue;
                repulsivePotential += -repulsionDirection * magnitude;
            }
        }
    }
}
}