using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Move : Command
{
    public Vector3 movePosition;
    public bool maxSpeedMovement;
    public float range;
    public float timeOnTarget;
    
    public LineRenderer potentialLine;
    public Vector3 diffToMovePosition = Vector3.positiveInfinity;
    public float dhRadians;
    public float dhDegrees;
    
    // Potential field variables
    public Vector3 attractivePotential = Vector3.zero;
    public Vector3 potentialSum = Vector3.zero;
    public Vector3 repulsivePotential = Vector3.zero;
    public float dh;
    public float angleDiff;
    public float cosValue;
    public float ds;
     public float doneDistanceSq = 100000f;

    public Move(Entity ent, Vector3 pos, bool maxSpeedMovement = false , float doneDistanceSq = 100000f) : base(ent)
    {
        movePosition = pos;
        this.maxSpeedMovement = maxSpeedMovement;
        if (ent.GetComponentInChildren<WeaponsAspect>() != null)
        {
            this.doneDistanceSq = ent.GetComponentInChildren<WeaponsAspect>().weapon.range * ent.GetComponentInChildren<WeaponsAspect>().weapon.range; 
        }
        // Debug.Log("DoneDistanceSq: " + this.doneDistanceSq);
        
    }

    public override void Init() 
    {
        if(!FogWarMgr.inst.nonRevelers.Contains(entity) ) 
        {
            line = LineMgr.inst.CreateMoveLine(entity.position, movePosition);
            line.gameObject.SetActive(false);
            potentialLine = LineMgr.inst.CreatePotentialLine(entity.position);
            if (potentialLine != null)
                potentialLine.gameObject.SetActive(false);
        }
        
    }

    public override void Tick() 
    {
        DHDS dhds;
        if (AIMgr.inst.isPotentialFieldsMovement) 
        {
            dhds = ComputePotentialDHDS(movePosition);
        }
        else
        {
            dhds = ComputeDHDS();
        }

        entity.desiredHeading = dhds.dh;
        entity.desiredSpeed = dhds.ds;
        
        if(!FogWarMgr.inst.nonRevelers.Contains(entity))
            line.SetPosition(1, movePosition);

        range = diffToMovePosition.magnitude;
        timeOnTarget = range / entity.speed;
    }

    public virtual DHDS ComputeDHDS()
    {
        diffToMovePosition = movePosition - entity.position;
        dhRadians = Mathf.Atan2(diffToMovePosition.x, diffToMovePosition.z);
        dhDegrees = Utils.Degrees360(Mathf.Rad2Deg * dhRadians);
        return new DHDS(dhDegrees, entity.maxSpeed);
    }

    // public virtual DHDS ComputePotentialDHDS(Vector3 movePosition)
    // {
    //     diffToMovePosition = movePosition - entity.position;
    //     repulsivePotential = Vector3.zero;
    //     attractivePotential = Vector3.zero;
    //     potentialSum = Vector3.zero;

    //     // Entity repulsion
    //     foreach (Entity ent in EntityMgr.inst.entities)
    //     {
    //         if (ent.entityClass == EntityClass.Missile) continue;
    //         if (ent == entity) continue;

    //         Potential p = DistanceMgr.inst.GetPotential(entity, ent);
    //         if (p.distance < AIMgr.inst.potentialDistanceThreshold)
    //         {
    //             repulsivePotential += p.direction * ent.mass *
    //                 AIMgr.inst.repulsiveCoefficient * 
    //                 Mathf.Pow(p.diff.magnitude, AIMgr.inst.repulsiveExponent);
    //         }
    //     }

    //     // No-Go Zone repulsion
    //     foreach (NoGoZoneBounds zone in NoGoZoneManager.Zones)
    //     {
    //         repulsivePotential += ComputeNoGoZoneContribution(zone, entity.position);
    //     }

    //     // Attraction to target
    //     Vector3 rawAttraction = movePosition - entity.position;
    //     attractivePotential = rawAttraction.normalized *
    //         AIMgr.inst.attractionCoefficient *
    //         Mathf.Pow(rawAttraction.magnitude, AIMgr.inst.attractiveExponent);

    //     potentialSum = attractivePotential - repulsivePotential;
        
    //     // Calculate final DHDS
    //     dh = Utils.Degrees360(Mathf.Rad2Deg * Mathf.Atan2(potentialSum.x, potentialSum.z));
    //     angleDiff = Utils.Degrees360(Utils.AngleDiffPosNeg(dh, entity.heading));
    //     cosValue = (Mathf.Cos(angleDiff * Mathf.Deg2Rad) + 1) / 2.0f;
    //     ds = entity.maxSpeed * cosValue;

    //     if (potentialLine != null)
    //     {
    //         potentialLine.SetPosition(0, entity.position);
    //         potentialLine.SetPosition(1, entity.position + potentialSum);
    //     }
        
    //     return new DHDS(dh, ds);
    // }
public virtual DHDS ComputePotentialDHDS(Vector3 movePosition)
{
    // Calculate basic position difference
    diffToMovePosition = movePosition - entity.position;
    repulsivePotential = Vector3.zero;
    attractivePotential = Vector3.zero;
    potentialSum = Vector3.zero;

    // Calculate flocking forces
    Vector3 cohesionForce = CalculateCohesionForce();
    Vector3 alignmentForce = CalculateAlignmentForce();
    float speedMatch = CalculateSpeedMatching();

    // Entity repulsion calculation
    foreach (Entity ent in EntityMgr.inst.entities)
    {
        if (ent.entityClass == EntityClass.Missile) continue;
        if (ent == entity) continue;

        Potential p = DistanceMgr.inst.GetPotential(entity, ent);
        if (p.distance < AIMgr.inst.potentialDistanceThreshold)
        {
            Vector3 repulsion = p.direction * ent.mass *
                              AIMgr.inst.repulsiveCoefficient *
                              Mathf.Pow(p.diff.magnitude, AIMgr.inst.repulsiveExponent);
            
            // Add group-specific repulsion
            if (ent.groupId == entity.groupId)
            {
                repulsion *= AIMgr.inst.groupRepulsiveCoefficient;
            }
            
            repulsivePotential += repulsion;
        }
    }

    // Terrain/No-Go Zone avoidance
    foreach (NoGoZoneBounds zone in NoGoZoneManager.Zones)
    {
        repulsivePotential += ComputeNoGoZoneContribution(zone, entity.position);
    }

    // Calculate attraction to target
    Vector3 rawAttraction = movePosition - entity.position;
    attractivePotential = rawAttraction.normalized *
                        AIMgr.inst.attractionCoefficient *
                        Mathf.Pow(rawAttraction.magnitude, AIMgr.inst.attractiveExponent);

    // Combine all forces
    potentialSum = attractivePotential 
                 - repulsivePotential 
                 + (cohesionForce * AIMgr.inst.cohesionStrength)
                 + (alignmentForce * AIMgr.inst.alignmentStrength);

    // Calculate desired heading
    dh = Utils.Degrees360(Mathf.Rad2Deg * Mathf.Atan2(potentialSum.x, potentialSum.z));
    
    // Calculate base speed from heading alignment
    angleDiff = Utils.Degrees360(Utils.AngleDiffPosNeg(dh, entity.heading));
    cosValue = (Mathf.Cos(angleDiff * Mathf.Deg2Rad) + 1) / 2.0f;
    float baseDS = entity.maxSpeed * cosValue;

    // Apply speed matching with group
    ds = Mathf.Lerp(baseDS, speedMatch, AIMgr.inst.speedMatchStrength);
    
    // Apply final speed constraints
    ds = Mathf.Clamp(ds, entity.minSpeed, entity.maxSpeed);

    // Update debug visualization
    if (potentialLine != null)
    {
        potentialLine.SetPosition(0, entity.position);
        potentialLine.SetPosition(1, entity.position + potentialSum);
        potentialLine.startColor = Color.Lerp(Color.red, Color.green, ds/entity.maxSpeed);
        potentialLine.endColor = Color.blue;
    }

    return new DHDS(dh, ds);
}
private float CalculateSpeedMatching()
{
    float totalSpeed = 0f;
    int groupCount = 0;

    foreach (Entity ent in EntityMgr.inst.entities)
    {
        if (ent == entity || ent.groupId != entity.groupId) continue;
        
        float distance = Vector3.Distance(entity.position, ent.position);
        if (distance < AIMgr.inst.cohesionRadius)
        {
            totalSpeed += ent.speed;
            groupCount++;
        }
    }

    return groupCount > 0 
        ? Mathf.Clamp(totalSpeed/groupCount, entity.minSpeed, entity.maxSpeed)
        : entity.speed;
}
    private Vector3 CalculateCohesionForce()
    {
        Vector3 groupCenter = Vector3.zero;
        int groupCount = 0;

        foreach (Entity ent in EntityMgr.inst.entities)
        {
            if (ent == entity || ent.groupId != entity.groupId) continue;

            float distance = Vector3.Distance(entity.position, ent.position);
            if (distance < AIMgr.inst.cohesionRadius && distance > 0)
            {
                groupCenter += ent.position;
                groupCount++;
            }
        }

        if (groupCount > 0)
        {
            groupCenter /= groupCount;
            return (groupCenter - entity.position).normalized;
        }

        return Vector3.zero;
    }

    private Vector3 CalculateAlignmentForce()
    {
        Vector3 averageDirection = Vector3.zero;
        int groupCount = 0;

        foreach (Entity ent in EntityMgr.inst.entities)
        {
            if (ent == entity || ent.groupId != entity.groupId) continue;

            float distance = Vector3.Distance(entity.position, ent.position);
            if (distance < AIMgr.inst.alignmentRadius && distance > 0)
            {
                averageDirection += ent.velocity.normalized;
                groupCount++;
            }
        }

        if (groupCount > 0)
        {
            averageDirection /= groupCount;
            return averageDirection.normalized;
        }

        return Vector3.zero;
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

    private Vector3 ComputeNoGoZoneContribution(NoGoZoneBounds zone, Vector3 entityPosition)
{
    Vector3 closestPoint = zone.GetClosestPoint(entityPosition);
    Vector3 toEntity = entityPosition - closestPoint;
    float distance = toEntity.magnitude;

    if(distance < 0.01f) return Vector3.zero;

    Vector3 dir = toEntity.normalized;
    
    if(zone.Contains(entityPosition))
    {
        float penetration = zone.GetPenetrationDepth(entityPosition);
        float force = AIMgr.inst.terrainContainmentStrength * penetration;
        return dir * force;
    }
    else
    {
        float force = AIMgr.inst.terrainRepulsionStrength / 
                      Mathf.Pow(distance, AIMgr.inst.terrainRepulsionFalloff);
        return dir * force;
    }
}

    public override bool IsDone()
    {
        if (entity == null ) return true;
        return (entity.position - movePosition).sqrMagnitude < doneDistanceSq;
        
    }

    public override void Stop()
    {
        entity.desiredSpeed = 0;
        entity.desiredHeading = entity.heading;
        LineMgr.inst.DestroyLR(line);
        LineMgr.inst.DestroyLR(potentialLine);
        line = null;
        potentialLine = null;
    }
}