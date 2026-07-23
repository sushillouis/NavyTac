using UnityEngine;

[System.Serializable]
public class Follow : Move
{
    public Entity targetEntity;
    public Vector3 relativeOffset;
    public Vector3 offset;
    protected Vector3 lastValidTargetPosition;
    private WeaponsAspect _weaponsAspect;
    private float weaponRangeSq;

    public Follow(Entity ent, Entity target, Vector3 delta, float doneDistanceSq = 1000f) : 
        base(ent, (target != null && target.gameObject.activeSelf ? target.transform.position : (ent != null && ent.gameObject.activeSelf ? ent.transform.position : Vector3.zero)), doneDistanceSq: doneDistanceSq)
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

        // Cache weapon range for maintain-distance logic
        if (ent != null)
        {
            _weaponsAspect = ent.GetComponentInChildren<WeaponsAspect>();
            if (_weaponsAspect != null && _weaponsAspect.weapon != null)
            {
                float range = _weaponsAspect.weapon.range;
                weaponRangeSq = (range * range) - 100f * 100f; // Same margin as AttackMove
            }
            else
            {
                weaponRangeSq = 0f;
            }
        }
    }

    public override void Init()
    {
        if (entity == null || !entity.gameObject.activeSelf)
        {
            return;
        }
        if (targetEntity == null || !targetEntity.gameObject.activeSelf)
        {
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
            return;
        }

        if (targetEntity == null || !targetEntity.gameObject.activeSelf)
        {
            // Target is invalid; move to last known valid position
            movePosition = lastValidTargetPosition;
            base.Tick();
            return;
        }

        // Update position and offset
        offset = targetEntity.transform.TransformVector(relativeOffset);
        movePosition = targetEntity.position + offset;
        lastValidTargetPosition = movePosition;

        float distanceSq = (movePosition - entity.position).sqrMagnitude;

        // Use weapon range as maintain distance (like AttackMove), fall back to doneDistanceSq
        float maintainDistanceSq = weaponRangeSq > 0f ? weaponRangeSq : doneDistanceSq;

        if (distanceSq <= maintainDistanceSq)
        {
            // Within maintain distance — match target's speed and heading
            entity.desiredSpeed = targetEntity.desiredSpeed;
            entity.desiredHeading = targetEntity.desiredHeading;

            // Update lines
            if (line != null)
            {
                line.SetPosition(0, entity.position);
                line.SetPosition(1, movePosition);
            }
            if (potentialLine != null && AIMgr.inst.isPotentialFieldsMovement)
            {
                potentialLine.SetPosition(0, entity.position);
                potentialLine.SetPosition(1, entity.position + potentialSum);
            }

            range = (movePosition - entity.position).magnitude;
            timeOnTarget = entity.speed > 0.001f ? range / entity.speed : float.PositiveInfinity;
        }
        else
        {
            // Outside maintain distance — close in using full Move.Tick() steering
            base.Tick();
        }
    }

    public override bool IsDone()
    {
        if (entity == null || !entity.gameObject.activeSelf) return true;
        return targetEntity == null || !targetEntity.gameObject.activeSelf;
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
        if (entity == null || !entity.gameObject.activeSelf) return 0f;
        if (targetEntity == null || !targetEntity.gameObject.activeSelf) return entity.heading;

        float dh;
        Vector3 calculatedMovePosition = targetEntity.position + targetEntity.transform.TransformVector(relativeOffset);
        diff = calculatedMovePosition - entity.position;
        relativeVelocity = entity.velocity - targetEntity.velocity;

        if (relativeVelocity.sqrMagnitude < Mathf.Epsilon * Mathf.Epsilon)
        {
            DHDS fallbackDhds = ComputeDHDS();
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