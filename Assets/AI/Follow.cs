using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Follow : Move
{
    public Entity targetEntity;
    public Vector3 relativeOffset;
    public Follow(Entity ent, Entity target, Vector3 delta) : base(ent, target.transform.position)
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
        if (targetEntity != null)
        {
            offset = targetEntity.transform.TransformVector(relativeOffset);
            movePosition = targetEntity.transform.position + offset;
            //entity.desiredHeading = ComputePredictiveDH(relativeOffset);
            entity.desiredHeading = ComputeDHDS().dh;
            if (diff.sqrMagnitude < followThreshold)
            {
                entity.desiredSpeed = targetEntity.speed;
                entity.desiredHeading = targetEntity.heading;
            }
            else
            {
                entity.desiredSpeed = entity.maxSpeed;
            }
        }
        else
        {
            Stop();
        }
        range = diff.magnitude;
        timeOnTarget = range / entity.speed;
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
    //------------------------------------------------------
    public float ComputePredictiveDH(Vector3 relativeOffset)
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

    public DHDS ComputePotentialPredictiveDHDS(Vector3 relativeOffset)
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
            Potential p;
            repulsivePotential = Vector3.one; repulsivePotential.y = 0;
            foreach (Entity ent in EntityMgr.inst.entities) {
                if (ent == entity && ent!= targetEntity) continue;
                p = DistanceMgr.inst.GetPotential(entity, ent);
                if (p.distance < AIMgr.inst.potentialDistanceThreshold) {
                    repulsivePotential += p.direction * entity.mass *
                    AIMgr.inst.repulsiveCoefficient * Mathf.Pow(p.diff.magnitude, AIMgr.inst.repulsiveExponent);
                }
            }
            attractivePotential = movePosition - entity.position;
            Vector3 tmp = attractivePotential.normalized;
            attractivePotential = tmp * 
                AIMgr.inst.attractionCoefficient * Mathf.Pow(attractivePotential.magnitude, AIMgr.inst.attractiveExponent);
            potentialSum = attractivePotential - repulsivePotential;
            dh = Utils.Degrees360(Mathf.Rad2Deg * Mathf.Atan2(potentialSum.x, potentialSum.z));
            angleDiff = Utils.Degrees360(Utils.AngleDiffPosNeg(dh, entity.heading));
            cosValue = (Mathf.Cos(angleDiff * Mathf.Deg2Rad) + 1) / 2.0f;
            ds = entity.maxSpeed * cosValue;
            
        }
        else
        {
            dh = ComputePotentialDHDS().dh;
            ds = ComputePotentialDHDS().ds;
        }
        return  new DHDS(dh, ds);    
    }

}
