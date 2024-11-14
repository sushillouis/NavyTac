using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinimapAspect : MonoBehaviour
{
    public GameObject iconPrefab;
    public Entity entity;
    // Start is called before the first frame update
    void Start()
    {
        MinimapMgr.inst.CreateMinimapIcon(entity, iconPrefab);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
