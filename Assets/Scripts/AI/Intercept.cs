using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

[System.Serializable]
public class Intercept : Follow
{
    float missileLaunchTimer = 2f;
    int missileCount = 20;
    float missileLaunchCooldown = 2f;
    bool isDoneOverride = false;
    public Intercept(Entity ent, Entity target): base(ent, target, Vector3.zero)
    {
        //Follow does all the work

    }

    public void TargetDead(Entity target) {
        Stop();
        isDoneOverride=true;
    }

    public override void Init()
    {
        //Debug.Log("Intercept:\t ing: " + targetEntity.gameObject.name);
        line = LineMgr.inst.CreateInterceptLine(entity.position, targetEntity.position, targetEntity.position);
        line.gameObject.SetActive(false);
        targetEntity.ai.OnDieEvent+= TargetDead;
        entity.ai.SetAllWeaponsTarget(targetEntity);
        entity.ai.SetAllWeaponsActive(true);
    }

    public override void Tick()
    {
        //movePosition = targetEntity.transform.position;
        float dh = ComputePredictiveDH(targetEntity.transform.position);
        entity.desiredHeading = dh;
        entity.desiredSpeed = entity.maxSpeed;

        range = diffToMovePosition.magnitude;
        timeOnTarget = range / entity.speed;

        // if(missileLaunchTimer<=0 && entity.entityClass == EntityClass.Destroyer) {
        //     missileLaunchTimer = 2f;
        //     WeaponsMgr.inst.LaunchCruseMissile(entity.position,targetEntity,new(-90,0,0));
        // } else if(missileLaunchTimer<=0 && entity.entityClass == EntityClass.Airplane) {
        //     missileLaunchTimer = 2f;
        //     if(targetEntity.entityClass==EntityClass.Airplane) {
        //         JudeAAMissile temp = WeaponsMgr.inst.LaunchAAMissile(entity.position,targetEntity,new(0,entity.heading,0)).GetComponent<JudeAAMissile>();
        //         temp.velocity = Quaternion.Euler(0,90,0)*entity.velocity;
        //     } else {
        //         JudeMissile temp = WeaponsMgr.inst.LaunchCruseMissile(entity.position,targetEntity,new(0,entity.heading,0)).GetComponent<JudeMissile>();
        //         temp.velocity = Quaternion.Euler(0,90,0)*entity.velocity;
        //         temp.phase = 1;
        //     }
        // } else {    
        //     missileLaunchTimer-=Time.deltaTime;
        // }
    }

    public override bool IsDone()
    {
        return diffToMovePosition.sqrMagnitude < doneDistanceSq || isDoneOverride;
    }

    public override void Stop() {
        //base.Stop();

        // FXMgr.inst.CreateExplosionAt(entity.position, 1);

        entity.desiredSpeed = 0;
        entity.speed = 0;

        LineMgr.inst.DestroyLR(line);

        line=null;
        
        targetEntity.ai.OnDieEvent-= TargetDead;
        entity.ai.SetAllWeaponsTarget(null);
        entity.ai.SetAllWeaponsActive(false);

        // targetEntity.desiredSpeed = 0;
        // Vector3 sunkenOffset = new Vector3(0, -5, 0);
        // targetEntity.GetComponentInChildren<UnitAI>().StopAndRemoveAllCommands();
        // targetEntity.GetComponentInChildren<OrientedPhysics>().enabled = false;
        // targetEntity.transform.position += sunkenOffset;

    }


}
