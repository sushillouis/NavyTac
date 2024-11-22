using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapMenuMain : MonoBehaviour
{
    public static bool isHost;
    public static string playerName = "Debugger";


    [SerializeField]
    private PanelPlus loginPanel;
    [SerializeField]
    private PanelPlus mapSelectPanel;
    [SerializeField]
    private PanelPlus HostOrJoinPanel;

    [SerializeField]
    private TMP_InputField loginNameInputField;
    [SerializeField]
    private Button hostButton;
    [SerializeField]
    private Button joinButton;

    public enum LobbyState
    {
        None = 0,
        Login,
        MapSelect,
        HostOrJoin,
        Done,
    }

    private void Start() {
        lobbyState = LobbyState.Login;
        hostButton.onClick.RemoveAllListeners();
        hostButton.onClick.AddListener(() => {
            isHost = true;
            OnHostOrJoinSubmit();
        });
        joinButton.onClick.RemoveAllListeners();
        joinButton.onClick.AddListener(() => {
            isHost = false;
            OnHostOrJoinSubmit();
        });
    }

    //[SerializeField]
    //private LobbyState lstate = LobbyState.None;
    [SerializeField]
    private LobbyState _lobbyState = LobbyState.None;
    private LobbyState lobbyState
    {
        get {
            return _lobbyState;
        }
        set {
            _lobbyState = value;

            loginPanel.isVisible = (value == LobbyState.Login);
            mapSelectPanel.isVisible = (value == LobbyState.MapSelect);
            HostOrJoinPanel.isVisible = (value == LobbyState.HostOrJoin);
        }
    }

    public void OnLogin() {
        playerName = loginNameInputField.text.Trim();
        lobbyState = LobbyState.MapSelect;
    }

    public MapNames selectedMapName;
    public void OnMapSelected() {
        lobbyState = LobbyState.HostOrJoin;
    }

    public void OnHostOrJoinSubmit() {
        lobbyState = LobbyState.None;
        MapMgr.inst.LoadMap();
    }
}
