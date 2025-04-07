using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;


//This enum may grow as I add more button types
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

    //Some buttons are slightly transparent in specifically the game mode so this covers that specific case
    public bool shouldBeTransparent;

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

                //This covers the transparency issue with some game UI button, it's a bit much but I do need the specifics I think for certain buttons
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
                //This changes the color of the audio image on the button
                GameObject child = this.gameObject.transform.GetChild(0).gameObject;
                Image audioImage = child.GetComponent<Image>();
                audioImage.color = ThemeMgr.inst.PanelThemeColorsList.Find(x => x.theme == theme).audioColor;
                break;
        }
    }
}
