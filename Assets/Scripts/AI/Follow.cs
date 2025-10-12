using UnityEngine;

[System.Serializable]
public class Follow : Move
{
    public Entity targetEntity;
    public Vector3 relativeOffset;
    public float followThresholdSq = 200f;
    public Vector3 offset;
    private Vector3 lastValidTargetPosition;

    public Follow(Entity ent, Entity target, Vector3 delta) : 
        base(ent, (target != null && target.gameObject.activeSelf ? target.transform.position : (ent != null && ent.gameObject.activeSelf ? ent.transform.position : Vector3.zero)))
    {
        targetEntity = target;
        relativeOffset = delta;

        if (targetEntity != null && targetEntity.gameObject.activeSelf)
        {
            lastValidTargetPosition = targetEntity.transform.position;
        }
        else if (ent != null && ent.gameObject.activeSelf)
        {
            lastValidTargetPosition = ent.transform.position;
        }
        else
        {
            lastValidTargetPosition = Vector3.zero;
        }
    }

    public override void Init()
    {
        if (entity == null || !entity.gameObject.activeSelf)
        {
            // //Debug.LogError("Follow.Init: Entity is null or inactive.");
            return;
        }
        if (targetEntity == null || !targetEntity.gameObject.activeSelf)
        {
            // //Debug.LogError("Follow.Init: TargetEntity is null or inactive.");
            return;
        }

        base.Init();
        offset = targetEntity.transform.TransformVector(relativeOffset);
        line = LineMgr.inst.CreateFollowLine(entity.position, targetEntity.position + offset, targetEntity.position, entity.isAI);
        if (line != null)
        {
            line.gameObject.SetActive(false);
        }
    }

    public override void Tick()
    {
        if (entity == null || !entity.gameObject.activeSelf)
        {
            return; // Entity performing the action is invalid
        }

        if (targetEntity == null || !targetEntity.gameObject.activeSelf)
        {
            // Target is invalid; stop following and move to last known valid position
            movePosition = lastValidTargetPosition;
            base.Tick(); // This will attempt to move to lastValidTargetPosition
            return;
        }

        // Update position and offset
        offset = targetEntity.transform.TransformVector(relativeOffset);
        movePosition = targetEntity.position + offset;
        // //Debug.Log(Mathf.Sqrt((movePosition-entity.position).sqrMagnitude));
        lastValidTargetPosition = movePosition; // Update last valid target position

        // Calculate movement
        DHDS dhds = ComputePotentialDHDS(movePosition);
        entity.desiredHeading = dhds.dh;

        // Adjust speed based on proximity
        float distanceSq = (movePosition - entity.position).sqrMagnitude;
        entity.desiredSpeed = (Mathf.Sqrt(distanceSq) < followThresholdSq) 
            ? targetEntity.desiredSpeed
            : entity.maxSpeed;
        //Debug.Log(followThresholdSq);
        //Debug.Log(Mathf.Sqrt(distanceSq) < followThresholdSq);
        // base.Tick(); // Allow base class to handle pathfinding if needed
    }

    public override bool IsDone()
    {
        // Terminate if target is invalid or base condition met
        // Also check if the entity itself is gone
        if (entity == null || !entity.gameObject.activeSelf) return true;
        return targetEntity == null || !targetEntity.gameObject.activeSelf ;
    }

    public override void Stop()
    {
        base.Stop();
        if (entity != null && entity.gameObject.activeSelf)
        {
            entity.desiredSpeed = 0;
        }
    }


    public Vector3 relativeVelocity;
    public float predictedInterceptTime;
    public Vector3 predictedMovePosition;
    Vector3 predictedDiff;
    public Vector3 diff;
    // this is also used by the Intercept class
   public float ComputePredictiveDH()
    {
        if (entity == null || !entity.gameObject.activeSelf) return 0f; // Or current heading: entity.heading
        if (targetEntity == null || !targetEntity.gameObject.activeSelf) return entity.heading;


        float dh;
        Vector3 calculatedMovePosition = targetEntity.position + targetEntity.transform.TransformVector(relativeOffset);
        diff = calculatedMovePosition - entity.position;
        relativeVelocity = entity.velocity - targetEntity.velocity;

        if (relativeVelocity.sqrMagnitude < Mathf.Epsilon * Mathf.Epsilon) // Avoid division by zero or near-zero
        {
            // Relative speed is zero, predictive intercept is not meaningful or will cause issues.
            // Fallback to direct heading towards the calculatedMovePosition.
            // This part might need to use ComputeDHDS(calculatedMovePosition) if available,
            // or set this.movePosition and call parameterless ComputeDHDS().
            // For now, using the existing else branch logic:
            DHDS fallbackDhds = ComputeDHDS(); // Assumes ComputeDHDS() uses current state or a default.
            dh = fallbackDhds.dh;
        }
        else
        {
            predictedInterceptTime = diff.magnitude / relativeVelocity.magnitude;
            if (predictedInterceptTime >= 0 && !float.IsNaN(predictedInterceptTime) && !float.IsInfinity(predictedInterceptTime))
            {
                predictedMovePosition = calculatedMovePosition + (targetEntity.velocity * predictedInterceptTime);

                predictedDiff = predictedMovePosition - entity.position;
                dh = Utils.Degrees360(Mathf.Atan2(predictedDiff.x, predictedDiff.z) * Mathf.Rad2Deg);
            }
            else
            {
                DHDS fallbackDhds = ComputeDHDS();
                dh = fallbackDhds.dh;
            }
        }
        return dh;
    }

}
