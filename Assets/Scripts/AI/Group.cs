using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Group 
{
    public List<Entity> entities = new List<Entity>();
    public int groupNumber = -1;
    public bool isInitialized;
    public bool isDone;

    [SerializeField]
    private List<Tactic> tactics = new List<Tactic>();

    public Group(List<Entity> entities, int gn) {
        foreach(Entity ent in entities)
            this.entities.Add(ent);
        groupNumber = gn;
        tactics.Clear();
        isInitialized = false;
        isDone = false;
    }

    public bool isEntityInGroup(Entity entity) {
        return entities.Contains(entity);
    }

    public void AddEntity(Entity entity) {
        entities.Add(entity);
    }

    public void RemoveEntity(Entity entity) {
        entities.Remove(entity);
    }

    public void AddEntities(List<Entity> entitiesToAdd, bool shouldClear) {
        if(shouldClear)
            entities.Clear();

        foreach(Entity entity in entitiesToAdd) 
            if(!entities.Contains(entity))
                entities.Add(entity);
    }

    public void ClearEntities() {
        entities.Clear();
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

    [SerializeField]
    private List<FormationMoveTactic> formations = new List<FormationMoveTactic>();
    public void Init() {
        /*FormationMoveTactic fmt = new FormationMoveTactic(entities, new Vector3(0, 0, 2500));
        fmt.Init();
        tactics.Add(fmt);
        formations.Add(fmt);*/
        isInitialized = true;
    }

    public void CreateExecuteFormMove(Vector3 pos) {
        FormationMoveTactic fmt = new FormationMoveTactic(entities, pos);
        fmt.Init();
        tactics.Add(fmt);
        formations.Add(fmt);
        isInitialized= true;
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
