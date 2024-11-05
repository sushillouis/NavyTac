using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem.Controls;

[Serializable]
public class Group {
    public Entity target = null;
    public List<UnitAI> members;
    [SerializeField] float groupMinSpeed;
    // [SerializeField] Tactic _groupStrategy;
    private Queue<Tactic> tactics = new();
    public bool isActive = true;
    // public Tactic groupStrategy{
    //     get { return _groupStrategy; }
    //     set {
    //          _groupStrategy = value; 
    //          RebuildGroup();
    //     }
    // }
    public void Tick() {
        if(!isActive) {
            return;
        }
        if(tactics.Count == 0) {
            AddTactic(new GroupHold(this));
            RebuildGroup();
        }
        if(tactics.Peek().needsRebuild) {
            RebuildGroup();
        }
        if (tactics.Peek().IsDone()) {
            NextTactic();
        }
    }

    public void AddTactic(Tactic n_Tactic) {
        if(tactics.Count > 0 && tactics.Peek().type == TacticsType.None) {
            tactics.Peek().Stop();
            tactics.Dequeue();
        }
        tactics.Enqueue(n_Tactic);
        if(tactics.Count == 1) {
            tactics.Peek().Init();
            RebuildGroup();
        }
    }

    public void SetTactic(Tactic n_Tactic) {
        tactics.Clear();

        tactics.Enqueue(n_Tactic);
        tactics.Peek().Init();

        RebuildGroup();
    }

    public void NextTactic() {
        if(tactics.Count>1) {
            tactics.Peek().Stop();
            tactics.Dequeue();
            tactics.Peek().Init();
        } else if(tactics.Count==1) {
            tactics.Peek().Stop();
            tactics.Dequeue();
            tactics.Enqueue(new GroupHold(this));
            tactics.Peek().Init();
            RebuildGroup();
        } else {
            tactics.Enqueue(new GroupHold(this));
            tactics.Peek().Init();
        }
    }

    public Group(List<UnitAI> n_members, Tactic n_Tactic) {
        members = n_members;
        foreach (UnitAI ai in members) {
            ai.group=this;
        }
        target = FindTarget();
        AddTactic(n_Tactic);
    }

    public Group(List<UnitAI> n_members) {
        members = n_members;
        foreach (UnitAI ai in members) {
            ai.group=this;
        }
        target = FindTarget();
        AddTactic(new GroupHold(this));
    }

    public Entity FindTarget() {
        members.Sort();
        return members[0].entity;
    }
    public void Disband() {
        isActive = false;
        foreach (UnitAI ai in members)
        {
            ai.HardSetGroup(null);
            if (tactics.Count > 0) {
                tactics.Peek().Stop();
            }
        }
        tactics.Clear();
        members.Clear();
        TacticalAIMgr.inst.RemoveGroup(this);
    }

    public void AddMembers(UnitAI[] unitAIs) {
        foreach (UnitAI ai in unitAIs) {
            if(!members.Contains(ai)) {
                ai.group=this;
                members.Add(ai);
            }
        }
        members.Sort();
        Entity ent = members[0].entity;
        target =  ent;
        members[0].preOrderOffset=-1;
        RebuildGroup();
    }

    public void AddMembers(Entity[] entities) {
        List<UnitAI> aIs = new();

        foreach (Entity aEnt in entities) {
            UnitAI uai = aEnt.GetComponentInChildren<UnitAI>();
            aIs.Add(uai);
        }

        AddMembers(aIs.ToArray());
    }

    public void RemoveMembers(UnitAI[] unitAIs, bool rebuild = true) {
        if(!isActive) {
            return;
        }
        foreach (UnitAI ai in unitAIs.Where((x) => members.Contains(x))) {
            if(ai.group==this) {
                ai.HardSetGroup(null);
            }
            if(target.GetComponentInChildren<UnitAI>()==ai) {
                // Disband();
                return;
            }
            members.Remove(ai);
        }
        if(members.Count==0) {
            Disband();
            return;
        }
        if(rebuild) {
            RebuildGroup();
        }
    }

    public void RemoveMember(UnitAI unitAI, bool rebuild = true) {
        if(!isActive) {
            return;
        }
        if(members.Contains(unitAI)) {
            if(unitAI.group==this) {
                unitAI.HardSetGroup(null);
            }
            UnitAI targetAI = target.GetComponentInChildren<UnitAI>();
            if(targetAI==unitAI) {
                Disband();
                return;
            }
            members.Remove(unitAI);
        }

        if(members.Count==0) {
            Disband();
            return;
        }
        if(rebuild) {
            RebuildGroup();
        }
    }

    public void RebuildGroup() {
        if(!isActive) {
            return;
        }
        if(members.Count==0) {
            Disband();
            return;
        }
        tactics.Peek().Tick();
    }
}

