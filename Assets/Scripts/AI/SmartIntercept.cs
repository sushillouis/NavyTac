using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

[System.Serializable]
public class SmartIntercept : Intercept
{
    public SmartIntercept(Entity ent, Entity target) : base(ent, target)
    {
    }

    public override void Init()
    {
        base.Init(); 
    }

    public override void Tick()
    {
        //movePosition = targetEntity.transform.position; 
        // float dh = ComputePotentialPredictiveDHDS(Vector3.zero).dh;
        // float ds = ComputePotentialPredictiveDHDS(Vector3.zero).ds;
        // entity.desiredHeading = dh;
        // entity.desiredSpeed = ds;
        // range = diff.magnitude;
        // timeOnTarget = range / entity.speed;
    }

    

}
