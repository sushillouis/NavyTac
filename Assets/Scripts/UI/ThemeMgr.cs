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
    public TMP_FontAsset font;
    public Color color;
    public Sprite frame;
    public Sprite decorativeButton;
    public Sprite buttonFrame;
    public Sprite pressableButton;
    public Sprite quitButton;
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

    public Color Theme1PanelColor;
    public Color Theme2PanelColor;

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
