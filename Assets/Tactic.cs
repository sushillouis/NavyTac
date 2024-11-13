using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem.Controls;

public abstract class Tactic{
    public TacticsType type;
    public Group group;
    public bool needsRebuild = false;
    
    public abstract void Init();
    //Need to set isComplete Here!
    public abstract void Tick();
    public abstract void Stop();
    public abstract bool IsDone();
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

    public override bool IsDone()
    {
        return false;
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
            group.members[i].preOrderOffset=i;
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
                group.members[i].AddCommand(new Move(group.members[i].GetComponentInParent<Entity>(),group.target.position+groupPos));
            }

            if(group.members[i].commands[0] is Move move) {
                move.movePosition = group.target.position+groupPos;
            }
        }
    }
}

class GroupNull : Tactic {
    public GroupNull(Group n_group) {
        group = n_group;
        type = TacticsType.None;
        
    }

    public override void Init() {
        foreach (UnitAI aI in group.members) {
            if (aI.entity != group.target) {
                aI.StopAndRemoveAllCommands();
            }
        }
    }

    public override bool IsDone() {
        return false;
    }

    public override void Stop() {
    }

    public override void Tick() {

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

    public override bool IsDone()
    {
        UnitAI targetAI = group.target.ai;
        if(targetAI.commands.Count>0 && targetAI.commands[0] is GroupTargetMove gMove) {
            return gMove.IsDoneGroup();
        }
        return true;
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
            group.members[i].preOrderOffset=i;
            //This is big bad
            Entity ent = group.members[i].GetComponentInParent<Entity>();
            Vector2 degRange = new(0,360);
            float distanceSclar = 1f;
            int ringDensity = 4;
            float angleOffset = 90;
            switch (ent.shipClass) {
                case ShipClasses.Carrier:
                    ringDensity=1;
                    break;
                case ShipClasses.Destroyer:
                    distanceSclar = 1.5f;
                    angleOffset = 45f;
                    break;
                case ShipClasses.Cruiser:
                    angleOffset = 45f;
                    break;
                case ShipClasses.USV:
                    ringDensity=2;
                    degRange = new(-80,80);
                    distanceSclar = 5f;
                    break;
                case ShipClasses.Tug:
                case ShipClasses.Merchant:
                case ShipClasses.Supply:
                    degRange = new(120,230);
                    distanceSclar = 5f;
                    break;
                default:
                    break;
            }

            if((i-1)-filledRingMembers>=ring*ringDensity) {
                filledRingMembers+=ring*ringDensity;
                ring++;
            }
            float baseDist = distanceSclar*group.target.length+group.members[i].entity.length+50;

            if(group.members[i].entity == group.target) {
                continue;
            }

            int remainingRingMembers = Mathf.Min(group.members.Count -1 - filledRingMembers, ring * ringDensity);
            float angle =  UnityEngine.Mathf.Lerp(degRange[0],degRange[1],(float)(i -1 - filledRingMembers) / remainingRingMembers)+angleOffset;
            Vector3 groupPos = new(0,0,0)
            {
                x =  baseDist * ring * Mathf.Cos(Mathf.Deg2Rad * angle),
                y=0,
                z = baseDist * ring * Mathf.Sin(Mathf.Deg2Rad * angle),
            };
            if (group.members[i].commands.Count == 0) {
                group.members[i].AddCommand(new GroupEscort(ent,group.target,Vector3.zero));
            }

            if(group.members[i].commands[0] is GroupEscort escort) {
                escort.Update(groupPos);
            }
        }
    }
}
