using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TacticalAIMgr : MonoBehaviour
{
    public static TacticalAIMgr inst;
    public List<Group> groups = new List<Group>();
    public Group currentGroup;

    private Dictionary<int, Group> controlGroups = new Dictionary<int, Group>();
    private void Awake() {
        inst = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        groups.Clear();
        controlGroups.Clear();
    }

    // Update is called once per frame
    void UpdateX() {
        if(Input.GetKeyUp(KeyCode.U)) {
            Debug.Log("Creating group");

            Group group = CreateGroup(SelectionMgr.inst.selectedEntities);
            group.Init();
            currentGroup = group;
            groups.Add(group);
        }

        RemoveDoneGroups();
        foreach(Group gai in groups) {
            gai.Tick();
        }
    }

    public bool Exists(int groupNumber) {
        return controlGroups.ContainsKey(groupNumber) && (controlGroups[groupNumber].groupEntities.Count > 0);
    }

    public void CreateBindControlGroup(List<Entity> entities, int groupNumber) {
        if(groupNumber >= 0 && groupNumber < 10) {
            Group group = CreateGroup(entities, groupNumber);
            if(controlGroups.ContainsKey(groupNumber))
                controlGroups[groupNumber] = group;
            else
                controlGroups.Add(groupNumber, group);
        }
    }

    public void SelectControlGroup(int groupNumber) {
        if(controlGroups.ContainsKey(groupNumber)) {
            foreach(Entity ent in controlGroups[groupNumber].groupEntities) {
                SelectionMgr.inst.SelectEntity(ent, false);
            }
        }
    }

    public Group CreateGroup(List<Entity> entities, int groupNumber = -1) {
        Group newGroup = new Group(entities, groupNumber);
        groups.Add(newGroup);
        return newGroup;
    }

    public void AddEntityToGroup(Entity entity, Group group) {
        RemoveEntityFromOtherGroups(entity, group);
    }

    public void RemoveEntityFromOtherGroups(Entity entity, Group groupToJoin) {
        foreach(Group group in groups) {
            if(groupToJoin != null){
                if(group != groupToJoin) {
                    if(group.isEntityInGroup(entity)) {
                        group.RemoveEntity(entity);
                    }
                }
            }
        }
    }

    public void RemoveDoneGroups() {
        groups.RemoveAll(x => x.isDone);
    }


}
