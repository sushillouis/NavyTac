using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

[System.Serializable]
public class Intercept : Follow
{
    public Intercept(Entity ent, Entity target) : base(ent, target, Vector3.zero)
    {
        //Follow does all the work
    }

    public override void Init()
    {
        //Debug.Log("Intercept:\t ing: " + targetEntity.gameObject.name);
        line = LineMgr.inst.CreateInterceptLine(entity.position, targetEntity.position, targetEntity.position);
        line.gameObject.SetActive(false);
    }

    public override void Tick()
    {
        //movePosition = targetEntity.transform.position;
        if (targetEntity != null)
        {
            // Compute predictive heading towards the target
            float dh = ComputePredictiveDH(Vector3.zero);
            entity.desiredHeading = dh;
            entity.desiredSpeed = entity.maxSpeed;
        }
        else
        {
            if (WeaponMgr.inst.weapons.Contains(entity))
            {
                entity.desiredHeading = entity.heading;
                entity.desiredSpeed = entity.maxSpeed;
            }
            else
            {
                Stop();
            }

        }
    }

    public override bool IsDone()
    {
        return diff.sqrMagnitude < doneDistanceSq;
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

}
