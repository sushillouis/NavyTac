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
using System.Text.RegularExpressions;


public class OpenOceanMain : MonoBehaviour
{
    public static OpenOceanMain inst;

    public string playerName = "PFTest";
    public int playerNo = 0;
    public string playerCode = "LoginCode";
    public string ipAddress = "127.0.0.1";
    public bool IsDebugging = false;
    public bool IsNetDebugging = false;
    public bool isSinglePlayer = true;

    [SerializeField]
    private ushort port = 7777;


    [Header("Panels")]
    [SerializeField]
    private PanelPlus loginPanel;
    [SerializeField]
    private PanelPlus mapSelectPanel;
    [SerializeField]
    private PanelPlus HostOrJoinPanel;
    [SerializeField]
    private PanelPlus MainGamePanel;
    [SerializeField]
    private PanelPlus SingleMultiplayerPanel;
    [SerializeField]
    private PanelPlus ScorePanel;
    [SerializeField]
    private RectTransform NetDebugConsolePanel;

    [Header("Single / Multi player Screen")]
    [SerializeField]
    private Button SinglePlayerButton;
    [SerializeField]
    private Button MultiPlayerButton;
    [SerializeField]
    private Button SingleMultiQuitButton;

    [Header("Host / Join Screen")]
    public TMP_InputField ipAddressInputField;
    [SerializeField]
    private Button hostButton;
    [SerializeField]
    private Button clientButton;
    [SerializeField]
    private Button HostJoinQuitButton;

    [Header("Login Screen")]
    public TMP_InputField loginNameInputField;
    [SerializeField]
    public TMP_InputField loginCodeInputField;
    [SerializeField]
    private Button loginButton;
    [SerializeField]
    private Button LoginQuitButton;

    [Header("Map Select Screen")]
    [SerializeField]
    private Button startButton;
    [Header("Score Panel")]

    [SerializeField] public TMP_Text damageDealtText;
    [SerializeField] public TMP_Text damageTakenText;
    [SerializeField] public TMP_Text winnerText;
    [SerializeField] public TMP_Text scoreText;
    [SerializeField] public TMP_Text ourUnitsDestroyedText;
    [SerializeField] public TMP_Text enemyUnitsDestroyedText;
    [SerializeField] public Button nextGameButton;
    [SerializeField] public TMP_Text winConditionText;

    private int gamesPlayedCount = 0;
    private const string NEXT_GAME_BUTTON_TEXT = "Next Game";
    private const string EXIT_BUTTON_TEXT = "Exit";

    public enum LobbyState
    {
        None = 0,
        SingleMultiPlayer,
        Login,
        MapSelect,
        HostOrJoin,
        Play,
        Done,
        ScorePanel
    }
    [Header("Lobby State and the rest")]

    [SerializeField]
    private LobbyState _lobbyState = LobbyState.None;
        public enum TrainingState
    {
        None,
        PreTest,
        PostTest,
        Adaptive,
        NonAdaptive
    }
    public TrainingState currentTrainingState = TrainingState.None;
    [SerializeField]
    private GameObject NetworkManagerGo;

    private void Awake()
    {
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

        //Single/Multi player screen setup

        SinglePlayerButton.onClick.RemoveAllListeners();
        SinglePlayerButton.onClick.AddListener(OnSinglePlayer);

        MultiPlayerButton.onClick.RemoveAllListeners();
        MultiPlayerButton.onClick.AddListener(OnMultiPlayer);


        SingleMultiQuitButton.onClick.RemoveAllListeners();
        SingleMultiQuitButton.onClick.AddListener(OnQuitButton);

        // Map selected, start button

        startButton.onClick.RemoveAllListeners();
        startButton.onClick.AddListener(OnMapSelected);
        // Map selected, start button
        // Score Panel next game button
        nextGameButton.onClick.RemoveAllListeners();
        nextGameButton.onClick.AddListener(OnNextGameOrExitClicked);

    }

    void SetupIPAddressAndPort()
    {
        string tmp = ipAddressInputField.text.Trim();
        int count = tmp.Count(x => x == '.');
        if (count == 3)
            ipAddress = tmp;
        //else use the default ip address of 127.0.0.1 initialized above
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(ipAddress, port);
    }

    [Header("To be filled on Login Button Press/Network setup")]
    public NetSetup localNetSetup;
    public NetworkObject localNetSetupNetworkObject;
    public TactNetMgr localTactNetMgr;
    public NetworkObject localTactNetMgrNetworkObject;

