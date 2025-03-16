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
        // if(entity.transform.position.y < 0 ) {
        //     FXMgr.inst.CreateExplosionAt(entity.position, 1);
        //     WeaponsMgr.inst.DestroyEntity(entity);
        //     return;
        // }
        // if(phx3d.altitude == 0 && phx3d.desiredAltitude == 0) {
        //     FXMgr.inst.CreateExplosionAt(entity.position, 1);
        //     WeaponsMgr.inst.DestroyEntity(entity);
        //     return;
        // }
        int boundary = 10000;
        if(entity.position.x > boundary || entity.position.x < -boundary || entity.position.z > boundary || entity.position.z < -boundary) {
            
            WeaponsMgr.inst.DestroyEntity(entity);
            return;
        }
        if(targetEntity == null) {
            float currentSpeed = entity.speed;
            base.Stop();
            entity.speed = currentSpeed;
            entity.desiredSpeed = currentSpeed; 
            // phx3d.desiredAltitude = 5;
            return;
        }
        
        base.Tick();
        diffToMovePosition = targetEntity.position - entity.position;
        if(diffToMovePosition.sqrMagnitude < terminalPhaseDistanceSq)
            phx3d.desiredAltitude = 10;
    }
    public override bool IsDone()
    {
        return diffToMovePosition.sqrMagnitude < 100;
    }
    public override void Stop()    {
        base.Stop();
    }


}
