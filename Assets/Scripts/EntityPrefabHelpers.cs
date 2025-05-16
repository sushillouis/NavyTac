using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityPrefabHelpers : MonoBehaviour
{

    public GameObject emptyGO;
    [ContextMenu("AddPFObjectsToEntity")]
    public void AddPFObjectsToEntity() {
        foreach(Entity ent in GetComponentsInChildren<Entity>()) {
            //if(!ent.name.Contains("DDG51"))  return;
            float entLength = ent.length;
            Vector3 start = new Vector3(0, 0, ent.length / 2);
            Vector3 stride = new Vector3(0, 0, -ent.length / 3);
            UnitAI uai = ent.gameObject.GetComponentInChildren<UnitAI>();
            if(uai != null) {
                for(int i = 1; i < 3; i++) {
                    //Debug.Log(ent.name + ": uai: " + uai.name + i);
                    GameObject go = new GameObject("PF" + i);
                    go.transform.parent = uai.transform;
                    go.transform.localPosition = start + stride * i;
                }
            }
        }
    }

    [ContextMenu("SetupPFObjects")]
    public void SetupPFObjects() {
        foreach(Entity ent in GetComponentsInChildren<Entity>()) {
            UnitAI uai = ent.gameObject.GetComponentInChildren<UnitAI>();
            if(uai != null) {
                uai.pfList.Clear();
                foreach(Transform t in uai.GetComponentsInChildren<Transform>()) {
                    if(t.gameObject.name.Contains("PF")) {
                        uai.pfList.Add(t);
                        //Debug.Log("Added: " + ent.name + ": " + t.gameObject.name);
                    }
                }
            }
        }

    }

    [ContextMenu("MovePFObjects")]
    public void MovePFPbjects() {
        foreach(Entity ent in GetComponentsInChildren<Entity>()) {
            UnitAI uai = ent.gameObject.GetComponentInChildren<UnitAI>();
            float entLength = ent.length;
            Vector3 start = new Vector3(0, 0, ent.length / 2);
            Vector3 stride = new Vector3(0, 0, -ent.length/3);
            //Debug.Log("ent: " + ent.name + ", start: " + start + ", " + stride);
            uai.pfList[0].localPosition = start + stride;
            uai.pfList[1].localPosition = start + stride * 2;
        }
    }
}
