using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;


public class MapButtonHandler : MonoBehaviour
{
    /*
    public TextMeshProUGUI currentMapName;
    public Image currentMapImage;
    public TextMeshProUGUI currentMapType;
    public Button currentButton;
    public int sceneIndex;
    MapPanelHandler panelHandler;
    ButtonStaysPressed buttonMgr;
    SceneSwitch sceneSwitch;


    //When you press the button set everything in the info box to the selected map
    public void pressedButton()
    {
        panelHandler.mapImage.overrideSprite = currentMapImage.overrideSprite;
        panelHandler.mapName.text = currentMapName.text;
        panelHandler.mapDescription.text = currentMapType.text;
        sceneSwitch.currentScene = sceneIndex;
        buttonMgr.OnButtonClicked(currentButton);
        

    }
    */
    public MapNames mapName = MapNames.OpenOcean;

    // Start is called before the first frame update
    //Using Start to grab the other scripts at runtime
    void Start() {
    }

    public void ButtonPressed() {
        MapMgr.inst.SetMap(mapName);
    }
        


    // Update is called once per frame
    void Update()
    {
        
    }
}


/*
panelHandler = GameObject.FindObjectOfType<MapPanelHandler>();
buttonMgr = GameObject.FindObjectOfType<ButtonStaysPressed>();
sceneSwitch = GameObject.FindObjectOfType<SceneSwitch>();
*/
