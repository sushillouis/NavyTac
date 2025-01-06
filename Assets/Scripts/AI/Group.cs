using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Activity
{
    None,
    Forming,
    Scout,
    Persue,
    Fighting,
    Retreating
}

[Serializable]
public class Group 
{
    public Entity target = null;
    public List<Entity> groupEntities = new List<Entity>();
    public int groupNumber = -1;
    public bool isInitialized;
    public bool isDone;
    public int totalCost;
    public int totalStrength;
    // This is just for the AI;
    public Activity activity = Activity.None;

    [SerializeField]
    private List<Tactic> tactics = new List<Tactic>();

    public Group(List<Entity> ents, int gn) {
        groupEntities.Clear();
        foreach(Entity ent in ents)
            groupEntities.Add(ent);
        groupNumber = gn;
        tactics.Clear();
        isInitialized = false;
        isDone = false;
        totalCost = -1;
        totalStrength = -1;
    }

    public void FindTarget() {
        groupEntities.Sort();
        target = groupEntities[0];
    }

    public int GetTotalStrength() {
        totalStrength = 0;
        foreach(Entity entity in groupEntities) 
            totalStrength+=Utils.strengthDict[entity.entityType];

        return totalStrength;
    } 
    public int GetTotalCost() {
        totalCost = 0;
        foreach(Entity entity in groupEntities) 
            totalCost+=Utils.costDict[entity.entityType];
        
        return totalCost;
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

    [SerializeField]
    private List<FormationMoveTactic> formations = new List<FormationMoveTactic>();
    public void Init() {
        /*FormationMoveTactic fmt = new FormationMoveTactic(entities, new Vector3(0, 0, 2500));
        fmt.Init();
        tactics.Add(fmt);
        formations.Add(fmt);*/
        isInitialized = true;
    }

    public void CreateExecuteEscortMove(Vector3 pos) {
        FormationMoveTactic fmt = new FormationMoveTactic(groupEntities, pos);
        fmt.Init();
        tactics.Add(fmt);
        formations.Add(fmt);
        isInitialized= true;
    }

    public void CreateExecuteAttack(List<Entity> targets) {
        FormationAttackTactic fmt = new FormationAttackTactic(groupEntities, targets);
        fmt.Init();
        tactics.Add(fmt);
        // formations.Add(fmt);
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

public class GroupProximityComparer: IComparer<Group> {

    Vector3 location;
    public GroupProximityComparer(Vector3 location) {
        this.location = location;
        
    }   
    public int Compare(Group left, Group right)
    {
        if(left != null && right != null) {
            if(left.target==null) {
                left.FindTarget();
            }
            
            if(right.target==null) {
                right.FindTarget();
            }
            float mag1 = Vector3.Distance(location, left.target.position);
            float mag2 = Vector3.Distance(location, right.target.position);
            return mag1 > mag2 ? -1 : 1;
        }

        if(right == null && left ==null) {
            return 0;
        }
        if(left!=null) {
            return -1;
        }
        return 1;
    }
}
