using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChangeUIPanelTheme : MonoBehaviour
{
    public bool basic = false;

    [Header("Images")]
    public Image frameImageBasic;
    public Image buttonBasicBackground;
    public Image solidButtonMetalBackground;
    public Image InputMetalBackground;
    public Image strechedButtonMetalBackground;
    public Image frameImageMetal;

    [Header("Fonts")]
    public TMP_Asset basicFont;
    public TMP_Asset metalFont;
    
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void ChangeTheme()
    {
        if(basic == false)
        {

        }

        if(basic == true)
        {

        }
    }
}
