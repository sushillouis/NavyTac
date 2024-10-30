using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem.Controls;

public abstract class Tactic{
    public abstract void UpdateGroup(ref List<UnitAI> unitAIs, Group group);
}

class GroupHold : Tactic {

    public override void UpdateGroup(ref List<UnitAI> unitAIs, Group group)
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
    public override void UpdateGroup(ref List<UnitAI> unitAIs, Group group)
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
