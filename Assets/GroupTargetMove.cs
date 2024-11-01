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
        line.gameObject.SetActive(false);
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
        Debug.Log(entity.name);
        if(line)
            line.SetPosition(1, movePosition);
    }


    public new DHDS ComputePotentialDHDS()
    {
        UnitAI thisAI = entity.GetComponentInChildren<UnitAI>();
        if(thisAI.group == null) {
            Stop();
            return new DHDS(0,0);
        }
        Potential p;
        repulsivePotential = Vector3.one; repulsivePotential.y = 0;
        foreach (Entity ent in EntityMgr.inst.entities) {
            //THIS SHOULD BE FIXED
            //THIS SHOULD BE FIXED
            //THIS SHOULD BE FIXED
            //THIS SHOULD BE FIXED
            //This many get calls is B A D
            UnitAI ai = ent.GetComponentInChildren<UnitAI>();
            if (ent == entity || (ai.group == thisAI.group && thisAI.group.target != ent))
                continue;
            p = DistanceMgr.inst.GetPotential(entity, ent);
            if (p.distance < AIMgr.inst.potentialDistanceThreshold) {
                repulsivePotential += p.direction * entity.mass *
                    AIMgr.inst.repulsiveCoefficient * Mathf.Pow(p.diff.magnitude, AIMgr.inst.repulsiveExponent);
                //repulsivePotential += p.diff;
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

    public override void Stop()
    {
        entity.desiredSpeed = 0;
        LineMgr.inst.DestroyLR(line);
        LineMgr.inst.DestroyLR(potentialLine);
        
        line = null;
        potentialLine = null;
    }
}
