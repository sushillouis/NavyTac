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
    [SerializeField] Tactic _groupStrategy;
    public Tactic groupStrategy{
        get { return _groupStrategy; }
        set {
             _groupStrategy = value; 
             RebuildGroup();
        }
    }
    
    
    public Group(Entity n_target) {
        target = n_target;
        members = new();
        _groupStrategy = new GroupHold();
    }

    public Group(List<UnitAI> n_members) {
        members = n_members;
        foreach (UnitAI ai in members) {
            ai.group=this;
        }
        target = FindTarget();
        _groupStrategy = new GroupHold();  
    }

    public Entity FindTarget() {
        members.Sort();
        return members[0].GetComponentInParent<Entity>();
    }
    public void Disband() {
        TacticalAIMgr.inst.RemoveGroup(this);
        if(target.GetComponentInChildren<UnitAI>().group==this) {
            target.GetComponentInChildren<UnitAI>().group=null;
        }
        foreach (UnitAI ai in members)
        {
            if(ai.commands.Count>0) {
                ai.commands[0].Stop();
                if(ai.group==this) {
                    ai.group=null;
                }
            }
        }
        members.Clear();
    }

    public void AddMembers(UnitAI[] unitAIs) {
        foreach (UnitAI ai in unitAIs) {
            if(!members.Contains(ai)) {
                ai.group=this;
                members.Add(ai);
            }
        }
        RebuildGroup();
    }

    public void RemoveMembers(UnitAI[] unitAIs, bool rebuild = true) {
        foreach (UnitAI ai in unitAIs.Where((x) => members.Contains(x))) {
            if(ai.group==this) {
                ai.group=null;
            }
            if(target.GetComponentInChildren<UnitAI>()==ai) {
                Disband();
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

        if(members.Contains(unitAI)) {
            if(unitAI.group==this) {
                unitAI.group=null;
            }
            if(target.GetComponentInChildren<UnitAI>()==unitAI) {
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
        if(members.Count==0) {
            Disband();
            return;
        }

        groupStrategy.UpdateGroup(ref members,this);
    }
}

public interface Tactic{
    public void UpdateGroup(ref List<UnitAI> unitAIs, Group group);
}

class GroupHold : Tactic {

    public void UpdateGroup(ref List<UnitAI> unitAIs, Group group)
    {
        unitAIs.Sort();

        int ring = 1;

        int filledRingMembers = 0;
        for (int i = 0; i<unitAIs.Count;i++) {
            if(i-filledRingMembers>=ring*4) {
                filledRingMembers+=ring*4;
                ring++;
            }

            int remainingRingMembers = Mathf.Min(unitAIs.Count - filledRingMembers, ring * 4);
            Vector3 groupPos = new(0,0,0)
            {
                x = ((group.target.mass/10)+50) * ring * Mathf.Cos(Mathf.Deg2Rad * (i - filledRingMembers)*(360f / remainingRingMembers)),
                y=0,
                z = ((group.target.mass/10)+50) * ring * Mathf.Sin(Mathf.Deg2Rad * (i - filledRingMembers)*(360f / remainingRingMembers)),
            };

            // if(unitAIs[i].commands.Count > 0 && unitAIs[i].commands[0] is EscortFormate fMove) {
            //     fMove.UpdateGroup(groupPos);
            // }
        }
    }
}

class CircleEscortMove : Tactic
{
    public CircleEscortMove() {
    
    }
    public void UpdateGroup(ref List<UnitAI> unitAIs, Group group)
    {
        unitAIs.Sort();

        int ring = 1;

        int filledRingMembers = 0;

        float baseDist = 2*Mathf.Log10(group.target.mass)+200;

        for (int i = 0; i<unitAIs.Count;i++) {
            if(i-filledRingMembers>=ring*4) {
                filledRingMembers+=ring*4;
                ring++;
            }

            if(unitAIs[i].GetComponentInParent<Entity>() == group.target) {
                continue;
            }

            int remainingRingMembers = Mathf.Min(unitAIs.Count - 1 - filledRingMembers, ring * 4);
            Vector3 groupPos = new(0,0,0)
            {
                x =  baseDist * ring * Mathf.Cos(Mathf.Deg2Rad * (i - filledRingMembers - 1)*(360f / remainingRingMembers)),
                y=0,
                z = baseDist * ring * Mathf.Sin(Mathf.Deg2Rad * (i - filledRingMembers - 1)*(360f / remainingRingMembers)),
            };

            if(unitAIs[i].commands.Count > 0 && unitAIs[i].commands[0] is EscortFormate escort) {
                escort.relativeOffset = groupPos;
            }
        }
    }
}
