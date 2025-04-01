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
    // public override void Tick()    {
    //     // if(entity.transform.position.y < 0 ) {
    //     //     FXMgr.inst.CreateExplosionAt(entity.position, 1);
    //     //     WeaponsMgr.inst.DestroyEntity(entity);
    //     //     return;
    //     // }
    //     // if(phx3d.altitude == 0 && phx3d.desiredAltitude == 0) {
    //     //     FXMgr.inst.CreateExplosionAt(entity.position, 1);
    //     //     WeaponsMgr.inst.DestroyEntity(entity);
    //     //     return;
    //     // }
    //     int boundary = 10000;
    //     if(entity.position.x > boundary || entity.position.x < -boundary || entity.position.z > boundary || entity.position.z < -boundary) {
            
    //         WeaponsMgr.inst.DestroyEntity(entity);
    //         return;
    //     }
    //     if(targetEntity == null) {
        
            
    //         base.Stop();

            
    //         entity.desiredSpeed = entity.maxSpeed; 
    //         // phx3d.desiredAltitude = 5;
    //         return;
    //     }
        
    //     base.Tick();
    //     entity.desiredSpeed = entity.maxSpeed;
    //     diffToMovePosition = targetEntity.position - entity.position;
    //     if(diffToMovePosition.sqrMagnitude < terminalPhaseDistanceSq)
    //         phx3d.desiredAltitude = 0;
    // }
    // public override bool IsDone()
    // {
    //     return diffToMovePosition.sqrMagnitude < 100;
    // }
    

public override void Tick()    
{
    
    int boundary = 10000;
        if(entity.position.x > boundary || entity.position.x < -boundary || entity.position.z > boundary || entity.position.z < -boundary) {
            WeaponsMgr.inst.DestroyEntity(entity);
            return;
        }
        if (targetEntity == null || !targetEntity.gameObject.activeSelf)
        {
        // Keep moving straight at current heading/speed
        entity.desiredHeading = entity.heading; // Maintain current heading
        entity.desiredSpeed = entity.maxSpeed;  // Continue at max speed
        diffToMovePosition = Vector3.positiveInfinity; // Disable distance checks
        return;
        }
        base.Tick();
    entity.desiredSpeed = entity.maxSpeed;
    diffToMovePosition = targetEntity.position - entity.position;
    // Set desired altitude to target's altitude during terminal phase
    if (diffToMovePosition.sqrMagnitude < terminalPhaseDistanceSq)
    {
        // Check if target has 3D physics (air entity) or adjust to minimum altitude
        if (targetEntity.TryGetComponent<Oriented3dPhysics>(out var targetPhx3d))
        {
            phx3d.desiredAltitude = targetPhx3d.altitude;
        }
        else
        {
            // Maintain a safe minimum altitude for ground targets
            phx3d.desiredAltitude = Mathf.Max(targetEntity.position.y, 10f);
        }
    }
}

public override bool IsDone()
{
    // Use a larger 3D distance threshold (e.g., 50 units)
    return diffToMovePosition.sqrMagnitude < 100; 
}
public override void Stop()    {
        base.Stop();
    }
}
