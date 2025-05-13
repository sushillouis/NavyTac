using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

[System.Serializable]
public class Intercept : Follow
{
    public Intercept(Entity ent, Entity target): base(ent, target, Vector3.zero)
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
        float dh = ComputePredictiveDH(targetEntity.transform.position);
        entity.desiredHeading = dh;
        entity.desiredSpeed = entity.maxSpeed;

        range = diffToMovePosition.magnitude;
        timeOnTarget = range / entity.speed;

    }

    public override bool IsDone()
    {
        return diffToMovePosition.sqrMagnitude < doneDistanceSq;
    }

    public override void Stop() {
        //base.Stop();

        // FXMgr.inst.CreateExplosionAt(entity.position, 1);

        entity.desiredSpeed = 0;
        entity.speed = 0;
        if (line != null)
        {
            line.gameObject.SetActive(false);
            line = null;
        }

        // targetEntity.desiredSpeed = 0;
        // Vector3 sunkenOffset = new Vector3(0, -5, 0);
        // targetEntity.GetComponentInChildren<UnitAI>().StopAndRemoveAllCommands();
        // targetEntity.GetComponentInChildren<OrientedPhysics>().enabled = false;
        // targetEntity.transform.position += sunkenOffset;

    }
    

}
