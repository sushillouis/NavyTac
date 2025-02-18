using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TacticalAIMgr : MonoBehaviour
{
    public static TacticalAIMgr inst;
    //public List<Group> groups = new List<Group>();
    
    public Group currentGroup;

    private Dictionary<int, Group> controlGroups = new Dictionary<int, Group>();
    public List<Group> controlGroupsShadow = new List<Group>(); // for debugging

    private void Awake() {
        inst = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        controlGroups.Clear();
    }

    // Update is called once per frame
    void UpdateX() {
        foreach(Group group in controlGroups.Values) {
            group.Tick();
        }
    }

    public bool Exists(int groupNumber) {
        return controlGroups.ContainsKey(groupNumber) && (controlGroups[groupNumber].groupEntities.Count > 0);
    }

    public void CreateBindControlGroup(List<Entity> entities, int groupNumber) {
        if(groupNumber >= 0 && groupNumber < 10) {
            Group group = new Group(entities, groupNumber); //control groups set isControl = true;
            if(controlGroups.ContainsKey(groupNumber))
                controlGroups[groupNumber] = group;
            else
                controlGroups.Add(groupNumber, group);
            currentGroup = group;
            controlGroupsShadow = controlGroups.Values.ToList();
        }
    }

    public void SelectControlGroup(int groupNumber) {
        if(controlGroups.ContainsKey(groupNumber)) {
            foreach(Entity ent in controlGroups[groupNumber].groupEntities) {
                SelectionMgr.inst.SelectEntity(ent, false);
            }
            currentGroup = controlGroups[groupNumber];
            currentGroup.tactics = controlGroups[groupNumber].tactics;
        }
    }


    //----------------------Handling commands----------------------
    public Group tacCommandGroup;
    /// <summary>
    /// 
    /// </summary>
    /// <param name="ents"></param>
    /// <param name="posEnt"></param>
    /// <param name="tt"></param>
    public void HandleTacticalCommand(Group group, WorldPosEntity posEnt, TacticsType tt) {
        Debug.Log("Handling command: " + tt);
        if(currentGroup.isControl) {//if selected ents are from control group
            tacCommandGroup = currentGroup;
        } else {
            tacCommandGroup = group; // else form temporary group
        }

        switch(tt) {
            case TacticsType.EscortMove:
                CreateEscortMove(tacCommandGroup, posEnt.worldPosition);
                break;
            default:
                Debug.Log("Not implemented yet");
                break;
        }

    }

    public void CreateEscortMove(Group group, Vector3 pos) {
        FormationMoveTactic fmt = new FormationMoveTactic(group.groupEntities, pos);
        fmt.Init();
        group.tactics.Add(fmt);
        group.formations.Add(fmt);
        group.Init();
    }



}
