using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DumbMove : Move
{
    public DumbMove(Entity ent, Vector3 pos) : base(ent, pos)
    {
        
    }

    public override void Init()
    {
        base.Init(); 
    }

    public override void Tick()
    {
        DHDS dhds = ComputeDHDS();
        entity.desiredHeading = dhds.dh;
        entity.desiredSpeed = entity.maxSpeed;
        line.SetPosition(1, movePosition);
    }

}
