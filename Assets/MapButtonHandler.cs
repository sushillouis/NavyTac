using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;


public class MapButtonHandler : MonoBehaviour
{
    public TextMeshProUGUI currentMapName;
    public RawImage currentMapImage;
    MapPanelHandler panelHandler;
    

    public void pressedButton()
    {
        panelHandler.mapImage = currentMapImage;
        panelHandler.mapName.text = currentMapName.text;
    }
    
    // Start is called before the first frame update
    void Start()
    {
        panelHandler = GameObject.FindObjectOfType<MapPanelHandler>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
