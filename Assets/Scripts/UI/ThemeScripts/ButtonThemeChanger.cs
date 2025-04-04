using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

[Serializable]
    public enum ButtonType {
        Decorative,
        Frame,
        Pressable,
        Quit,
        Sound
    }
public class ButtonThemeChanger : MonoBehaviour
{
    public ButtonType buttonType;
    public Image buttonImage;
    public bool shouldBeTransparent;

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
                Color c = buttonImage.color;
                if (shouldBeTransparent == true && theme == Theme.Game)
                {
                    c.a = 105f / 255f;
                    c.r = 96f /225f;
                    c.g = 96f /225f;
                    c.b = 96f /225f;
                    buttonImage.color = c;
                }
                else
                {
                    c.a = 1;
                    c.r = 1;
                    c.g = 1;
                    c.b = 1;
                    buttonImage.color = c;
                }
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
            case ButtonType.Sound:
                buttonImage.sprite = ThemeMgr.inst.PanelThemeColorsList.Find(x => x.theme == theme).soundButton;
                GameObject child = this.gameObject.transform.GetChild(0).gameObject;
                Image audioImage = child.GetComponent<Image>();
                audioImage.color = ThemeMgr.inst.PanelThemeColorsList.Find(x => x.theme == theme).audioColor;
                break;
        }
    }
}