    private void Start()
    {
        if (IsDebugging)
        {
            Time.timeScale = 0f;
            lobbyState = LobbyState.Login;
            loginButton.onClick.RemoveAllListeners();
            loginButton.onClick.AddListener(() =>
            {
                Time.timeScale = 0f;
                Debug.Log("Login button pressed");
                playerName = loginNameInputField.text.Trim();
                playerCode = loginCodeInputField.text.Trim();
                Debug.Log($"Player Name: {playerName}");
                Regex nameRegex = new Regex(@"^Student\s*\d+$");
                if (!nameRegex.IsMatch(playerName))
                {
                    Debug.Log("Invalid name format");
                    return;
                }
                if (playerCode == "AAA")
                {
                    Debug.Log("Adaptive code");
                    currentTrainingState = TrainingState.Adaptive;
                }
                else if (playerCode == "BBB")
                {
                    Debug.Log("Non-Adaptive code");
                    currentTrainingState = TrainingState.NonAdaptive;
                }
                else if (playerCode == "ABC")
                {
                    Debug.Log("Pre-test code");
                    currentTrainingState = TrainingState.PreTest;
                }
                else if (playerCode == "XYZ")
                {
                    Debug.Log("Post-test code");
                    currentTrainingState = TrainingState.PostTest;
                }
                else
                {
                    Debug.Log("Invalid code");
                    return;
                }


                Match match = Regex.Match(playerName, @"\d+");
                if (match.Success)
                {
                    // Get the last number found in the name
                    var numbers = Regex.Matches(playerName, @"\d+");
                    int.TryParse(numbers[numbers.Count - 1].Value, out playerNo);
                }
                else
                {
                    playerNo = 0; // Default if no number found
                }
                Debug.Log($"Player No: {playerNo}");
                if (playerNo % 2 == 0)
                {
                    Debug.Log("Player is Adaptive");
                }
                else
                {
                    Debug.Log("Player is Non-Adaptive");
                }
                lobbyState = LobbyState.MapSelect;
                SinglePlayerSetup();

            });


            // GameMgr.inst.OpenOcean1x1(); //GameMgr.inst.MakeMapEntities();
        }
        else
        {

            lobbyState = LobbyState.SingleMultiPlayer;

            loginButton.onClick.RemoveAllListeners();
            loginButton.onClick.AddListener(() =>
            {
                playerName = loginNameInputField.text.Trim();
                playerCode = loginCodeInputField.text.Trim();

                lobbyState = LobbyState.MapSelect;
                if (isSinglePlayer)
                {
                    SinglePlayerSetup();
                }
                else
                {
                    NetPlayersSetup();
                }
            });
        }
    }

    void NetPlayersSetup()
    {
        Debug.Log("Setting up network multiplayer ...");
        foreach (NetSetup ns in FindObjectsOfType<NetSetup>())
        {
            NetworkObject tmp = ns.GetComponent<NetworkObject>();
            if (tmp.IsLocalPlayer)
            {
                localNetSetupNetworkObject = tmp;
                localNetSetup = ns;
            }
        }
        foreach (TactNetMgr tnm in FindObjectsOfType<TactNetMgr>())
        {
            NetworkObject tmp = tnm.GetComponent<NetworkObject>();
            if (tmp.IsLocalPlayer)
            {
                localTactNetMgrNetworkObject = tmp;
                localTactNetMgr = tnm;
            }
        }


    }

    void SinglePlayerSetup()
    {
        Debug.Log("Setting up single player ...");
        TactPlayer tmp = PlayerMgr.inst.CreateSinglePlayer(playerName);
        PlayerMgr.inst.AddPlayer(tmp);
        PlayerMgr.inst.localPlayer = tmp;

        PlayerMgr.inst.AddPlayer(PlayerMgr.inst.CreateSinglePlayer("Ai"));
    }

    public LobbyState lobbyState
    {
        get
        {
            return _lobbyState;
        }
        set
        {
            _lobbyState = value;

            loginPanel.isVisible = (value == LobbyState.Login);
            mapSelectPanel.isVisible = (value == LobbyState.MapSelect);
            HostOrJoinPanel.isVisible = (value == LobbyState.HostOrJoin);
            MainGamePanel.isVisible = (value == LobbyState.Play);
            NetDebugConsolePanel.gameObject.SetActive(IsDebugging && IsNetDebugging);
            SingleMultiplayerPanel.isVisible = (value == LobbyState.SingleMultiPlayer);
            ScorePanel.isVisible = (value == LobbyState.ScorePanel);

            if (value == LobbyState.ScorePanel)
            {
                gamesPlayedCount++; // Increment when a game is completed and score panel is shown
                TMP_Text buttonTextComponent = nextGameButton.GetComponentInChildren<TMP_Text>();
                if (buttonTextComponent != null)
                {
                    if (gamesPlayedCount >= 5)
                    {
                        buttonTextComponent.text = EXIT_BUTTON_TEXT;
                    }
                    else
                    {
                        buttonTextComponent.text = NEXT_GAME_BUTTON_TEXT;
                    }
                }
            }
            
            Time.timeScale = 0f;
            Time.timeScale = (value == LobbyState.Play) ? 1f : 0f;
        }
    }



    public void OnMapSelected()
    {
        if (isSinglePlayer)
            GameMgr.inst.OpenOcean1x1(); //GameMgr.inst.MakeMapEntities();
        else
            localNetSetup.OnStartButton();
        lobbyState = LobbyState.Play;
    }


    public void OnSinglePlayer()
    {
        isSinglePlayer = true;
        lobbyState = LobbyState.Login;
    }
    public void OnMultiPlayer()
    {
        isSinglePlayer = false;
        lobbyState = LobbyState.HostOrJoin;
    }


    public void OnQuitButton()
    {
        Debug.Log("Shutting down TactNetMgr and quitting");
        if (TactNetMgr.inst != null) // Check if TactNetMgr.inst exists
        {
            TactNetMgr.inst.TactNetShutdown();
        }
        if (NetworkManager.Singleton != null) // Check if NetworkManager.Singleton exists
        {
            NetworkManager.Singleton.Shutdown();
        }
        
        if (Application.isEditor)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
        else
        {
            Application.Quit();
        }

    }
    
    public void ResetGameState()
    {
        lobbyState = LobbyState.Play;
    }

    public void OnNextGameOrExitClicked()
    {
        if (gamesPlayedCount >= 5)
        {
            OnQuitButton();
        }
        }}