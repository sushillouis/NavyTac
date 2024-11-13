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
        foreach (Group group in groups)
        {
            group.Tick();
        }
    }

    public void RemoveGroup(Group group) {
        if (groups.Contains(group)) {
            groups.Remove(group);
        }
    }

    public Group AssembleGroup(List<Entity> entities) {
        Group targetGroup = null;
        List<UnitAI> aIs = new();

        foreach (Entity ent in entities) {
            UnitAI uai = ent.ai;
            aIs.Add(uai);
        }
        if(aIs.Count==0)
            return null;

        for (int i = 0 ; i < aIs.Count; i++) {
            if(aIs[i].group != null && aIs[i].group.target == entities[i]) {
                targetGroup = aIs[i].group;
                break;
            }
        }

        if(targetGroup==null) {
            targetGroup = new(aIs);
            groups.Add(targetGroup);
        } else {
            targetGroup.AddMembers(aIs.ToArray());
        }
        return targetGroup;
    }
    
}
