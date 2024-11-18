using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;

[Serializable]
public class MapSceneData
{
    public Sprite mapSprite;
    public MapNames mapName;
    public string mapDescription;
    public int SceneID;
}
public class MapMgr : MonoBehaviour
{
    public static MapMgr inst;
    private void Awake() {
        inst = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        mapSceneData = mapSceneDataList[0];
        //GameMgr.inst.InitMapMenu();
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public List<MapSceneData> mapSceneDataList = new List<MapSceneData>();

    public TextMeshProUGUI MapNameText;
    public TextMeshProUGUI MapDescriptionText;
    public RectTransform MapImagePanel;
    public MapSceneData mapSceneData;
    public void SetMap(MapNames mapName) {
        if(mapSceneDataList.Exists(x => x.mapName == mapName)) {
            mapSceneData = mapSceneDataList.Find(x => x.mapName == mapName);
            MapNameText.text = mapSceneData.mapName.ToString();
            MapImagePanel.GetComponent<Image>().sprite = mapSceneData.mapSprite;
            MapDescriptionText.text = mapSceneData.mapDescription;
        }
    }

    public void LoadMap() {
        SceneManager.LoadSceneAsync(mapSceneData.SceneID);
    }


}


