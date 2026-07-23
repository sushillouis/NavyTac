using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FormationMoveTactic : Tactic
{
    [SerializeField]
    private Vector3 movePosition;



    public FormationMoveTactic(List<Entity> ents, Vector3 movePos) : base(ents) {
        tacticsType = TacticsType.EscortMove;
        movePosition = movePos;
    }
    public override void Init() {
        base.Init();

        foreach(Entity ent in entities) {
            ent.ai.StopAndRemoveAllCommands();
        }

        highValueUnit = null;
        if(entities.Exists(x => x.entityType == EntityType.CVN75)) {
            highValueUnit = entities.Find(x => x.entityType == EntityType.CVN75);
        } else if(entities.Exists(x => x.entityType == EntityType.DDG51)) {
            highValueUnit = entities.Find(x => x.entityType == EntityType.DDG51);
        } else {
            highValueUnit = entities[0];
        }

        AIMgr.inst.HandleMove(new List<Entity> { highValueUnit }, movePosition, false);
        highValueUnit.desiredSpeed = highValueUnit.speed = highValueUnit.maxSpeed * 0.6f; // let escorts catch up

        List<Entity> capitalShips = entities.FindAll(x => x.entityClass == EntityClass.Destroyer);
        CircleFormate(highValueUnit, capitalShips, 1852f);

        List<EntityRole> supportRoles = new List<EntityRole> { EntityRole.ISR, EntityRole.ASW, EntityRole.SuicideAttack };
        List<Entity> others = entities.FindAll(x => supportRoles.Contains(x.entityRole));
        int perEnt = others.Count / (capitalShips.Count + 1);
        int start = 0;
        foreach(Entity ent in capitalShips) {
            ////Debug.Log($"start: {start}, perEnt: {perEnt}");
            CircleFormate(ent, others.GetRange(start, perEnt), 1852);
            start += perEnt;
        }
        CircleFormate(highValueUnit, others.GetRange(start, others.Count - start), 1852);
    }


    void CircleFormate(Entity hvu, List<Entity> esc, float distance) {

        List<Entity> escorts = new List<Entity> ();
        escorts.AddRange(esc); //don't change the passed in list, make a local shallow copy and use it

        if(escorts.Count > 0) {
            float deltaAngle = 360 / escorts.Count; //360 degrees divided by numbr of escorts
            float startAngle;
            if(escorts.Count % 2 == 0) {
                startAngle = Utils.Degrees360(deltaAngle / 2); //if even distribute evenly around sides
            } else {
                startAngle = Utils.Degrees360(deltaAngle); //if odd start from ahead
            }

            Vector3 offset = RoRMath.VectorFromAngle(startAngle).normalized * distance;
            int count = escorts.Count;
            for(int i = 0; i < count; i++) {
                Entity closestEnt = FindClosest(hvu, offset, escorts);
                escorts.Remove(closestEnt);
                AIMgr.inst.HandleFollow(new List<Entity> { closestEnt }, hvu, offset, false);
                startAngle = Utils.Degrees360(startAngle + deltaAngle);
                offset = RoRMath.VectorFromAngle(startAngle).normalized * distance;
            }
        }
    }

    Entity FindClosest(Entity hvu, Vector3 relativeOffset, List<Entity> escorts) {
        float minDistanceSq = float.MaxValue;
        float distSq;
        Entity minEnt = null;
        Vector3 worldPos = hvu.transform.position + hvu.transform.InverseTransformVector(relativeOffset);
        foreach(Entity ent in escorts) {
            distSq = (worldPos - ent.transform.position).sqrMagnitude;
            if(distSq < minDistanceSq) {
                minEnt = ent;
                minDistanceSq = distSq;
            }
        }

        return minEnt;
    }

    public override bool IsDone() {
        return false;
    }

    public override void Stop() {
        
    }

    public override void Tick() {

    }
}
