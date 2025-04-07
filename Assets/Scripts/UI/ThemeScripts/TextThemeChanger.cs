using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;
public class TextThemeChanger : MonoBehaviour
{

    // The font used for each box
    public TextMeshProUGUI textFont;

    // This is a color used specifically for some panels on the game theme, I wasn't sure what to call it
    // The bool exists so that I can change only some of the text colors instead of all of them if the theme is game
    public bool changeTextColor;
    public Color optionalTextColor;

    //The buttons require different text centering, so each box has a game and theme location
    //I had to put it here and not theme manager so i could adjust each individually if needed
    //It's a bit tedious but I couldn't think of another way to get the positioning different for every one
    public Vector2 gameTextLocation;
    public Vector2 seriousTextLocation;
    

    void Start()
    {
        textFont = GetComponent<TextMeshProUGUI>();
    }

    public void ChangeTheme(Theme theme)
    {
        //Mostly for debugging purposes
        if (textFont == null)
        {
            Debug.LogError("TextThemeChanger is missing a TextMeshProUGUI component.");
            return;
        }
        
        textFont.font = ThemeMgr.inst.PanelThemeColorsList.Find(x => x.theme == theme).font;

        //Checks if both the font color needs to be changed, or if it should be regular white
        if(changeTextColor == true && theme == Theme.Game)
            textFont.color = optionalTextColor;
        else
            textFont.color = Color.white;

        switch (theme)
        {
            case Theme.Game:
                textFont.rectTransform.anchoredPosition = gameTextLocation;
            break;

            case Theme.Serious:
                textFont.rectTransform.anchoredPosition  = seriousTextLocation;
            break;
        }
    }

}
