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

    [ContextMenu("TestInfo")]
    void TestInfo()
    {
        SwitchPanelsInfo();
    }

    [ContextMenu("TestDisplay")]
    void TestDisplay()
    {
        SwitchPanelsDisplay();
    }



    // Start is called before the first frame update
    void Start()
    {
        // mapImage;
        mapName.text = "Four Corners";
        // mapDisplayPanel.isVisible = true;
        // mapInfoPanel.isVisible = false;
    }

    //Switch the Info Panel In
    public void SwitchPanelsInfo()
    {
        if(mapInfoPanel.isVisible == false)
        {
            mapDisplayPanel.isVisible = false;
            mapInfoPanel.isVisible = true;
        }
    }


    //Switch the Display panel in
    public void SwitchPanelsDisplay()
    {
        if (mapDisplayPanel.isVisible == false)
        {
            mapDisplayPanel.isVisible = true;
            mapInfoPanel.isVisible = false;
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
