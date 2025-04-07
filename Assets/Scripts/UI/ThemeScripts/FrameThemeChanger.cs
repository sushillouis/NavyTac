using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class FrameThemeChanger : MonoBehaviour
{

    // Whole refers to the rectangle frames and corner is the decorative frames I use in the sound panel
    public enum FrameType {
        Whole,
        Corner,
    }
    public FrameType frameType;
    public Image PanelImage;

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

            // For the case of the serious UI, it doesn't use corner frames as of now, so I set them to transparent when the theme is serious
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
