using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.Controls;

[Serializable]
public class Formation {
    public Entity target = null;
    [SerializeReference] List<UnitAI> members;
    [SerializeField] IFormationStrategy _formationStrategy;
    public IFormationStrategy formationStrategy{
        get { return _formationStrategy; }
        set {
             _formationStrategy = value; 
             RebuildFormation();
        }
    }
    
    public Formation(Entity n_target) {
        target = n_target;
        members = new();
        _formationStrategy = new FormationHold();
    }

    public Formation(List<UnitAI> n_members) {
        members = n_members;
        foreach (UnitAI ai in members) {
            ai.formation=this;
        }
        target = FindTarget();
        _formationStrategy = new FormationHold();
    }

    public Entity FindTarget() {
        members.Sort();
        return members[0].GetComponent<Entity>();
    }
    public void Disband() {
        AIMgr.inst.RemoveFormation(this);
        if(target.GetComponent<UnitAI>().formation==this) {
            target.GetComponent<UnitAI>().formation=null;
        }
        foreach (UnitAI ai in members)
        {
            if(ai.commands.Count>0) {
                ai.commands[0].Stop();
                if(ai.formation==this) {
                    ai.formation=null;
                }
            }
        }
        members.Clear();
    }

    public void AddMembers(UnitAI[] unitAIs) {
        foreach (UnitAI ai in unitAIs) {
            if(!members.Contains(ai)) {
                ai.formation=this;
                members.Add(ai);
            }
        }
        RebuildFormation();
    }

    public void RemoveMembers(UnitAI[] unitAIs, bool rebuild = true) {
        foreach (UnitAI ai in unitAIs.Where((x) => members.Contains(x))) {
            if(ai.formation==this) {
                ai.formation=null;
            }
            if(target.GetComponent<UnitAI>()==ai) {
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
            RebuildFormation();
        }
    }

    public void RemoveMember(UnitAI unitAI, bool rebuild = true) {

        if(members.Contains(unitAI)) {
            if(unitAI.formation==this) {
                unitAI.formation=null;
            }
            if(target.GetComponent<UnitAI>()==unitAI) {
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
            RebuildFormation();
        }
    }

    public void RebuildFormation() {
        if(members.Count==0) {
            Disband();
            return;
        }

        formationStrategy.UpdateFormation(ref members,this);
    }
}

public interface IFormationStrategy{
    public void UpdateFormation(ref List<UnitAI> unitAIs, Formation formation);
}

class FormationHold : IFormationStrategy {

    public void UpdateFormation(ref List<UnitAI> unitAIs, Formation formation)
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
            Vector3 formationPos = new(0,0,0)
            {
                x = ((formation.target.mass/10)+50) * ring * Mathf.Cos(Mathf.Deg2Rad * (i - filledRingMembers)*(360f / remainingRingMembers)),
                y=0,
                z = ((formation.target.mass/10)+50) * ring * Mathf.Sin(Mathf.Deg2Rad * (i - filledRingMembers)*(360f / remainingRingMembers)),
            };

            if(unitAIs[i].commands.Count > 0 && unitAIs[i].commands[0] is Move fMove) {
                fMove.movePosition = formation.target.position+formationPos;
            }
        }
    }
}

class CircleEscortMove : IFormationStrategy
{
    Vector3 movePos = Vector3.zero;
    public CircleEscortMove(Vector3 n_movePos, Entity moveCenter) {
        movePos = n_movePos;
        moveCenter.GetComponent<UnitAI>().SetCommand(new Move(moveCenter, movePos));
    }
    public void UpdateFormation(ref List<UnitAI> unitAIs, Formation formation)
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
            Vector3 formationPos = new(0,0,0)
            {
                x = ((formation.target.mass/10)+50) * ring * Mathf.Cos(Mathf.Deg2Rad * (i - filledRingMembers)*(360f / remainingRingMembers)),
                y=0,
                z = ((formation.target.mass/10)+50) * ring * Mathf.Sin(Mathf.Deg2Rad * (i - filledRingMembers)*(360f / remainingRingMembers)),
            };

            if(unitAIs[i].commands.Count > 0 && unitAIs[i].commands[0] is EscortFormate fMove) {
                fMove.UpdateFormation(formationPos);
            }
        }
    }
}
