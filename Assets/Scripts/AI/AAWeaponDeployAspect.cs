using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AAWeaponDeployAspect : WeaponDeployAspect
{
    public override void SetTarget(Entity newTarget) {
        if(target!=null && target!=newTarget) {
            target.ai.OnDieEvent -= TargetDead;
        }
        if(newTarget!=null && target!=newTarget && newTarget.entityClass == EntityClass.Airplane) {
            newTarget.ai.OnDieEvent +=TargetDead;
            target=newTarget;
        }
    }
    public override void FireWeapon() {
        JudeAAMissile temp = WeaponsMgr.inst.LaunchAAMissile(unitAI.entity.position,target,new(0,unitAI.entity.heading,0),unitAI.entity.owner).GetComponent<JudeAAMissile>();
        temp.velocity = Quaternion.Euler(0,90,0)*unitAI.entity.velocity;
    }
}
