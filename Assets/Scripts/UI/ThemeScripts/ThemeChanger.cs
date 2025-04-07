using System.Collections;
using System.Collections.Generic;
using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine;

public class ThemeChanger : MonoBehaviour
{

    public Image PanelImage;
    public Color PanelColor;
    

    void Start()
    {
        PanelImage = GetComponent<Image>();
        PanelColor = GetComponent<Color>();
    }

    public void ChangeTheme(Theme theme)
    {
        PanelColor = ThemeMgr.inst.PanelThemeColorsList.Find(x => x.theme == theme).color;
    }
}
