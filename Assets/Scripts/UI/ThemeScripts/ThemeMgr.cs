using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;


[Serializable]
public enum Theme {
    Game, Serious
}

[Serializable]
public class ThemeColors {
    public Theme theme;

    //These are the different types of text and colors that are used in textthemechanger
    public TMP_FontAsset font;
    public Color color;
    public Color audioColor;

    //These are the different types of frames that correspond to the enums in framethemechanger
    public Sprite frame;
    public Sprite cornerFrame;

    //These are the different types of buttons that correspond to the enums in buttonthemechanger
    public Sprite decorativeButton;
    public Sprite buttonFrame;
    public Sprite pressableButton;
    public Sprite quitButton;
    public Sprite soundButton;

}


[System.Serializable]
public class ThemeMgr : MonoBehaviour
{
    public static ThemeMgr inst;
    private void Awake()
    {
        inst = this;
    }
    public Theme choosenTheme;
    
    [SerializeField] public List<ThemeColors> PanelThemeColorsList;

    // These aren't really in use right now because both use the same background, but I kept it in in case it changes
    public Color Theme1PanelColor;
    public Color Theme2PanelColor;

    // Testing at runtime from the inspector panel
    [ContextMenu("Change Theme")]
    void changeThemeInspector()
    {
        ChangeAllThemes();
    }

    public void ChangeAllThemes()
    {
        TextThemeChanger[] textList = FindObjectsOfType<TextThemeChanger>();
        ThemeChanger[] panelList = FindObjectsOfType<ThemeChanger>();
        FrameThemeChanger[] frameList = FindObjectsOfType<FrameThemeChanger>();
        ButtonThemeChanger[] buttonList = FindObjectsOfType<ButtonThemeChanger>();

        //Cycles through each of the script types
        foreach (TextThemeChanger theme in textList)
        {
            theme.ChangeTheme(choosenTheme);
        }
        foreach (ThemeChanger theme in panelList)
        {
            theme.ChangeTheme(choosenTheme);
        }
        foreach (FrameThemeChanger theme in frameList)
        {
            theme.ChangeTheme(choosenTheme);
        }
        foreach (ButtonThemeChanger theme in buttonList)
        {
            theme.ChangeTheme(choosenTheme);
        }

    }
}
