using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;


public class OpenOceanMain : MonoBehaviour
{
    public static OpenOceanMain inst;

    public string playerName = "Debugger";
    public bool IsDebugging = false;

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

    private void Awake() {
        inst = this;
        hostButton.onClick.RemoveAllListeners();
        hostButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartHost();

            lobbyState = LobbyState.Login;
        });
        clientButton.onClick.RemoveAllListeners();
        clientButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartClient();
            lobbyState = LobbyState.Login;
        });

        HostJoinQuitButton.onClick.RemoveAllListeners();
        HostJoinQuitButton.onClick.AddListener(OnQuitButton);

        LoginQuitButton.onClick.RemoveAllListeners();
        LoginQuitButton.onClick.AddListener(OnQuitButton);

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
