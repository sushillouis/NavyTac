using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistanceIconsMain : MonoBehaviour
{
    public List<GameObject> entityPrefabs;
    // Start is called before the first frame update
    void Start()
    {
        GameMgr.inst.InitDistanceIconsTesting(entityPrefabs);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
