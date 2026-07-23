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
    protected float pathUpdateTimer = 0f;
    private readonly float groupSpeed = -1f;
    public float doneDistanceSq = 100f * 100f;
    public Color? lineColorOverride = null;

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

    // ───────────────── Terrain detection config ──────────────────
    private int terrainLayerMask;

    // How far above the entity Y the terrain surface must be to count as land
    private const float LAND_MARGIN = 5f;
    // How far ahead to sample terrain for steering
    private const float LOOKAHEAD_DISTANCE = 80f;
    // Number of directions to sample around the entity
    private const int STEER_SAMPLES = 12;

    public override void Init()
    {
        pathUpdateTimer = 0f;
        terrainLayerMask = LayerMask.GetMask("Terrain");

        line = LineMgr.inst.CreateMoveLine(entity.position, movePosition, entity.isAI);
        if (line != null)
        {
            if (lineColorOverride.HasValue)
            {
                line.startColor = lineColorOverride.Value;
                line.endColor = lineColorOverride.Value;
            }
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

        // ── HARD CONSTRAINT: runs every frame, prevents terrain penetration ──
        EnforceTerrainConstraint();

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

    // ═════════════════════════════════════════════════════════════
    //  TERRAIN DETECTION
    //  Uses a single downward raycast from high above.
    //  Works regardless of terrain Y offset (your terrain is at
    //  ~800Y while ships are at Y=0 — this handles that correctly).
    // ═════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns true if terrain surface at this XZ is above the ship's Y level.
    /// One raycast per call — use sparingly per frame.
    /// </summary>
    private bool IsLand(Vector3 worldPos)
    {
        Vector3 rayOrigin = new Vector3(worldPos.x, 10000f, worldPos.z);

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 20000f, terrainLayerMask))
        {
            // Terrain surface is above the ship → it's land
            return hit.point.y > entity.position.y + LAND_MARGIN;
        }

        return false; // No terrain hit = open water
    }

    /// <summary>
    /// Returns the terrain surface Y at this XZ, or -9999 if no terrain.
    /// </summary>
    private float GetTerrainSurfaceY(Vector3 worldPos)
    {
        Vector3 rayOrigin = new Vector3(worldPos.x, 10000f, worldPos.z);

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 20000f, terrainLayerMask))
        {
            return hit.point.y;
        }

        return -9999f;
    }

    // ═════════════════════════════════════════════════════════════
    //  HARD CONSTRAINT — every frame, prevent ships from being on land
    // ═════════════════════════════════════════════════════════════

    private void EnforceTerrainConstraint()
    {
        Vector3 pos = entity.position;

        // ── Case 1: Ship is ON land → find nearest water direction and escape ──
        if (IsLand(pos))
        {
            Vector3 bestEscape = Vector3.zero;
            float lowestHeight = float.MaxValue;

            for (int i = 0; i < STEER_SAMPLES; i++)
            {
                float angle = i * (360f / STEER_SAMPLES);
                Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                Vector3 samplePos = pos + dir * 30f;

                float surfaceY = GetTerrainSurfaceY(samplePos);

                if (surfaceY < lowestHeight)
                {
                    lowestHeight = surfaceY;
                    bestEscape = dir;
                }
            }

            if (bestEscape != Vector3.zero)
            {
                float escapeHeading = Utils.Degrees360(Mathf.Rad2Deg * Mathf.Atan2(bestEscape.x, bestEscape.z));
                entity.desiredHeading = escapeHeading;
                entity.desiredSpeed = entity.maxSpeed * 0.5f;
            }
            else
            {
                entity.desiredSpeed = 0f;
            }
            return;
        }

        // ── Case 2: Ship heading toward land → slow down ──
        Vector3 velDir = entity.velocity.sqrMagnitude > 0.01f
            ? entity.velocity.normalized
            : Quaternion.Euler(0, entity.heading, 0) * Vector3.forward;

        float aheadDist = Mathf.Max(entity.speed * 0.5f, 30f);
        Vector3 aheadPos = pos + velDir * aheadDist;

        if (IsLand(aheadPos))
        {
            entity.desiredSpeed *= 0.3f;
        }
    }

    // ═════════════════════════════════════════════════════════════
    //  TERRAIN REPULSION FOR POTENTIAL FIELDS
    // ═════════════════════════════════════════════════════════════

    private Vector3 ComputeTerrainRepulsion()
    {
        Vector3 pos = entity.position;
        Vector3 repulsion = Vector3.zero;
        float shipY = pos.y;

        float[] distances = { 40f, LOOKAHEAD_DISTANCE };
        float[] weights = { 3f, 1f };

        for (int d = 0; d < distances.Length; d++)
        {
            float dist = distances[d];
            float weight = weights[d];

            for (int i = 0; i < STEER_SAMPLES; i++)
            {
                float angle = i * (360f / STEER_SAMPLES);
                Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                Vector3 samplePos = pos + dir * dist;

                float surfaceY = GetTerrainSurfaceY(samplePos);
                if (surfaceY > shipY + LAND_MARGIN)
                {
                    float elevationAboveWater = surfaceY - shipY;
                    float strength = Mathf.Clamp01(elevationAboveWater / 50f) * weight;
                    repulsion -= dir * strength;
                }
            }
        }

        return repulsion;
    }

    private Vector3 FindSlideDirection(Vector3 pos, Vector3 toTarget)
    {
        Vector3 left = Quaternion.Euler(0, -90, 0) * toTarget;
        Vector3 right = Quaternion.Euler(0, 90, 0) * toTarget;

        float leftScore = 0f;
        float rightScore = 0f;

        for (int i = 1; i <= 3; i++)
        {
            float dist = i * 30f;
            if (!IsLand(pos + left * dist)) leftScore += 1f;
            if (!IsLand(pos + right * dist)) rightScore += 1f;
        }

        if (Mathf.Approximately(leftScore, rightScore))
        {
            return Vector3.Dot(left, toTarget) > Vector3.Dot(right, toTarget) ? left : right;
        }

        return leftScore > rightScore ? left : right;
    }

    // ═════════════════════════════════════════════════════════════
    //  MOVEMENT COMPUTATION
    // ═════════════════════════════════════════════════════════════

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
            targetSpeed = groupSpeed;
        else
            targetSpeed = useMaxSpeedMovement ? entity.maxSpeed : entity.cruiseSpeed;

        return new DHDS(dhDegrees, targetSpeed);
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

        // ── 1. Terrain repulsion ──
        Vector3 terrainRepulsion = ComputeTerrainRepulsion();
        repulsivePotential += terrainRepulsion * AIMgr.inst.repulsive2Coefficient * entity.mass;

        // ── 2. Entity repulsion ──
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

        // ── 3. Boundary repulsion ──
        if (DistanceMgr.inst != null)
        {
            Vector3 boundaryRepulsion = DistanceMgr.inst.GetBoundaryRepulsion(entity);
            repulsivePotential = Vector3.Lerp(repulsivePotential, repulsivePotential + boundaryRepulsion * 2f, 0.5f);
        }

        // ── 4. Attractive potential toward target ──
        Vector3 tmp = diffToMovePosition.sqrMagnitude > 0.0001f
            ? diffToMovePosition.normalized
            : Vector3.zero;
        attractivePotential = tmp
            * AIMgr.inst.attraction2Coefficient * entity.mass * Mathf.Pow(diffToMovePosition.magnitude, AIMgr.inst.attractiveExponent);

        // ── 5. Combine ──
        potentialSum = attractivePotential - repulsivePotential;

        // ── 6. If still heading into land, slide along coast ──
        if (potentialSum.sqrMagnitude > 0.0001f)
        {
            Vector3 proposedDir = potentialSum.normalized;
            Vector3 checkPos = entity.position + proposedDir * LOOKAHEAD_DISTANCE * 0.5f;

            if (IsLand(checkPos))
            {
                Vector3 toTarget = (movePosition - entity.position).normalized;
                Vector3 slideDir = FindSlideDirection(entity.position, toTarget);
                potentialSum = slideDir * potentialSum.magnitude;
            }
        }

        // ── 7. Heading & speed ──
        dh = Utils.Degrees360(Mathf.Rad2Deg * Mathf.Atan2(potentialSum.x, potentialSum.z));
        angleDiff = Utils.Degrees360(Utils.AngleDiffPosNeg(dh, entity.heading));
        cosValue = (Mathf.Cos(angleDiff * Mathf.Deg2Rad) + 1) / 2.0f;

        float baseSpeed = groupSpeed > 0 ? groupSpeed :
                          (useMaxSpeedMovement ? entity.maxSpeed : entity.cruiseSpeed);

        // Slow down if heading toward land
        Vector3 aheadPos = entity.position + (potentialSum.sqrMagnitude > 0.0001f ? potentialSum.normalized : tmp) * 40f;
        if (IsLand(aheadPos))
        {
            float surfaceY = GetTerrainSurfaceY(aheadPos);
            float elevation = surfaceY - entity.position.y;
            float slowFactor = 1f - Mathf.Clamp01(elevation / 50f);
            baseSpeed *= Mathf.Lerp(0.15f, 1f, slowFactor);
        }

        ds = isWaypoint ? baseSpeed : baseSpeed * cosValue;

        return new DHDS(dh, ds);
    }

    // ═════════════════════════════════════════════════════════════
    //  DONE / STOP
    // ═════════════════════════════════════════════════════════════

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