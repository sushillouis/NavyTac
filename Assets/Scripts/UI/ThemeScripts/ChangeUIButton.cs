using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeUIButton : MonoBehaviour
{
    public ThemeMgr themeMgr;

    // Set the theme to serious at the start
    void Start()
    {
        themeMgr.choosenTheme = Theme.Serious;
        themeMgr.ChangeAllThemes();
    }

    // Update is called once per frame
    public void pressedUIButton()
    {
        if (themeMgr.choosenTheme == Theme.Serious)
        {
            themeMgr.choosenTheme = Theme.Game;
            themeMgr.ChangeAllThemes();
        }
        else{
            themeMgr.choosenTheme = Theme.Serious;
            themeMgr.ChangeAllThemes();
        }
    }
}
