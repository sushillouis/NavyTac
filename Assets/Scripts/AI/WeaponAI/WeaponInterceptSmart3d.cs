using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WeaponInterceptSmart3d : Move
{
    Entity targetShip;
    OrientedPhysics targetPhysics;
    public WeaponInterceptSmart3d(Entity missile, Entity target): base(missile, target.transform.position){
        targetShip = target;
    }
    public override void Init()
    {
        base.Init();
        targetPhysics = targetShip.GetComponentInChildren<OrientedPhysics>();
        line = LineMgr.inst.CreateInterceptLine(entity.position, targetShip.position, targetShip.position);;
        
    }
}

