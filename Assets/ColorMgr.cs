using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class ColorMgr : MonoBehaviour
{
    [ContextMenu("Update Color")]
    void UpdateColor()
    {
        colorPicker();
    }

    public RawImage[] mainPanelOptions;
    public RawImage[] mapPanelOptions;
    public int chosenPanel;
    public Color chosenColor;

    public void colorPicker()
    {
        if (chosenPanel == 0){
            foreach(RawImage panel in mainPanelOptions)
            {
                panel.color = chosenColor;
            }
        }
        else{
            foreach(RawImage panel in mapPanelOptions)
            {
                panel.color = chosenColor;
            }
        }
    }
}
