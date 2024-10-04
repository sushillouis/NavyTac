using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapPanelHandler : MonoBehaviour
{
    public RawImage mapImage;
    public TextMeshProUGUI mapName;
    public PanelMover mapInfoPanel;
    public PanelMover mapDisplayPanel;


    // Start is called before the first frame update
    void Start()
    {
        // mapImage;
        mapName.text = "Four Corners";
        mapDisplayPanel.isVisible = true;
    }

    public void SwitchPanels()
    {
        if(mapDisplayPanel.isVisible == false){
            mapDisplayPanel.isVisible = true;
            mapInfoPanel.isVisible = false;
        }
        else{
            mapDisplayPanel.isVisible = false;
            mapInfoPanel.isVisible = true;
        }

    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
