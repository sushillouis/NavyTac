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

    public int gamePlayCountMAX = 5;
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
    public TMP_Text WrongCodeText;
    [SerializeField]
    public TMP_Text WrongNameText;
    [SerializeField]
    private Button loginButton;
    [SerializeField]
    private Button LoginQuitButton;

    [Header("Map Select Screen")]
    [SerializeField]
    public TMP_Text mapNameText;
    [SerializeField]
    public TMP_Text mapDescriptionText;
    [SerializeField]
    private Button startButton; // Added SerializeField and declaration
    [Header("Score Panel")]

    [SerializeField] public TMP_Text damageDealtText;
    [SerializeField] public TMP_Text damageTakenText;
    [SerializeField] public TMP_Text winnerText;
    [SerializeField] public TMP_Text scoreText;
    [SerializeField] public TMP_Text ourUnitsDestroyedText;
    [SerializeField] public TMP_Text enemyUnitsDestroyedText;
    [SerializeField] public TMP_Text feedbackText;
    [SerializeField] public TMP_Text playerNameText;

    [SerializeField] public Button nextGameButton;
    [SerializeField] public TMP_Text winConditionText;

    public int gamesPlayedCount = 0;
    private const string NEXT_GAME_BUTTON_TEXT = "Next Game";
    private const string EXIT_BUTTON_TEXT = "Exit";

    // Variables for play session timing
    private float playSessionStartTime;
    public float playSessionDuration { get; private set; }

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

        SinglePlayerButton.onClick.RemoveAllListeners();
        SinglePlayerButton.onClick.AddListener(OnSinglePlayer);

        MultiPlayerButton.onClick.RemoveAllListeners();
        MultiPlayerButton.onClick.AddListener(OnMultiPlayer);

        SingleMultiQuitButton.onClick.RemoveAllListeners();
        SingleMultiQuitButton.onClick.AddListener(OnQuitButton);

        if (startButton != null) // Ensure startButton is assigned in Inspector
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnMapSelected);
        }
        else
        {
            Debug.LogError("StartButton is not assigned in the Inspector.");
        }
        
        nextGameButton.onClick.RemoveAllListeners();
        nextGameButton.onClick.AddListener(OnNextGameOrExitClicked);
    }

    void SetupIPAddressAndPort()
    {
        string tmp = ipAddressInputField.text.Trim();
        int count = tmp.Count(x => x == '.');
        if (count == 3)
            ipAddress = tmp;
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(ipAddress, port);
    }

    [Header("To be filled on Login Button Press/Network setup")]
    public NetSetup localNetSetup;
    public NetworkObject localNetSetupNetworkObject;
    public TactNetMgr localTactNetMgr;
    public NetworkObject localTactNetMgrNetworkObject;

    private bool ProcessLogin()
    {
        if (loginNameInputField == null || loginCodeInputField == null)
        {
            Debug.LogError("Login input fields are not assigned.");
            return false;
        }

        playerName = loginNameInputField.text.Trim();
        playerCode = loginCodeInputField.text.Trim();
        
        Debug.Log($"Attempting login for Player Name: {playerName}, Code: {playerCode}");

        // Hide previous error messages
        if (WrongNameText != null) WrongNameText.gameObject.SetActive(false);
        if (WrongCodeText != null) WrongCodeText.gameObject.SetActive(false);

        Regex nameRegex = new Regex(@"^Student\s*\d+$");
        if (!nameRegex.IsMatch(playerName))
        {
            Debug.LogWarning($"Invalid player name format: '{playerName}'. Expected 'Student <number>'.");
            if (WrongNameText != null)
            {
                WrongNameText.text = "Invalid name format. Expected 'Student <number>'.";
                StartCoroutine(ShowMessageForDuration(WrongNameText, 10f));
            }
            return false;
        }

        if (playerCode == "AAA")
        {
            Debug.Log("Code: AAA (Adaptive session type)");
            currentTrainingState = TrainingState.Adaptive;
            if (GameMgr.inst != null) GameMgr.inst.difficultyLevel = 0.2f;
        }
        else if (playerCode == "BBB")
        {
            Debug.Log("Code: BBB (Non-Adaptive session type)");
            currentTrainingState = TrainingState.NonAdaptive;
        }
        else if (playerCode == "ABC")
        {
            Debug.Log("Code: ABC (Pre-test session type)");
            currentTrainingState = TrainingState.PreTest;
        }
        else if (playerCode == "XYZ")
        {
            Debug.Log("Code: XYZ (Post-test session type)");
            currentTrainingState = TrainingState.PostTest;
        }
        else
        {
            Debug.LogWarning($"Invalid player code: '{playerCode}'.");
            if (WrongCodeText != null)
            {
                WrongCodeText.text = "Invalid login code.";
                StartCoroutine(ShowMessageForDuration(WrongCodeText, 10f));
            }
            return false;
        }

        Match numberMatch = Regex.Match(playerName, @"\d+$");
        if (numberMatch.Success)
        {
            if (!int.TryParse(numberMatch.Value, out playerNo))
            {
                Debug.LogWarning($"Could not parse player number from name: '{playerName}'.");
                playerNo = 0; // Default or handle as error
            }
        }
        else
        {
            Debug.LogWarning($"No number found at the end of player name: '{playerName}'.");
            playerNo = 0; // Default or handle as error
        }
        
        // Set seed for GameMgr based on the currentTrainingState
        if (GameMgr.inst != null)
        {
            // Assuming GameMgr.GetSelectedSeed() is a public method in GameMgr
            // and GameMgr.CurrentSeed is a public field/property in GameMgr.
            // The GetSelectedSeed() method (defined in GameMgr) uses OpenOceanMain.inst.currentTrainingState.
            Random.InitState( GameMgr.inst.GetSelectedSeed()); 
            Debug.Log($"Player No set to: {playerNo}. Training State: {currentTrainingState}. GameMgr seed set to: {GameMgr.inst.GetSelectedSeed()}");
        }
        else
        {
            Debug.LogWarning("GameMgr.inst is null. Cannot set seed.");
            Debug.Log($"Player No set to: {playerNo}. Training State: {currentTrainingState}. GameMgr seed NOT set.");
        }
        
        return true;
    }

    private IEnumerator ShowMessageForDuration(TMP_Text textElement, float duration)
    {
        if (textElement != null)
        {
            textElement.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(duration); // Use WaitForSecondsRealtime as Time.timeScale might be 0
            textElement.gameObject.SetActive(false);
        }
    }

    // Overload for custom messages

    private void Start()
    {
        if (IsDebugging)
        {
            // Time.timeScale = 0f; // Time.timeScale is handled by lobbyState setter
            lobbyState = LobbyState.Login; // Start at login for debug
            loginButton.onClick.RemoveAllListeners();
            loginButton.onClick.AddListener(() =>
            {
                Debug.Log("Login button pressed (Debug Mode)");
                if (ProcessLogin())
                {
                    lobbyState = LobbyState.MapSelect;
                    SinglePlayerSetup(); // Debug mode defaults to single player setup after login
                }
                else
                {
                    Debug.LogError("Login failed in debug mode. Check input fields or logs.");
                    // Optionally, show an error message on the UI
                }
            });
        }
        else
        {
            lobbyState = LobbyState.SingleMultiPlayer;
            loginButton.onClick.RemoveAllListeners();
            loginButton.onClick.AddListener(() =>
            {
                Debug.Log("Login button pressed (Normal Mode)");
                if (ProcessLogin())
                {
                    lobbyState = LobbyState.MapSelect;
                    if (isSinglePlayer)
                    {
                        SinglePlayerSetup();
                    }
                    else
                    {
                        // For multiplayer, NetPlayersSetup might need to happen after network connection
                        // or ensure local player data is ready for map selection logic.
                        // Current ProcessLogin sets local playerNo and currentTrainingState.
                        NetPlayersSetup();
                    }
                }
                else
                {
                     Debug.LogError("Login failed. Check input fields or logs.");
                    // Optionally, show an error message on the UI
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
    
    private void UpdateMapSelectionUI()
    {
        string name = "Default Map"; // Fallback name
        string description = "Default description."; // Fallback description

        switch (currentTrainingState)
        {
            case TrainingState.PreTest:
                name = "Pre Test";
                description = "This is a pre-test session.";
                break;
            case TrainingState.PostTest:
                name = "Post Test";
                description = "This is a post-test session.";
                break;
            case TrainingState.Adaptive: // Player is configured for an Adaptive type session
                // playerNo % 2 == 0 suggests player is from "Adaptive Group" (even ID)
                // playerNo % 2 != 0 suggests player is from "Non-Adaptive Group" (odd ID)
                if (playerNo % 2 == 0) // Player from "Adaptive Group" doing Adaptive session
                {
                    name = "Training";
                    description = "This is a training session.";
                }
                else // Player from "Non-Adaptive Group" doing Adaptive session
                {
                    name = "Alternate Training";
                    description = "This is an alternate training session.";
                }
                break;
            case TrainingState.NonAdaptive: // Player is configured for a Non-Adaptive type session
                if (playerNo % 2 != 0) // Player from "Non-Adaptive Group" doing Non-Adaptive session
                {
                    name = "Training";
                    description = "This is a training session.";
                }
                else // Player from "Adaptive Group" doing Non-Adaptive session
                {
                    name = "Alternate Training";
                    description = "This is an alternate training session.";
                }
                break;
            case TrainingState.None:
                name = "Map Selection Pending";
                description = "Please complete login to determine training type.";
                Debug.LogWarning("UpdateMapSelectionUI called with TrainingState.None. Login might not be complete or code is invalid.");
                break;
        }

        if (mapNameText != null)
        {
            mapNameText.text = name;
        }
        else
        {
            Debug.LogError("mapNameText is not assigned in the Inspector.");
        }

        if (mapDescriptionText != null)
        {
            mapDescriptionText.text = description;
        }
        else
        {
            Debug.LogError("mapDescriptionText is not assigned in the Inspector.");
        }
    }


    public LobbyState lobbyState
    {
        get
        {
            return _lobbyState;
        }
        set
        {
            LobbyState previousState = _lobbyState;
            _lobbyState = value;

            loginPanel.isVisible = (value == LobbyState.Login);
            mapSelectPanel.isVisible = (value == LobbyState.MapSelect);
            HostOrJoinPanel.isVisible = (value == LobbyState.HostOrJoin);
            MainGamePanel.isVisible = (value == LobbyState.Play);
            NetDebugConsolePanel.gameObject.SetActive(IsDebugging && IsNetDebugging);
            SingleMultiplayerPanel.isVisible = (value == LobbyState.SingleMultiPlayer);
            ScorePanel.isVisible = (value == LobbyState.ScorePanel);

           
            if (value == LobbyState.MapSelect)
            {
                UpdateMapSelectionUI(); // Update map name and description
            }
            
            if (value == LobbyState.Play)
            {
                playSessionStartTime = Time.realtimeSinceStartup;
                Debug.Log($"Play session started. Start time: {playSessionStartTime}");
            }
            
            if (value == LobbyState.ScorePanel)
            {
                if (previousState == LobbyState.Play)
                {
                    playSessionDuration = Time.realtimeSinceStartup - playSessionStartTime;
                    Debug.Log($"Play session ended. Duration: {playSessionDuration:F2} seconds.");
                }

                gamesPlayedCount++;
                TMP_Text buttonTextComponent = nextGameButton.GetComponentInChildren<TMP_Text>();
                if (buttonTextComponent != null)
                {
                    if (gamesPlayedCount >= gamePlayCountMAX)
                    {
                        buttonTextComponent.text = EXIT_BUTTON_TEXT;
                    }
                    else
                    {
                        buttonTextComponent.text = NEXT_GAME_BUTTON_TEXT;
                    }
                }
            }
            
            Time.timeScale = (value == LobbyState.Play) ? 1f : 0f;
            if (UIMgr.inst != null)  UIMgr.inst.gameObject.SetActive(value == LobbyState.Play);
            if (GroupUIMgr.inst != null) GroupUIMgr.inst.gameObject.SetActive(value == LobbyState.Play);
            
        }
    }

    public void OnMapSelected()
    {
        if (isSinglePlayer)
            GameMgr.inst.OpenOcean1x1();
        else if (localNetSetup != null) // Ensure localNetSetup is initialized for multiplayer
            localNetSetup.OnStartButton();
        else
            Debug.LogError("localNetSetup is null. Cannot start multiplayer map selection.");
            
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
        if (TactNetMgr.inst != null)
        {
            TactNetMgr.inst.TactNetShutdown();
        }
        if (NetworkManager.Singleton != null)
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
        // This method might need more logic to truly reset the game for a new session
        // For now, it just sets the lobby state.
        // Consider resetting player scores, map states, etc.
        lobbyState = LobbyState.Play; // Or LobbyState.Login / SingleMultiPlayer depending on desired flow
    }

    public void OnNextGameOrExitClicked()
    {
        if (gamesPlayedCount >= gamePlayCountMAX) // Use gamePlayCountMAX
        {
            OnQuitButton();
        }
        else 
        {
            // Logic for "Next Game"
            // This could mean going back to map select, or login, or single/multi selection
            // For now, let's assume going back to map select if another game is to be played.
            // You might need to reset other game-specific states here.
            Debug.Log("Next game selected.");
            lobbyState = LobbyState.Play; // Or another appropriate state like Login or SingleMultiPlayer
        }
    }
}