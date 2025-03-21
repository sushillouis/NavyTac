using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class FrameThemeChanger : MonoBehaviour
{
    public Image PanelImage;
    public Color PanelColor;
    // Start is called before the first frame update
    void Start()
    {
        PanelImage = GetComponent<Image>();
        PanelColor = GetComponent<Color>();
        
    }

    public void ChangeTheme(Theme theme)
    {
        PanelImage.sprite = ThemeMgr.inst.PanelThemeColorsList.Find(x => x.theme == theme).frame;
        PanelColor = ThemeMgr.inst.PanelThemeColorsList.Find(x => x.theme == theme).color;
    }
}
