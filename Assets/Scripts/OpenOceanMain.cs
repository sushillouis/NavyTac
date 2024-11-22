using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;


public class OpenOceanMain : MonoBehaviour
{
    public static OpenOceanMain inst;


    [SerializeField]
    private RectTransform netUIPanel;
    [SerializeField]
    private Button hostButton;
    [SerializeField]
    private Button clientButton;
    [SerializeField]
    private Button serverButton;


    private void Awake() {
        inst = this;

        Debug.Log("Player: " + MapMenuMain.playerName);
        hostButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartHost();
            ShowNetGui(false);
        });

        clientButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartClient();
            ShowNetGui(false);
        });

        serverButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartServer();
            ShowNetGui(false);
        });


    }
    // Start is called before the first frame update
    void Start()
    {
        NetDebugConsole.inst.Log("Player name: " + MapMenuMain.playerName);
        if(MapMenuMain.playerName.Contains("Debugger")) {
            ShowNetGui(true);
        } else {
            ShowNetGui(false);
            SetupNet(MapMenuMain.isHost);
        }
        GameMgr.inst.NetTest();
        //GameMgr.inst.InitOpenOceanMap();
        //GameMgr.inst.InitTestWidgetMap();



    }



    public void ShowNetGui(bool shouldShow) {
        netUIPanel.gameObject.SetActive(shouldShow);
    }
    void SetupNet(bool isHost) {
        if(isHost) {
            NetworkManager.Singleton.StartHost();
            Debug.Log("setting up network as Host");
        } else {
            NetworkManager.Singleton.StartClient();
            Debug.Log("setting up network as Client");
        }
    }


}
