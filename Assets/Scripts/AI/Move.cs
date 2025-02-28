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
    public Move(Entity ent, Vector3 pos, bool maxSpeedMovement = false) : base(ent)
    {
        movePosition = pos;
        this.maxSpeedMovement = maxSpeedMovement;
    }

    public LineRenderer potentialLine;
    public override void Init() {
        //Debug.Log("MoveInit:\tMoving to: " + movePosition);
        if(!FogWarMgr.inst.nonRevelers.Contains(entity)) {
            line = LineMgr.inst.CreateMoveLine(entity.position, movePosition);
            line.gameObject.SetActive(false);
            potentialLine = LineMgr.inst.CreatePotentialLine(entity.position);
            potentialLine.gameObject.SetActive(false);
        }
        
    }

    public override void Tick() {
        DHDS dhds;
        if (AIMgr.inst.isPotentialFieldsMovement) {
            if (maxSpeedMovement) {
                dhds = ComputePotentialDHDS(movePosition);
            }
            else {
                dhds = ComputePF2(movePosition);    
            }
        }
            
            
    // ComputePotentialDHDS(movePosition);
        else
            dhds = ComputeDHDS();

        entity.desiredHeading = dhds.dh;
        entity.desiredSpeed = dhds.ds;
        if(!FogWarMgr.inst.nonRevelers.Contains(entity))  line.SetPosition(1, movePosition);

        range = diffToMovePosition.magnitude;
        timeOnTarget = range / entity.speed;

    }

    public Vector3 diffToMovePosition = Vector3.positiveInfinity;
    public float dhRadians;
    public float dhDegrees;
    public virtual DHDS ComputeDHDS()
    {
        diffToMovePosition = movePosition - entity.position;
        dhRadians = Mathf.Atan2(diffToMovePosition.x, diffToMovePosition.z);
        dhDegrees = Utils.Degrees360(Mathf.Rad2Deg * dhRadians);
        return new DHDS(dhDegrees, entity.maxSpeed);

    }

   public virtual DHDS ComputePotentialDHDS(Vector3 movePosition)
{
    Debug.Log("ComputePotentialDHDS:\tMoving to: " + movePosition);
    diffToMovePosition = movePosition - entity.position;
    
    // Initialize potentials
    repulsivePotential = Vector3.zero;
    attractivePotential = Vector3.zero;
    potentialSum = Vector3.zero;

    // 1. Calculate repulsion from other entities
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

    // 2. Calculate repulsion from bounding boxes
    foreach (NoGoZoneBounds zone in NoGoZoneManager.Zones)
    {
        Vector3 closestPoint = zone.GetClosestPoint(entity.position);
        Vector3 diff = closestPoint - entity.position;
        float distance = diff.magnitude;

        if (zone.Contains(entity.position))
        {
            // Push outward from box center when inside
            Vector3 dirFromCenter = (entity.position - zone.transform.position).normalized;
            repulsivePotential += dirFromCenter * zone.repulsionStrength;
        }
        else if (distance <= zone.repulsionRadius)
        {
            // Apply distance-based repulsion when near
            Vector3 dir = diff.normalized;
            float force = zone.repulsionStrength * 
                         (1 - (distance / zone.repulsionRadius));
            repulsivePotential += dir * force;
        }
    }

    // 3. Calculate attraction to target
    Vector3 rawAttraction = movePosition - entity.position;
    attractivePotential = rawAttraction.normalized *
        AIMgr.inst.attractionCoefficient *
        Mathf.Pow(rawAttraction.magnitude, AIMgr.inst.attractiveExponent);

    // 4. Combine potentials
    potentialSum = attractivePotential - repulsivePotential;

    // 5. Calculate desired heading
    dh = Utils.Degrees360(
        Mathf.Rad2Deg * Mathf.Atan2(potentialSum.x, potentialSum.z)
    );

    // 6. Calculate speed based on heading alignment
    angleDiff = Utils.Degrees360(Utils.AngleDiffPosNeg(dh, entity.heading));
    cosValue = (Mathf.Cos(angleDiff * Mathf.Deg2Rad) + 1) / 2.0f;
    ds = entity.maxSpeed * cosValue;

    // 7. Update debug visualization
    if (potentialLine != null)
    {
        potentialLine.SetPosition(0, entity.position);
        potentialLine.SetPosition(1, entity.position + potentialSum);
    }

    return new DHDS(dh, ds);
}
    public Vector3 attractivePotential = Vector3.zero;
    public Vector3 potentialSum = Vector3.zero;
    public Vector3 repulsivePotential = Vector3.zero;
    public float dh;
    public float angleDiff;
    public float cosValue;
    public float ds;

    public DHDS ComputePF2(Vector3 pos) {
        diffToMovePosition = movePosition - entity.position;
        repulsivePotential = Vector3.one;
        repulsivePotential.y = 0;
        Potential pot;
        foreach(Entity otherEnt in EntityMgr.inst.entities) {
            if(otherEnt != entity && (entity.position - otherEnt.position).sqrMagnitude < AIMgr.inst.potentialDistanceThresholdSq) {
                if(entity.ai.potentialsD.ContainsKey(otherEnt)) {
                    pot = entity.ai.potentialsD[otherEnt];
                } else {
                    pot = new Potential(entity, otherEnt);
                    entity.ai.potentialsD.Add(otherEnt, pot);
                    entity.ai.potentialsL.Add(new EntityPotential { entity = otherEnt, potential = pot });
                }
                pot.ReCompute();
                foreach(SubPotential subPot in pot.subPotentials) {
                    repulsivePotential += subPot.direction * otherEnt.mass*.05f
                        * AIMgr.inst.repulsive2Coefficient * Mathf.Pow(subPot.distance, AIMgr.inst.repulsiveExponent);
                }
            }
        }
        foreach (NoGoZoneBounds zone in NoGoZoneManager.Zones)
    {
        Vector3 closestPoint = zone.GetClosestPoint(entity.position);
        Vector3 diff = closestPoint - entity.position;
        float distance = diff.magnitude;

        if (zone.Contains(entity.position))
        {
            // Push outward from box center when inside
            Vector3 dirFromCenter = (entity.position - zone.transform.position).normalized;
            repulsivePotential += dirFromCenter * zone.repulsionStrength;
        }
        else if (distance <= zone.repulsionRadius)
        {
            // Apply distance-based repulsion when near
            Vector3 dir = diff.normalized;
            float force = zone.repulsionStrength * 
                         (1 - (distance / zone.repulsionRadius));
            repulsivePotential += dir * force;
        }
    }
        Vector3 tmp = (movePosition - entity.position).normalized;
        attractivePotential = tmp 
            * AIMgr.inst.attraction2Coefficient * entity.mass * Mathf.Pow(diffToMovePosition.magnitude, AIMgr.inst.attractiveExponent);

        potentialSum = attractivePotential - repulsivePotential;

        dh = Utils.Degrees360(Mathf.Rad2Deg * Mathf.Atan2(potentialSum.x, potentialSum.z)); 

        angleDiff = Utils.Degrees360(Utils.AngleDiffPosNeg(dh, entity.heading));
        cosValue = (Mathf.Cos(angleDiff * Mathf.Deg2Rad) + 1) / 2.0f;
        ds = entity.cruiseSpeed * cosValue;
        return new DHDS(dh, ds);
    }




    public float doneDistanceSq = 1000;
    public override bool IsDone()
    {
        entity.desiredSpeed = 0;
        return (entity.position - movePosition).sqrMagnitude < doneDistanceSq;
    }

    public override void Stop()
    {
        
        LineMgr.inst.DestroyLR(line);
        LineMgr.inst.DestroyLR(potentialLine);
        line = null;
        potentialLine = null;
    }
}
