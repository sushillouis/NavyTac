using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Intercept3d : Intercept
{
    public Intercept3d(Entity entity, Entity target) : base(entity, target)    {
    }
    Oriented3dPhysics phx3d;
    public float terminalPhaseDistanceSq;
    public override void Init()    {
        base.Init();
        phx3d = entity.GetComponentInChildren<Oriented3dPhysics>();
        if(phx3d != null) {
            terminalPhaseDistanceSq = entity.maxSpeed * entity.maxSpeed * 9;
        } else {
            terminalPhaseDistanceSq = 0;
        }
    }

    public override void Tick()    {
        base.Tick();
        diffToMovePosition = targetEntity.position - entity.position;
        if(diffToMovePosition.sqrMagnitude < terminalPhaseDistanceSq)
            phx3d.desiredAltitude = 0;
    }


}
