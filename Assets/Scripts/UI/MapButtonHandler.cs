using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapButtonHandler : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Button button = transform.GetComponent<Button>();
        button.onClick.AddListener(HandlePress);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public MapNames mapName = MapNames.OpenOcean;

    public void HandlePress() {
        // MapMgr.inst.SetMap(mapName);
    }

}
