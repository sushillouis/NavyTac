using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem.Controls;

public abstract class Tactic{
    public TacticsType type;
    public Group group;
    // If Group Action is Done
    public bool isComplete = false;
    public bool needsRebuild = false;
    
    public abstract void Init();
    //Need to set isComplete Here!
    public abstract void Tick();
    public abstract void Stop();
}

class GroupHold : Tactic {
    public GroupHold(Group n_group) {
        group = n_group;
        type = TacticsType.None;
        
    }

    public override void Init() {
        foreach (UnitAI aI in group.members) {
            if (aI.GetComponentInParent<Entity>() != group.target) {
                GroupTargetMove escort = new GroupTargetMove(aI.GetComponentInParent<Entity>(), group.target.position);
                aI.SetCommand(escort);
            }
        }
    }

    public override void Stop()
    {
        foreach (UnitAI aI in group.members) {
            if (aI.commands.Count > 0) {
                
                aI.StopAndRemoveAllCommands();
            }
        }
    }

    public override void Tick()
    {
        group.members.Sort();

        int ring = 1;

        int filledRingMembers = 0;


        for (int i = 0; i<group.members.Count;i++) {
            if((i-1)-filledRingMembers>=ring*4) {
                filledRingMembers+=ring*4;
                ring++;
            }
            float baseDist = group.target.length+group.members[i].GetComponentInParent<Entity>().length+100;

            if(group.members[i].GetComponentInParent<Entity>() == group.target) {
                continue;
            }

            int remainingRingMembers = Mathf.Min(group.members.Count -1 - filledRingMembers, ring * 4);
            Vector3 groupPos = new(0,0,0)
            {
                x =  baseDist * ring * Mathf.Cos(Mathf.Deg2Rad * (i -1 - filledRingMembers)*(360f / remainingRingMembers)),
                y=0,
                z = baseDist * ring * Mathf.Sin(Mathf.Deg2Rad * (i -1 - filledRingMembers)*(360f / remainingRingMembers)),
            };

            if (group.members[i].commands.Count == 0) {
                group.members[i].AddCommand(new Move(group.members[i].GetComponentInParent<Entity>(),group.target.position+groupPos));
            }

            if(group.members[i].commands[0] is Move move) {
                move.movePosition = group.target.position+groupPos;
            }
        }
    }
}

class EscortTactic : Tactic
{
    Vector3 destination;
    public EscortTactic(ref Group n_group, Vector3 point) {
        group = n_group;
        type = TacticsType.Formate;
        destination = point;
    }

    public override void Init() {
        foreach (UnitAI aI in group.members) {
            if (aI.GetComponentInParent<Entity>() == group.target) {
                GroupTargetMove m = new GroupTargetMove(group.target, destination);
                aI.SetCommand(m);
            } else {
                GroupEscort escort = new GroupEscort(aI.GetComponentInParent<Entity>(), group.target, Vector3.zero);
                aI.SetCommand(escort);
            }
        }
    }

    public override void Stop()
    {
        foreach (UnitAI aI in group.members) {
            if (aI.commands.Count > 0) {
                aI.StopAndRemoveAllCommands();
            }
        }
    }

    public override void Tick()
    {
        if(group.target.GetComponentInChildren<UnitAI>().commands[0].IsDone()) {
            isComplete=true;
        }
        group.members.Sort();

        int ring = 1;

        int filledRingMembers = 0;


        for (int i = 0; i<group.members.Count;i++) {
            if((i-1)-filledRingMembers>=ring*4) {
                filledRingMembers+=ring*4;
                ring++;
            }
            float baseDist = group.target.length+group.members[i].GetComponentInParent<Entity>().length;

            if(group.members[i].GetComponentInParent<Entity>() == group.target) {
                continue;
            }

            int remainingRingMembers = Mathf.Min(group.members.Count -1 - filledRingMembers, ring * 4);
            Vector3 groupPos = new(0,0,0)
            {
                x =  baseDist * ring * Mathf.Cos(Mathf.Deg2Rad * (i -1 - filledRingMembers)*(360f / remainingRingMembers)),
                y=0,
                z = baseDist * ring * Mathf.Sin(Mathf.Deg2Rad * (i -1 - filledRingMembers)*(360f / remainingRingMembers)),
            };
            if (group.members[i].commands.Count == 0) {
                group.members[i].AddCommand(new GroupEscort(group.members[i].GetComponentInParent<Entity>(),group.target,Vector3.zero));
            }

            if(group.members[i].commands[0] is GroupEscort escort) {
                escort.Update(groupPos);
            }
        }
    }
}
