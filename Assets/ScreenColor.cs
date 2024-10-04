using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScreenColor : MonoBehaviour
{
  [ContextMenu("Update Color")]
    void UpdateColor()
    {
        colorPicker();
    }

    GameObject[] screenOptions;
    
    public Color chosenColor;

     public void colorPicker()
    {
        screenOptions = GameObject.FindGameObjectsWithTag("Screen");
        foreach(GameObject screen in screenOptions)
        {
            RawImage panel = screen.GetComponent<RawImage>();
            panel.color = chosenColor;
        }
    }
}
