using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Move : Command
{
    public Vector3 movePosition;
    public float range;
    public float timeOnTarget;
    private readonly bool useMaxSpeedMovement;
    protected float pathUpdateCooldown = 0.25f; // baseline so derived commands have a default
    public Move(Entity ent, Vector3 pos, bool maxSpeedMovement = false, float doneDistanceSq = 1000f) : base(ent) {
        movePosition = pos;
        useMaxSpeedMovement = maxSpeedMovement;
        if (doneDistanceSq > 0f) {
            this.doneDistanceSq = doneDistanceSq;
        }
    }

    public LineRenderer potentialLine;
    public override void Init() {
        //Debug.Log("MoveInit:\tMoving to: " + movePosition);
        line = LineMgr.inst.CreateMoveLine(entity.position, movePosition);
        line.gameObject.SetActive(false);
        potentialLine = LineMgr.inst.CreatePotentialLine(entity.position);
        if (potentialLine != null) {
            potentialLine.gameObject.SetActive(false);
        }
    }

    public override void Tick() {
        DHDS dhds;
        if(AIMgr.inst.isPotentialFieldsMovement)
            dhds = ComputePF2(movePosition);// ComputePotentialDHDS(movePosition);
        else
            dhds = ComputeDHDS();

        entity.desiredHeading = dhds.dh;
        entity.desiredSpeed = dhds.ds;
        if (line != null) {
            line.SetPosition(1, movePosition);
        }

        if (potentialLine != null && AIMgr.inst.isPotentialFieldsMovement) {
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
        foreach (Entity ent in EntityMgr.inst.entities) {
            if (ent == entity) continue;


            p = DistanceMgr.inst.GetPotential(entity, ent);
            if (p.distance < AIMgr.inst.potentialDistanceThreshold) {
                //repulsivePotential += p.direction * entity.mass *
                repulsivePotential += p.direction * ent.mass *
                    AIMgr.inst.repulsiveCoefficient * Mathf.Pow(p.diff.magnitude, AIMgr.inst.repulsiveExponent);

            }
        }
        //repulsivePotential *= repulsiveCoefficient * Mathf.Pow(repulsivePotential.magnitude, repulsiveExponent);
        attractivePotential = movePosition - entity.position;
        Vector3 tmp = attractivePotential.normalized;
        attractivePotential = tmp * 
            AIMgr.inst.attractionCoefficient * Mathf.Pow(attractivePotential.magnitude, AIMgr.inst.attractiveExponent);
        potentialSum = attractivePotential - repulsivePotential;

        dh = Utils.Degrees360(Mathf.Rad2Deg * Mathf.Atan2(potentialSum.x, potentialSum.z));

        angleDiff = Utils.Degrees360(Utils.AngleDiffPosNeg(dh, entity.heading));
        cosValue = (Mathf.Cos(angleDiff * Mathf.Deg2Rad) + 1) / 2.0f; // makes it between 0 and 1
        ds = entity.maxSpeed * cosValue;

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
        diffToMovePosition = pos - entity.position;
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
                    repulsivePotential += subPot.direction * otherEnt.mass
                        * AIMgr.inst.repulsive2Coefficient * Mathf.Pow(subPot.distance, AIMgr.inst.repulsiveExponent);
                }
            }
        }
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
        ds = baseSpeed * cosValue;

        return new DHDS(dh, ds);
    }




    public float doneDistanceSq = 100f; // 10 units squared
    public override bool IsDone()
    {
        float thresholdSq = doneDistanceSq;
        var weaponsAspect = entity.GetComponentInChildren<WeaponsAspect>();
        if (weaponsAspect != null && weaponsAspect.weapon != null && weaponsAspect.weapon.range > 0f) {
            float weaponRange = weaponsAspect.weapon.range;
            thresholdSq = weaponRange * weaponRange;
        }
        return (entity.position - movePosition).sqrMagnitude < thresholdSq;
    }

    public override void Stop()
    {
        entity.desiredSpeed = 0;
        if (line != null) {
            LineMgr.inst.DestroyLR(line);
        }
        if (potentialLine != null) {
            LineMgr.inst.DestroyLR(potentialLine);
        }
        line = null;
        potentialLine = null;
    }
}
