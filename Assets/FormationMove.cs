using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Principal;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;

[System.Serializable]
public class FormationMove : Follow
{
    Formation myFormation;
    public FormationMove(Entity ent, Entity target, Formation n_formation): base(ent, target, Vector3.left)
    {
        targetEntity = target;
        myFormation = n_formation;
    }

    public void UpdateFormation(Vector3 newOffset) {
        relativeOffset = newOffset;
    }

    public override void Stop()
    {
        myFormation.RemoveMember(entity.GetComponent<UnitAI>());
        base.Stop();
        entity.desiredSpeed = 0;
        isRunning = false;
    }
}

public class Formation {
    public Entity target;
    List<UnitAI> members;
    public Formation(Entity n_target) {
        target = n_target;
        members = new();
    }
    public void Dispand() {
        target.followers=0;
        foreach (UnitAI ent in members)
        {
            if(ent.commands.Count>0) {
                ent.commands[0].Stop();
            }
        }
        members.Clear();
    }

    public void AddMembers(UnitAI[] unitAIs) {
        foreach (UnitAI ai in unitAIs) {
            if(!members.Contains(ai)) {
                members.Add(ai);
                target.followers++;
            }
        }
        RebuildFormation();
    }

    public void RemoveMembers(UnitAI[] unitAIs, bool rebuild = true) {
        foreach (UnitAI ai in unitAIs) {
            if(members.Contains(ai)) {
                members.Remove(ai);
                target.followers--;
            }
        }
        if(members.Count==0) {
            AIMgr.inst.RemoveFormation(this);
            return;
        }
        if(rebuild) {
            RebuildFormation();
        }
    }

    public void RemoveMember(UnitAI unitAI, bool rebuild = true) {

        if(members.Contains(unitAI)) {
            members.Remove(unitAI);
            target.followers--;
        }

        if(members.Count==0) {
            AIMgr.inst.RemoveFormation(this);
            return;
        }
        if(rebuild) {
            RebuildFormation();
        }
    }

    public void RebuildFormation() {
        if(members.Count==0) {
            return;
        }

        Debug.Log("Rebuilding with: "+members.Count);

        members.Sort();

        int ring = 1;

        int filledRingMembers = 0;
        int remainingRingMembers = 0;

        for (int i = 0; i<members.Count;i++) {
            if(i-filledRingMembers>=ring*4) {
                filledRingMembers+=ring*4;
                ring++;
            }

            remainingRingMembers = Mathf.Min(members.Count-filledRingMembers, ring*4);

            Vector3 formationPos = new(0,0,0)
            {
                x = ((target.mass/10)+50) * ring * Mathf.Cos(Mathf.Deg2Rad * (i - filledRingMembers)*(360f / remainingRingMembers)),
                y=0,
                z = ((target.mass/10)+50) * ring * Mathf.Sin(Mathf.Deg2Rad * (i - filledRingMembers)*(360f / remainingRingMembers)),
            };

            if(members[i].commands[0] is FormationMove fMove) {
                fMove.UpdateFormation(formationPos);
            } else {
                RemoveMember(members[i],false);
            }
        }
    }
}
