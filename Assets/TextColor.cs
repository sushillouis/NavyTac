using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TextColor : MonoBehaviour
{
  [ContextMenu("Update Color")]
    void UpdateColor()
    {
        colorPicker();
    }

    GameObject[] textOptions;
    
    public Color chosenColor;

     public void colorPicker()
    {
        textOptions = GameObject.FindGameObjectsWithTag("Text");
        foreach(GameObject text in textOptions)
        {
            TextMeshProUGUI content = text.GetComponent<TextMeshProUGUI>();
            content.color = chosenColor;
        }
    }
}