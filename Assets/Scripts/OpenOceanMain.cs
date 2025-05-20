using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro;
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
    private float savedTimeScale = 1f;
    private float totalPlayTime = 0f;

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
    [SerializeField]
    private PanelPlus GamePausePanel;

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
    private Button startButton;
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
    private float playSessionStartTime; // This variable is still present from previous logic, but totalPlayTime is now the primary tracker
    public float playSessionDuration { get; private set; }

    [Header("Game Pause Panel")]
    [SerializeField]
    private Button resumeButton;
    [SerializeField]
    private Button quitButton;
    [SerializeField]
    private List<Button> menuButtons;


    public enum LobbyState
    {
        None = 0,
        SingleMultiPlayer,
        Login,
        MapSelect,
        HostOrJoin,
        Play,
        Done,
        ScorePanel,
        GamePaused
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
        resumeButton.onClick.RemoveAllListeners();
        resumeButton.onClick.AddListener(OnResumeButton);
        quitButton.onClick.RemoveAllListeners();
        quitButton.onClick.AddListener(OnQuitButton);
        foreach (Button button in menuButtons)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnMenuButton);
        }

        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnMapSelected);
        }
        else
        {
            Debug.LogError("StartButton is not assigned in the Inspector.", this);
        }

        nextGameButton.onClick.RemoveAllListeners();
        nextGameButton.onClick.AddListener(OnNextGameOrExitClicked);
    }

    void SetupIPAddressAndPort()
    {
        if (ipAddressInputField == null)
        {
            Debug.LogError("ipAddressInputField is not assigned.", this);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(ipAddress, port); // Use default
            return;
        }
        string tmp = ipAddressInputField.text.Trim();
        int count = tmp.Count(x => x == '.');
        if (count == 3) // Basic validation
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
            Debug.LogError("Login input fields are not assigned.", this);
            return false;
        }

        playerName = loginNameInputField.text.Trim();
        playerCode = loginCodeInputField.text.Trim();

        if (WrongNameText != null) WrongNameText.gameObject.SetActive(false);
        if (WrongCodeText != null) WrongCodeText.gameObject.SetActive(false);

        Regex nameRegex = new Regex(@"^Student\s*\d+$");
        if (!nameRegex.IsMatch(playerName))
        {
            if (WrongNameText != null)
            {
                WrongNameText.text = "Invalid name format. Expected 'Student <number>'.";
                StartCoroutine(ShowMessageForDuration(WrongNameText, 10f));
            }
            return false;
        }

        if (playerCode == "AAA")
        {
            currentTrainingState = TrainingState.Adaptive;
            if (GameMgr.inst != null) GameMgr.inst.difficultyLevel = 0.2f;
        }
        else if (playerCode == "BBB")
        {
            currentTrainingState = TrainingState.NonAdaptive;
        }
        else if (playerCode == "ABC")
        {
            currentTrainingState = TrainingState.PreTest;
        }
        else if (playerCode == "XYZ")
        {
            currentTrainingState = TrainingState.PostTest;
        }
        else
        {
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
                if (IsDebugging) Debug.LogWarning($"Could not parse player number from name: '{playerName}'. Defaulting to 0.", this);
                playerNo = 0;
            }
        }
        else
        {
            if (IsDebugging) Debug.LogWarning($"No number found at the end of player name: '{playerName}'. Defaulting to 0.", this);
            playerNo = 0;
        }

        if (GameMgr.inst != null)
        {
            Random.InitState(GameMgr.inst.GetSelectedSeed());
            if (IsDebugging) Debug.Log($"Player No set to: {playerNo}. Training State: {currentTrainingState}. GameMgr seed set to: {GameMgr.inst.GetSelectedSeed()}", this);
        }
        else
        {
            Debug.LogWarning("GameMgr.inst is null. Cannot set seed.", this);
            if (IsDebugging) Debug.Log($"Player No set to: {playerNo}. Training State: {currentTrainingState}. GameMgr seed NOT set.", this);
        }

        return true;
    }

    private IEnumerator ShowMessageForDuration(TMP_Text textElement, float duration)
    {
        if (textElement != null)
        {
            textElement.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(duration);
            textElement.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        loginButton.onClick.RemoveAllListeners();
        loginButton.onClick.AddListener(() =>
        {
            if (IsDebugging) Debug.Log("Login button pressed.", this);
            if (ProcessLogin())
            {
                lobbyState = LobbyState.MapSelect;
                if (isSinglePlayer)
                {
                    SinglePlayerSetup();
                }
                else
                {
                    NetPlayersSetup();
                }
            }
            else
            {
                Debug.LogError("Login failed. Check input fields or logs.", this);
            }
        });

        if (IsDebugging)
        {
            lobbyState = LobbyState.Login;
            // isSinglePlayer defaults to true, so SinglePlayerSetup will be called after login if ProcessLogin is successful.
        }
        else
        {
            lobbyState = LobbyState.SingleMultiPlayer;
        }
    }


    void NetPlayersSetup()
    {
        if (IsDebugging) Debug.Log("Setting up network multiplayer ...", this);
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
        if (IsDebugging) Debug.Log("Setting up single player ...", this);
        TactPlayer tmp = PlayerMgr.inst.CreateSinglePlayer(playerName);
        PlayerMgr.inst.AddPlayer(tmp);
        PlayerMgr.inst.localPlayer = tmp;

        PlayerMgr.inst.AddPlayer(PlayerMgr.inst.CreateSinglePlayer("Ai"));
    }

    private void UpdateMapSelectionUI()
    {
        string name = "Default Map";
        string description = "Default description.";

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
            case TrainingState.Adaptive:
                name = (playerNo % 2 == 0) ? "Training" : "Alternate Training";
                description = (playerNo % 2 == 0) ? "This is a training session." : "This is an alternate training session.";
                break;
            case TrainingState.NonAdaptive:
                name = (playerNo % 2 != 0) ? "Training" : "Alternate Training";
                description = (playerNo % 2 != 0) ? "This is a training session." : "This is an alternate training session.";
                break;
            case TrainingState.None:
                name = "Map Selection Pending";
                description = "Please complete login to determine training type.";
                Debug.LogWarning("UpdateMapSelectionUI called with TrainingState.None. Login might not be complete or code is invalid.", this);
                break;
        }

        if (mapNameText != null) mapNameText.text = name;
        if (mapDescriptionText != null) mapDescriptionText.text = description;
    }


    public LobbyState lobbyState
    {
        get => _lobbyState;
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
            GamePausePanel.isVisible = (value == LobbyState.GamePaused);


            if (value == LobbyState.MapSelect)
            {
                UpdateMapSelectionUI();
            }

            // Time scale management
            if (value == LobbyState.Play)
            {
                if (previousState == LobbyState.GamePaused)
                {
                    // Restore saved time scale
                    Time.timeScale = savedTimeScale;
                }
                else if (previousState != LobbyState.Play) // Started playing (not from pause)
                {
                      // Ensure time scale is 1 when starting play
                     // playSessionStartTime = Time.realtimeSinceStartup; // This line is from old logic, totalPlayTime handles this now
                     if (IsDebugging) Debug.Log($"Play session started. Total playtime reset/started.", this);
                }
            }
            else // Not in Play state
            {
                if (previousState == LobbyState.Play)
                {
                    // Save current time scale if we were playing
                    savedTimeScale = Time.timeScale;
                }
                Time.timeScale = 0f; // Pause the game
            }


            // Update total playtime when entering score panel
            if (value == LobbyState.ScorePanel && previousState == LobbyState.Play)
            {
                playSessionDuration = totalPlayTime; // Store the accumulated playtime
                if (IsDebugging) Debug.Log($"Play session ended. Duration: {playSessionDuration:F2} seconds (from totalPlayTime).", this);
                totalPlayTime = 0f; // Reset for the next session

                gamesPlayedCount++;
                TMP_Text buttonTextComponent = nextGameButton.GetComponentInChildren<TMP_Text>();
                if (buttonTextComponent != null)
                {
                    buttonTextComponent.text = (gamesPlayedCount >= gamePlayCountMAX) ? EXIT_BUTTON_TEXT : NEXT_GAME_BUTTON_TEXT;
                }
            }
            
            if (UIMgr.inst != null) UIMgr.inst.gameObject.SetActive(value == LobbyState.Play);
            if (GroupUIMgr.inst != null) GroupUIMgr.inst.gameObject.SetActive(value == LobbyState.Play);
        }
    }

    void Update()
    {
        if (lobbyState == LobbyState.Play)
        {
            totalPlayTime += Time.unscaledDeltaTime; // Use unscaledDeltaTime to track time even if Time.timeScale is modified (e.g. slow-mo effects)
        }
        // ... existing update code ...
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (lobbyState == LobbyState.Play)
            {
                lobbyState = LobbyState.GamePaused;
            }
            else if (lobbyState == LobbyState.GamePaused)
            {
                lobbyState = LobbyState.Play;
            }
        }
    }


    public void OnMapSelected()
    {
        totalPlayTime = 0f; // Reset playtime when starting a new game
        if (isSinglePlayer)
        {
            GameMgr.inst.OpenOcean1x1();
        }
        else if (localNetSetup != null)
        {
            localNetSetup.OnStartButton();
        }
        else
        {
            Debug.LogError("localNetSetup is null. Cannot start multiplayer map selection.", this);
        }
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
        if (IsDebugging) Debug.Log("Shutting down TactNetMgr and quitting application.", this);
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
    public void OnResumeButton()
    {
        lobbyState = LobbyState.Play;
    }
    public void OnMenuButton()
    {
        lobbyState = LobbyState.GamePaused;
    }
    public void ResetGameState()
    {
        // Consider resetting player scores, map states, etc.
        lobbyState = LobbyState.Play; // Or LobbyState.Login / SingleMultiPlayer
    }

    public void OnNextGameOrExitClicked()
    {
        if (gamesPlayedCount >= gamePlayCountMAX)
        {
            OnQuitButton();
        }
        else
        {
            if (IsDebugging) Debug.Log("Next game selected.", this);
            lobbyState = LobbyState.Play; 
        }
    }
}
