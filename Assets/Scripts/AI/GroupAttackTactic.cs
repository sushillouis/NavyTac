using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GroupAttackTactic : Tactic
{
    [SerializeField]
    private Vector3 movePosition;
    [SerializeField] List<Entity> targets;
    public GroupAttackTactic(List<Entity> ents, List<Entity> n_targets) : base(ents) {
        tacticsType = TacticsType.EscortMove;
        targets = n_targets;
    }
    public override void Init() {
        base.Init();

        foreach(Entity ent in entities) {
            ent.ai.StopAndRemoveAllCommands();
        }

        EqualAttack();

    }


    void EqualAttack() {
        if(targets.Count==0) {
            return;
        }
        int surfaceIterator = 0;
        int subIterator = 0;
        List<Entity> subs = entities.FindAll(x => x.entityClass == EntityClass.Sub);
        List<Entity> notSubs = entities.FindAll(x => x.entityClass != EntityClass.Sub);
        notSubs.Sort();
        targets.Sort(new EntityStrengthCompararer());
        float entRatio = entities.Count/targets.Count; 
        foreach (Entity ent in targets)
        {
            switch(ent.entityClass) {
                case EntityClass.Sub:
                    for(int countUp = 0;countUp < entRatio;countUp++){
                        if(subIterator < subs.Count){
                            subs[subIterator].ai.AddCommand(new Intercept(subs[subIterator],ent));
                            subIterator++;
                        }
                    }
                    
                break;
                case EntityClass.Carrier:
                case EntityClass.Cruiser:
                case EntityClass.Destroyer:
                case EntityClass.Frigate:
                case EntityClass.LHA:
                case EntityClass.USV:
                    for(int countUp = 0;countUp < entRatio;countUp++){
                        if(surfaceIterator >= notSubs.Count) {
                            continue;
                        }
                        notSubs[surfaceIterator].ai.AddCommand(new Intercept(notSubs[surfaceIterator],ent));
                        surfaceIterator++;
                    }
                break;
                default:
                    
                break;
            }
        }

        while (surfaceIterator<notSubs.Count)
        {
            notSubs[surfaceIterator].ai.AddCommand(new Intercept(notSubs[surfaceIterator],targets[0]));
            surfaceIterator++;
        }

        while (subIterator<subs.Count)
        {
            notSubs[surfaceIterator].ai.AddCommand(new Intercept(notSubs[surfaceIterator],targets[0]));
            surfaceIterator++;
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
