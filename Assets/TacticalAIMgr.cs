using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TacticalAIMgr : MonoBehaviour
{
    public static TacticalAIMgr inst;
    private void Awake()
    {
        inst = this;
    }
    [SerializeReference] List<Group> groups = new();
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void RemoveGroup(Group group) {
        if (groups.Contains(group)) {
            groups.Remove(group);
        }
    }

    public Group AssembleGroup(List<Entity> entities) {
        Group targetGroup = null;
        foreach (Entity ent in entities) {
            foreach (Group group in groups) {
                if (group.target == ent) {
                    targetGroup = group;
                    break;
                }
            }
        }
        List<UnitAI> aIs = new();

        foreach (Entity ent in SelectionMgr.inst.selectedEntities) {
            UnitAI uai = ent.GetComponentInChildren<UnitAI>();
            aIs.Add(uai);
        }

        if(aIs.Count==0)
            return null;

        if(targetGroup==null) {
            targetGroup = new(aIs);
            groups.Add(targetGroup);
        } else {
            targetGroup.AddMembers(aIs.ToArray());
        }
        return targetGroup;
    }
    
}
