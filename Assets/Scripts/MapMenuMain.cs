using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class MapMenuMain : MonoBehaviour
{
    public static bool isHost = true;  // Always host by default
    public static string playerName = "Debugger";

    [SerializeField] private PanelPlus loginPanel;
    [SerializeField] private PanelPlus mapSelectPanel;

    [SerializeField] private TMP_InputField loginNameInputField;

    public MapNames selectedMapName;

    public enum LobbyState
    {
        None = 0,
        MapSelect,
        Login,
        Done,
    }

    [SerializeField]
    private LobbyState _lobbyState = LobbyState.None;
    private LobbyState lobbyState
    {
        get { return _lobbyState; }
        set
        {
            _lobbyState = value;

            mapSelectPanel.isVisible = (value == LobbyState.MapSelect);
            loginPanel.isVisible = (value == LobbyState.Login);
        }
    }

    private void Start()
    {
        // Show map select first
        lobbyState = LobbyState.MapSelect;
    }

    // Called when a map is selected
    public void OnMapSelected(MapNames chosenMap)
    {
        selectedMapName = chosenMap;
        // MapMgr.inst.LoadMap();  // Load selected map
        lobbyState = LobbyState.Login;
    }

    // Called when player enters their name
    public void OnLogin()
    {
        playerName = loginNameInputField.text.Trim();

        // Start as Host
        NetworkManager.Singleton.StartHost();

        lobbyState = LobbyState.Done;
    }
}
