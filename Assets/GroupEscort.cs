using UnityEngine;


[System.Serializable]
public class GroupEscort : Follow
{
    public GroupEscort(Entity ent, Entity target, Vector3 newOffset): base(ent, target, Vector3.left)
    {
        targetEntity = target;
        relativeOffset = newOffset;
    }

    public override void Init()
    {
        offset = targetEntity.transform.TransformVector(relativeOffset);
        potentialLine = LineMgr.inst.CreatePotentialLine(entity.position);
        potentialLine.gameObject.SetActive(false);
    }

    public void Update(Vector3 newOffset) {
        relativeOffset = newOffset;
    }

    public override bool IsDone()
    {
        return false;
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
    }


    public new DHDS ComputePotentialDHDS()
    {
        UnitAI thisAI = entity.GetComponentInChildren<UnitAI>();
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
            UnitAI ai = ent.GetComponentInChildren<UnitAI>();
            if (ent == entity) {
                continue;
            }
            if (ent == entity || (ai.group == thisAI.group && thisAI.group.target != ent)) {
                potentialScalar=0.25f;
            }
            p = DistanceMgr.inst.GetPotential(entity, ent);
            if (p.distance < AIMgr.inst.potentialDistanceThreshold) {
                repulsivePotential += p.direction * entity.mass * potentialScalar *
                    AIMgr.inst.repulsiveCoefficient * Mathf.Pow(p.diff.magnitude, AIMgr.inst.repulsiveExponent);
            }
        }
        //repulsivePotential *= repulsiveCoefficient * Mathf.Pow(repulsivePotential.magnitude, repulsiveExponent);
        attractivePotential = movePosition - (entity.position+relativeOffset);
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
        // myGroup.RemoveMember(entity.GetComponentInChildren<UnitAI>());
        base.Stop();
        entity.desiredSpeed = 0;
        isRunning = false;
    }
}
