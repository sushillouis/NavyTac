using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NetDebugConsole : MonoBehaviour
{
    public static NetDebugConsole inst;
    private void Awake() {
        inst = this;
        GetBars(root);
    }

    public TextMeshProUGUI console;

    public void Log(string msg) {
        console.text += msg + "\n> ";
        //Debug.Log(msg);
    }

    // Start is called before the first frame update
    void Start() {
        console.text = ">";
    }

    // Update is called once per frame
    void Update() {
        int eid = 0;
        Entity entity;
        foreach(NetDiffVis ndv in diffVisList) {
            entity = EntityMgr.inst.entities.Find(x => x.entityId == eid);
            if(entity != null) {
                ndv.SetPosRot(entity.net.posDiff, entity.net.rotDiff);
            }
            eid += 1;
        }
    }

    [SerializeField]
    private RectTransform root;
    [SerializeField]
    private List<NetDiffVis> diffVisList;
    public void GetBars(RectTransform root) {
        diffVisList = new List<NetDiffVis>();
        foreach(NetDiffVis ndv in root.GetComponentsInChildren<NetDiffVis>()) {
            diffVisList.Add(ndv);
        }
    }

}
