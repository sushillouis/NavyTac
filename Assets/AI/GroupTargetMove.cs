using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GroupTargetMove : Move
{
    public GroupTargetMove(Entity ent, Vector3 pos) : base(ent, pos)
    {

    }

    public override void Init()
    {
        //Debug.Log("MoveInit:\tMoving to: " + movePosition);
        line = LineMgr.inst.CreateMoveLine(entity.position, movePosition);
        line.gameObject.SetActive(false);
        potentialLine = LineMgr.inst.CreatePotentialLine(entity.position);
        potentialLine.gameObject.SetActive(false);
    }

    public override void Tick()
    {
        DHDS dhds;
        if (AIMgr.inst.isPotentialFieldsMovement)
            dhds = ComputePotentialDHDS();
        else
            dhds = ComputeDHDS();

        entity.desiredHeading = dhds.dh;
        entity.desiredSpeed = dhds.ds;

        if(line)
            line.SetPosition(1, movePosition);
    }


    public new DHDS ComputePotentialDHDS()
    {
        UnitAI thisAI = entity.ai;
        if(thisAI.group == null) {
            Stop();
            return new DHDS(0,0);
        }
        Potential p;
        repulsivePotential = Vector3.zero; repulsivePotential.y = 0;
        foreach (Entity ent in EntityMgr.inst.entities) {
            //THIS SHOULD BE FIXED
            //THIS SHOULD BE FIXED
            //THIS SHOULD BE FIXED
            //THIS SHOULD BE FIXED
            //This many get calls is B A D
            float potentialScalar = 1.0f;
            UnitAI ai = ent.ai;
            if (ent == entity) {
                continue;
            }
            if (ai.group == thisAI.group) {
                potentialScalar=0.25f;
                if(ai.group.target == entity) {
                    potentialScalar=0.01f;
                }
            }
            p = DistanceMgr.inst.GetPotential(entity, ent);
            if (p.distance < AIMgr.inst.potentialDistanceThreshold) {
                repulsivePotential += p.direction * entity.mass * potentialScalar *
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

    public override bool IsDone()
    {
        return false;
    }

    public bool IsDoneGroup() {
        return (entity.position - movePosition).sqrMagnitude < doneDistanceSq;
    }

    public override void Stop()
    {
        entity.desiredSpeed = 0;
        LineMgr.inst.DestroyLR(line);
        line = null;
        LineMgr.inst.DestroyLR(potentialLine);
        potentialLine = null;
    }
}
