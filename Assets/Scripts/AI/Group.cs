using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Group 
{
    public List<Entity> groupEntities = new List<Entity>();
    public int groupNumber = -1;
    public bool isInitialized = false;
    public bool isDone = false;
    public bool isControl = false;

    public List<Tactic> tactics = new List<Tactic>();
    public List<FormationMoveTactic> formations = new List<FormationMoveTactic>();

    public Group(List<Entity> ents, int gn) {
        Construct(ents, gn);
        if(gn >= 0)
            isControl = true;

    }

    public Group(List<Entity> ents) {
        Construct(ents, -1);
        isControl = false;
    }
    
    public void Construct(List<Entity> ents, int gn) {
        groupEntities.Clear();
        foreach(Entity ent in ents)
            groupEntities.Add(ent);//copy the entity pointers to this list
        groupNumber = gn;
        tactics.Clear();
        formations.Clear();
        isInitialized = false;
        isDone = false;
    }

    public bool isEntityInGroup(Entity entity) {
        return groupEntities.Contains(entity);
    }

    public void AddEntity(Entity entity) {
        groupEntities.Add(entity);
    }

    public void RemoveEntity(Entity entity) {
        groupEntities.Remove(entity);
    }

    public void AddEntities(List<Entity> entitiesToAdd, bool shouldClear) {
        if(shouldClear)
            groupEntities.Clear();

        foreach(Entity entity in entitiesToAdd) 
            if(!groupEntities.Contains(entity))
                groupEntities.Add(entity);
    }

    public void ClearEntities() {
        groupEntities.Clear();
    }

    public void AddTactic(Tactic tactic) {
        tactics.Add(tactic);
    }

    public void SetTactic(Tactic tactic) {
        tactics.Clear();
        tactics.Add(tactic);
    }

    public void Stop() {
        if(groupNumber < 0)
            isDone = true;
    }

    public void Init() {
        isInitialized = true;
    }


    public void Tick() {
        if(tactics.Count > 0) {
            if(tactics[0].IsDone()) {
                tactics[0].Stop();
                tactics.RemoveAt(0);
            } else {
                tactics[0].Tick();
            }
        }
        if(isInitialized && tactics.Count == 0)
            Stop();
    }

}
