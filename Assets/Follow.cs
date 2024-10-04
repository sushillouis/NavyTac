using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Follow : Move
{
    public Entity targetEntity;
    public Vector3 relativeOffset;
    public Follow(Entity ent, Entity target, Vector3 delta): base(ent, target.transform.position)
    {
        targetEntity = target;
        relativeOffset = delta;
    }

    // Start is called before the first frame update
    public override void Init()
    {
        //Debug.Log("Follow:\t Following: " + targetEntity.gameObject.name);
        offset = targetEntity.transform.TransformVector(relativeOffset);
        line = LineMgr.inst.CreateFollowLine(entity.position, targetEntity.position + offset, targetEntity.position);
        line.gameObject.SetActive(false);
    }

    public float followThreshold = 2000;
    public Vector3 offset;
    // Update is called once per frame
    public override void Tick()
    {
        offset = targetEntity.transform.TransformVector(relativeOffset);
        movePosition = targetEntity.transform.position + offset;
        //entity.desiredHeading = ComputePredictiveDH(relativeOffset);
        entity.desiredHeading = ComputeDHDS().dh;
        if (diff.sqrMagnitude < followThreshold) {
            entity.desiredSpeed = targetEntity.speed;
            entity.desiredHeading = targetEntity.heading;
        } else {
            entity.desiredSpeed = entity.maxSpeed;
        }

    }

    public bool done = false;//user can set it to done

    public override bool IsDone()
    {
        return done;
    }

    public override void Stop()
    {
        base.Stop();
        entity.desiredSpeed = 0;
        isRunning = false;

    }

    Vector3 relativeVelocity;
    public float predictedInterceptTime;
    public Vector3 predictedMovePosition;
    Vector3 predictedDiff;
    private Vector3 lastKnownPosition = Vector3.zero;
    //------------------------------------------------------
    public float ComputePredictiveDH(Vector3 relativeOffset)
    {

        if (targetEntity != null)
        {
            lastKnownPosition = targetEntity.position;
        }
        Vector3 currentTargetPosition = targetEntity != null ? targetEntity.position : lastKnownPosition;
        Vector3 currentTargetVelocity = targetEntity != null ? targetEntity.velocity : Vector3.zero;

        float dh;
        movePosition = currentTargetPosition + (targetEntity != null ? targetEntity.transform.TransformVector(relativeOffset) : relativeOffset);
        diff = movePosition - entity.position;
        relativeVelocity = entity.velocity - currentTargetVelocity;
        predictedInterceptTime = diff.magnitude / relativeVelocity.magnitude;
        if (predictedInterceptTime >= 0) {
            predictedMovePosition = movePosition + (currentTargetVelocity * predictedInterceptTime);
            predictedDiff = predictedMovePosition - entity.position;
            dh = Utils.Degrees360(Mathf.Atan2(predictedDiff.x, predictedDiff.z) * Mathf.Rad2Deg);
        } else {
            dh = ComputeDHDS().dh;
        }
        return dh;
    }

}
