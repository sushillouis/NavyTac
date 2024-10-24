using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Pincer : Move
{
    public Entity targetEntity;
    public Vector3 relativeOffset;
    public float attackAngle = 0;
    public Pincer(Entity ent, Entity target, float dist, float angel): base(ent, target.transform.position)
    {
        targetEntity = target;
        collapsePincerDistance = dist;
        attackAngle = angel;
    }

    // Start is called before the first frame update
    public override void Init()
    {
        //Debug.Log("Follow:\t Following: " + targetEntity.gameObject.name);
        line = LineMgr.inst.CreateGeneric3PointLine(entity.position, ComputePincerPoint(), targetEntity.position, Color.magenta);
        line.gameObject.SetActive(false);
    }

    public float collapsePincerDistance = 2000;
    public Vector3 offset;
    // Update is called once per frame
    public override void Tick()
    {
        offset = targetEntity.transform.TransformVector(relativeOffset);
        //entity.desiredHeading = ComputePredictiveDH(relativeOffset);
        // entity.desiredHeading = ComputeDHDS().dh;
        if (diff.sqrMagnitude < collapsePincerDistance) {
            entity.desiredSpeed = entity.maxSpeed;
            entity.desiredHeading = attackAngle;
        } else {
            entity.desiredSpeed = entity.maxSpeed;
            entity.desiredHeading = ComputePincerDH();
        }
    }

    public bool done = false;//user can set it to done

    public override bool IsDone()
    {
        return Vector3.Distance(entity.position,targetEntity.position) < collapsePincerDistance;
    }

    public override void Stop()
    {
        base.Stop();
        entity.desiredSpeed = 0;
        targetEntity.desiredSpeed = 0;
        targetEntity.GetComponentInChildren<UnitAI>().StopAndRemoveAllCommands();
        Vector3 deadRot = targetEntity.transform.localEulerAngles;
        deadRot.z = 90;
        targetEntity.transform.localEulerAngles = deadRot;
    }
    public Vector3 ComputePincerPoint()
    {
        return targetEntity.position + Quaternion.Euler(0,attackAngle+targetEntity.heading,0) * Vector3.forward * collapsePincerDistance;
    }
    //------------------------------------------------------
    public float ComputePincerDH()
    {
        float dh = 0;
        movePosition = ComputePincerPoint();
        Vector3 predictedDiff = movePosition - entity.position;
        dh = Utils.Degrees360(Mathf.Atan2(predictedDiff.x, predictedDiff.z) * Mathf.Rad2Deg);
        return dh;
    }
}

public struct Approach
{
    public float angle;
    public float mag;

    public Approach(float angle, float mag) {
        this.angle = angle;
        this.mag = mag;
    }
}