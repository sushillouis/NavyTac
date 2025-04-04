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
        Debug.Log("DoneDistanceSq: " + this.doneDistanceSq);
        
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
        entity.desiredHeading = entity.heading;
        LineMgr.inst.DestroyLR(line);
        LineMgr.inst.DestroyLR(potentialLine);
        line = null;
        potentialLine = null;
    }
}