using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro;
using Unity.Networking.Transport;
using System.Net.NetworkInformation;
using System.Linq;


public class OpenOceanMain : MonoBehaviour
{
    public static OpenOceanMain inst;

    public string playerName = "Debugger";
    public bool IsDebugging = false;
    public string ipAddress = "127.0.0.1";

    [SerializeField]
    private PanelPlus loginPanel;
    [SerializeField]
    private PanelPlus mapSelectPanel;
    [SerializeField]
    private PanelPlus HostOrJoinPanel;
    [SerializeField]
    private PanelPlus MainGamePanel;
    [SerializeField]
    private RectTransform NetDebugConsolePanel;


    public TMP_InputField loginNameInputField;

    public TMP_InputField ipAddressInputField;
    [SerializeField]
    private Button hostButton;
    [SerializeField]
    private Button clientButton;
    [SerializeField]
    private Button loginButton;

    [SerializeField]
    private Button HostJoinQuitButton;
    [SerializeField]
    private Button LoginQuitButton;


    public enum LobbyState
    {
        None = 0,
        Login,
        MapSelect,
        HostOrJoin,
        Play,
        Done,
    }
    [SerializeField]
    private LobbyState _lobbyState = LobbyState.None;

    [SerializeField]
    private GameObject NetworkManagerGo;

    private void Awake() {
        inst = this;
        hostButton.onClick.RemoveAllListeners();
        hostButton.onClick.AddListener(() =>
        {
            SetupIPAddressAndPort();

            NetworkManager.Singleton.StartHost();

            lobbyState = LobbyState.Login;
        });
        clientButton.onClick.RemoveAllListeners();
        clientButton.onClick.AddListener(() =>
        {
            SetupIPAddressAndPort();
            NetworkManager.Singleton.StartClient();
            lobbyState = LobbyState.Login;
        });

        HostJoinQuitButton.onClick.RemoveAllListeners();
        HostJoinQuitButton.onClick.AddListener(OnQuitButton);

        LoginQuitButton.onClick.RemoveAllListeners();
        LoginQuitButton.onClick.AddListener(OnQuitButton);

    }

    void SetupIPAddressAndPort() {
        string tmp = ipAddressInputField.text.Trim();
        int count = tmp.Count(x => x == '.');
        if(count == 3)
            ipAddress = tmp;
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(ipAddress, 7777);
    }

    [Header("To be filled on Login Button Press/Network setup")]
    public NetSetup localNetSetup;
    public NetworkObject localNetSetupNetworkObject;
    public TactNetMgr localTactNetMgr;
    public NetworkObject localTactNetMgrNetworkObject;

    private void Start() {
        lobbyState = LobbyState.HostOrJoin;
        
        loginButton.onClick.RemoveAllListeners();
        loginButton.onClick.AddListener(() =>
        {
            playerName = loginNameInputField.text.Trim();
            lobbyState = LobbyState.Play;
            foreach(NetSetup ns in FindObjectsOfType<NetSetup>()) {
                NetworkObject tmp = ns.GetComponent<NetworkObject>();
                if(tmp.IsLocalPlayer) {
                    localNetSetupNetworkObject = tmp;
                    localNetSetup = ns;
                }
            }
            foreach(TactNetMgr tnm in FindObjectsOfType<TactNetMgr>()) {
                NetworkObject tmp = tnm.GetComponent<NetworkObject>();
                if(tmp.IsLocalPlayer) {
                    localTactNetMgrNetworkObject = tmp;
                    localTactNetMgr = tnm;
                }
            }
            localNetSetup.OnLoginButton();
        });
    }


    public LobbyState lobbyState
    {
        get {
            return _lobbyState;
        }
        set {
            _lobbyState = value;

            loginPanel.isVisible = (value == LobbyState.Login);
            mapSelectPanel.isVisible = (value == LobbyState.MapSelect);
            HostOrJoinPanel.isVisible = (value == LobbyState.HostOrJoin);
            MainGamePanel.isVisible = (value == LobbyState.Play);
            NetDebugConsolePanel.gameObject.SetActive(IsDebugging);
        }
    }

    public void OnMapSelected() {
        lobbyState = LobbyState.None;
    }

    public void OnQuitButton() {
        Debug.Log("Shutting down TactNetMgr and quitting");
        TactNetMgr.inst.TactNetShutdown();
        NetworkManager.Singleton.Shutdown();
        if(Application.isEditor) {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        } else {
            Application.Quit();
        }

    }

}
