using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Follow : Move
{
    public Entity targetEntity;
    public Vector3 relativeOffset;
    public float followThresholdSq = 200f;
    public Vector3 offset;
    private Vector3 lastValidTargetPosition;

    public Follow(Entity ent, Entity target, Vector3 delta) : base(ent, target.transform.position)
    {
        targetEntity = target;
        relativeOffset = delta;
        lastValidTargetPosition = target.transform.position;
    }

    public override void Init()
    {
        base.Init();
        offset = targetEntity.transform.TransformVector(relativeOffset);
        line = LineMgr.inst.CreateFollowLine(entity.position, targetEntity.position + offset, targetEntity.position);
        line.gameObject.SetActive(false);
    }

    public override void Tick()
    {
        if (targetEntity == null || !targetEntity.gameObject.activeSelf)
        {
            // Target is invalid; stop following
            movePosition = lastValidTargetPosition;
            base.Tick();
            return;
        }

        // Update position and offset
        offset = targetEntity.transform.TransformVector(relativeOffset);
        movePosition = targetEntity.position + offset;
        // Debug.Log(Mathf.Sqrt((movePosition-entity.position).sqrMagnitude));
        lastValidTargetPosition = movePosition;

        // Calculate movement
        DHDS dhds = ComputePotentialDHDS(movePosition);
        entity.desiredHeading = dhds.dh;

        // Adjust speed based on proximity
        float distanceSq = (movePosition - entity.position).sqrMagnitude;
        entity.desiredSpeed = (Mathf.Sqrt(distanceSq) < followThresholdSq) 
            ? targetEntity.desiredSpeed
            : entity.maxSpeed;
        Debug.Log(followThresholdSq);
        Debug.Log((Mathf.Sqrt(distanceSq) < followThresholdSq) );
        // base.Tick(); // Allow base class to handle pathfinding if needed
    }

    public override bool IsDone()
    {
        // Terminate if target is invalid or base condition met
        return targetEntity == null || !targetEntity.gameObject.activeSelf ;
    }

    public override void Stop()
    {
        base.Stop();
        entity.desiredSpeed = 0;
    }


    public Vector3 relativeVelocity;
    public float predictedInterceptTime;
    public Vector3 predictedMovePosition;
    Vector3 predictedDiff;
    public Vector3 diff;
    // this is also used by the Intercept class
   public float ComputePredictiveDH(Vector3 movePosition)
    {
        float dh;
        movePosition = targetEntity.position + targetEntity.transform.TransformVector(relativeOffset);
        diff = movePosition - entity.position;
        relativeVelocity = entity.velocity - targetEntity.velocity;
        predictedInterceptTime = diff.magnitude / relativeVelocity.magnitude;
        if (predictedInterceptTime >= 0)
        {
            predictedMovePosition = movePosition + (targetEntity.velocity * predictedInterceptTime);

            predictedDiff = predictedMovePosition - entity.position;
            dh = Utils.Degrees360(Mathf.Atan2(predictedDiff.x, predictedDiff.z) * Mathf.Rad2Deg);
        }
        else
        {
            dh = ComputeDHDS().dh;
        }
        return dh;
    }

}
