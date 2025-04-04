using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class FrameThemeChanger : MonoBehaviour
{
    public enum FrameType {
        Whole,
        Corner,
    }
    public FrameType frameType;
    public Image PanelImage;

    // Start is called before the first frame update
    void Start()
    {
        PanelImage = GetComponent<Image>();        
    }

    public void ChangeTheme(Theme theme)
    {
        switch (frameType)
        {
            case FrameType.Whole:
                PanelImage.sprite = ThemeMgr.inst.PanelThemeColorsList.Find(x => x.theme == theme).frame;
            break;
            case FrameType.Corner:
                Color c = PanelImage.color;
                if(theme == Theme.Game)
                {
                    c.a = 1;
                    PanelImage.color = c;
                    PanelImage.sprite = ThemeMgr.inst.PanelThemeColorsList.Find(x => x.theme == theme).cornerFrame;
                }
                else
                {
                    c.a = 0;
                    PanelImage.color = c;
                }
            break;
        }
    }
}
