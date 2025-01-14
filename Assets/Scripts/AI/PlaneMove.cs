using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlaneMove : Move
{
    public PlaneMove(Entity ent, Vector3 pos) : base(ent, pos) {
    }

    public override void Init() {
        //Debug.Log("MoveInit:\tMoving to: " + movePosition);
        line = LineMgr.inst.CreateMoveLine(entity.position, movePosition);
        line.gameObject.SetActive(false);
    }


    public override void Tick() {
        
        entity.desiredHeading = Utils.Degrees360(Vector3.SignedAngle(Vector3.back, entity.position-movePosition,Vector3.up));
        entity.desiredSpeed = 1f;
        line.SetPosition(1, movePosition+entity.position.y*Vector3.up);

        range = diffToMovePosition.magnitude;
        timeOnTarget = range / entity.speed;

    }
}
