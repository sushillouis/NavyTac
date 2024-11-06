using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapButtonHandler : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public MapNames mapName = MapNames.OpenOcean;

    public void HandlePress() {
        MapMgr.inst.SetMap(mapName);
    }

}
