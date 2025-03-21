using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

    [Serializable]
    public enum ButtonType {
        Decorative,
        Frame,
        Pressable,
        Quit
    }
public class ButtonThemeChanger : MonoBehaviour
{
    public ButtonType buttonType;
    public Image buttonImage;

    // Start is called before the first frame update
    void Start()
    {
        buttonImage = GetComponent<Image>();
    }

    public void ChangeTheme(Theme theme)
    {
        switch (buttonType)
        {
            case ButtonType.Decorative:
                buttonImage.sprite = ThemeMgr.inst.PanelThemeColorsList.Find(x => x.theme == theme).decorativeButton;
                break;
            case ButtonType.Frame:
                buttonImage.sprite = ThemeMgr.inst.PanelThemeColorsList.Find(x => x.theme == theme).buttonFrame;
                break;
            case ButtonType.Pressable:
                buttonImage.sprite = ThemeMgr.inst.PanelThemeColorsList.Find(x => x.theme == theme).pressableButton;
                break;
            case ButtonType.Quit:
                buttonImage.sprite = ThemeMgr.inst.PanelThemeColorsList.Find(x => x.theme == theme).quitButton;
                break;
        }
    }
}
