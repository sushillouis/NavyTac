using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;
public class TextThemeChanger : MonoBehaviour
{

    public TextMeshProUGUI textFont;
    public bool changeTextColor;
    public Color optionalTextColor;
    // Start is called before the first frame update
    void Start()
    {
        textFont = GetComponent<TextMeshProUGUI>();
    }

    public void ChangeTheme(Theme theme)
    {
        if (textFont == null)
        {
            Debug.LogError("TextThemeChanger is missing a TextMeshProUGUI component.");
            return;
        }
        
        textFont.font = ThemeMgr.inst.PanelThemeColorsList.Find(x => x.theme == theme).font;
        if(changeTextColor == true && theme == Theme.Game)
            textFont.color = optionalTextColor;
        else
            textFont.color = Color.white;
    }

}
