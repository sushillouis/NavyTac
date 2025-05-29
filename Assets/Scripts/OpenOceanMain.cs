using System.Threading;
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
    public float totalTrainingTime = 0f;
    public float nonAdaptiveTrainingTime = 15f;
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
    [SerializeField]
    private PanelPlus MultiScorePanel;
    [SerializeField]
    private PanelPlus ReplayPanel;

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

    [Header("MultiScorePanel")]
    [SerializeField] public GameObject MultiScoreList;
    [SerializeField] public GameObject Score;

    [SerializeField] public TMP_Text scenarioNumberText;
    [SerializeField] public TMP_Text totalUnitsText;
    [SerializeField] public TMP_Text totalUnitXText;
    [SerializeField] public TMP_Text totalSeaHunterText;
    [SerializeField] public TMP_Text totalDDG51Text;
    [SerializeField] public TMP_Text ourUnitsDestroyedTextMulti;
    [SerializeField] public TMP_Text ourDestroyedUnitXText;
    [SerializeField] public TMP_Text ourDestroyedSeaHunterText;
    [SerializeField] public TMP_Text ourDestroyedDDG51Text;
    [SerializeField] public TMP_Text enemyUnitsDestroyedTextMulti;
    [SerializeField] public TMP_Text enemyDestroyedUnitXText;
    [SerializeField] public TMP_Text enemyDestroyedSeaHunterText;
    [SerializeField] public TMP_Text enemyDestroyedDDG51Text;
    [SerializeField] public TMP_Text damageTakenTextMulti;
    [SerializeField] public TMP_Text damageDealtTextMulti;
    [SerializeField] public TMP_Text winLossTextMulti;
    [SerializeField] public TMP_Text scoreTextMulti;
    [SerializeField] public Button nextGameOrExitButton;
    [SerializeField] public Button backButton;

    public int gamesPlayedCount = 0;
    private const string Score_PANEL_TEXT = "Feedback";
    private const string NEXT_GAME_BUTTON_TEXT = "Next";
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

    [Header("Replay Panel")]

    [SerializeField]
    private Button replayExitButton;



    [Header("Lobby State and the rest")]

    [SerializeField]
    private LobbyState _lobbyState = LobbyState.None;

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
        nextGameOrExitButton.onClick.RemoveAllListeners();
        nextGameOrExitButton.onClick.AddListener(OnNextClicked);
        replayExitButton.onClick.RemoveAllListeners();
        replayExitButton.onClick.AddListener(OnScoreListClicked);
        backButton.onClick.RemoveAllListeners();
        backButton.onClick.AddListener(BackButton);
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
            UnityEngine.Random.InitState(GameMgr.inst.GetSelectedSeed());
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
            MultiScorePanel.isVisible = (value == LobbyState.MultiScorePanel);
            ReplayPanel.isVisible = (value == LobbyState.Replay);

            if (value == LobbyState.MapSelect)
            {
                UpdateMapSelectionUI();
            }

            if (value == LobbyState.Play)
            {
                if (previousState == LobbyState.GamePaused)
                {
                    Time.timeScale = savedTimeScale;
                }
                else if (previousState != LobbyState.Play)
                {
                    if (IsDebugging) Debug.Log($"Play session started. Total playtime reset/started.", this);
                }
            }
            else
            {
                if (previousState == LobbyState.Play)
                {
                    savedTimeScale = Time.timeScale;
                }
                Time.timeScale = 0f;
            }

            if (value == LobbyState.ScorePanel && previousState == LobbyState.Play)
            {
                playSessionDuration = totalPlayTime;
                totalTrainingTime += playSessionDuration;
                if (IsDebugging) Debug.Log($"Play session ended. Duration: {playSessionDuration:F2} seconds (from totalPlayTime).", this);
                totalPlayTime = 0f;

                gamesPlayedCount++;
                TMP_Text buttonTextComponentMultiScore = nextGameOrExitButton.GetComponentInChildren<TMP_Text>();
                if (buttonTextComponentMultiScore != null)
                {
                    if (currentTrainingState == TrainingState.NonAdaptive)
                    {
                        buttonTextComponentMultiScore.text = (totalTrainingTime >= nonAdaptiveTrainingTime * 60f) ? EXIT_BUTTON_TEXT : NEXT_GAME_BUTTON_TEXT;
                    }
                    else
                    {
                        if (currentTrainingState == TrainingState.Adaptive)
                        {

                            buttonTextComponentMultiScore.text = (gamesPlayedCount >= gamePlayCountMAX) ? EXIT_BUTTON_TEXT : NEXT_GAME_BUTTON_TEXT;
                        }
                        else // PreTest, PostTest, None
                        {
                            buttonTextComponentMultiScore.text = (gamesPlayedCount >= gamePlayCountMAX) ? EXIT_BUTTON_TEXT : NEXT_GAME_BUTTON_TEXT;
                        }

                    }
                }

                TMP_Text buttonTextComponent = nextGameButton.GetComponentInChildren<TMP_Text>();
                if (buttonTextComponent != null)
                {
                    if (currentTrainingState == TrainingState.Adaptive)
                    {
                        buttonTextComponent.text = Score_PANEL_TEXT;
                    }
                    else if (currentTrainingState == TrainingState.NonAdaptive)
                    {
                        buttonTextComponent.text = (totalTrainingTime >= nonAdaptiveTrainingTime * 60f) ? Score_PANEL_TEXT : NEXT_GAME_BUTTON_TEXT;
                    }
                    else // PreTest, PostTest, None
                    {
                        buttonTextComponent.text = (gamesPlayedCount >= gamePlayCountMAX) ? Score_PANEL_TEXT : NEXT_GAME_BUTTON_TEXT;
                    }
                }
            }

            if (UIMgr.inst != null) UIMgr.inst.gameObject.SetActive(value == LobbyState.Play);
            if (GroupUIMgr.inst != null) GroupUIMgr.inst.gameObject.SetActive(value == LobbyState.Play);
            if (value == LobbyState.MultiScorePanel)
            {
                if (previousState != LobbyState.Replay)
                {
                    UpdateMultiScoreDisplay();
                }

            }

            if (value == LobbyState.Replay)
            {
                Time.timeScale = 1f;
                if (ReplayMgr.inst != null)
                {
                    ReplayMgr.inst.StartReplay();
                }
                else
                {
                    Debug.LogError("ReplayMgr instance is null.");
                }
            }

            if (IsDebugging) Debug.Log($"Lobby state changed from {previousState} to {value}.", this);
        }
    }

    void Update()
    {
        if (lobbyState == LobbyState.Play)
        {
            totalPlayTime += Time.unscaledDeltaTime;
            totalTrainingTime += Time.unscaledDeltaTime; // Use unscaledDeltaTime to track time even if Time.timeScale is modified (e.g. slow-mo effects)
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


    // In OpenOceanMain.cs
    public void OnMapSelected()
    {
        totalPlayTime = 0f;
        int scenarioNumber = gamesPlayedCount + 1;

        // Then spawn entities
        if (isSinglePlayer)
        {
            GameMgr.inst.OpenOcean1x1();
        }
        else if (localNetSetup != null)
        {
            localNetSetup.OnStartButton();
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


        if (currentTrainingState == TrainingState.Adaptive)
        {
            // In Adaptive mode, after every game's score screen, proceed to a multi-game score screen.
            if (IsDebugging) Debug.Log("Adaptive mode: Next button clicked on score panel. Proceeding to MultiScorePanel.", this);
            lobbyState = LobbyState.MultiScorePanel;
        }
        else if (currentTrainingState == TrainingState.NonAdaptive)
        {
            if (totalTrainingTime >= nonAdaptiveTrainingTime * 60f) // nonAdaptiveTrainingTime is in minutes
            {
                if (IsDebugging) Debug.Log($"Non-Adaptive mode, training time {totalTrainingTime / 60f:F2}m / {nonAdaptiveTrainingTime}m. Proceeding to MultiScorePanel.", this);
                lobbyState = LobbyState.MultiScorePanel;
            }
            else
            {
                if (IsDebugging) Debug.Log($"Non-Adaptive mode, training time {totalTrainingTime / 60f:F2}m / {nonAdaptiveTrainingTime}m. Starting next game.", this);
                lobbyState = LobbyState.Play;
            }
        }
        else // For PreTest, PostTest, or None training states
        {
            if (gamesPlayedCount >= gamePlayCountMAX)
            {
                // In other cases (PreTest, PostTest), after the max number of games, go to the multi-game score screen.
                if (IsDebugging) Debug.Log($"Other mode ({currentTrainingState}), {gamesPlayedCount}/{gamePlayCountMAX} games played. Proceeding to MultiScorePanel.", this);
                lobbyState = LobbyState.MultiScorePanel;
            }
            else
            {
                // In other cases (PreTest, PostTest), if more games are to be played, start the next game.
                if (IsDebugging) Debug.Log($"Other mode ({currentTrainingState}), {gamesPlayedCount}/{gamePlayCountMAX} games played. Starting next game.", this);
                lobbyState = LobbyState.Play;
            }
        }
    }

    public void OnNextClicked()
    {
        if (currentTrainingState == TrainingState.NonAdaptive)
        {
            if (totalTrainingTime >= nonAdaptiveTrainingTime * 60f) // nonAdaptiveTrainingTime is in minutes
            {
                if (IsDebugging) Debug.Log($"Non-Adaptive mode on MultiScorePanel: Training time limit reached ({totalTrainingTime / 60f:F2}m / {nonAdaptiveTrainingTime}m). Exiting.", this);
                OnQuitButton();
            }
            else
            {
                if (IsDebugging) Debug.Log($"Non-Adaptive mode on MultiScorePanel: Training time {totalTrainingTime / 60f:F2}m / {nonAdaptiveTrainingTime}m. Proceeding to next game.", this);
                lobbyState = LobbyState.Play;
            }
        }
        else // For Adaptive, PreTest, PostTest, or None training states
        {
            if (gamesPlayedCount >= gamePlayCountMAX)
            {
                if (IsDebugging) Debug.Log($"Other mode ({currentTrainingState}) on MultiScorePanel: Max games played ({gamesPlayedCount}/{gamePlayCountMAX}). Exiting.", this);
                OnQuitButton();
            }
            else
            {
                if (IsDebugging) Debug.Log($"Other mode ({currentTrainingState}) on MultiScorePanel: {gamesPlayedCount}/{gamePlayCountMAX} games played. Proceeding to next game.", this);
                lobbyState = LobbyState.Play;
            }
        }
    }
    private void UpdateMultiScoreDisplay()
    {
        if (MultiScoreList == null)
        {
            Debug.LogError("MultiScoreList GameObject (the container for score entries) is not assigned in the Inspector.");
            return;
        }
        if (Score == null)
        {
            Debug.LogError("Score prefab/template is not assigned in the Inspector.");
            return;
        }

        // Clear previously instantiated items from MultiScoreList
        foreach (Transform child in MultiScoreList.transform)
        {
            child.gameObject.SetActive(false);
        }

        if (ScenarioDataMgr.inst == null || ScenarioDataMgr.inst.scenarioDataList == null)
        {
            Debug.LogWarning("ScenarioDataMgr instance or scenarioDataList is null. Cannot display multi-score data.");
            if (scenarioNumberText != null) scenarioNumberText.text = "0";
            if (totalUnitsText != null) totalUnitsText.text = "N/A";
            // Clear other summary fields
            if (totalUnitXText != null) totalUnitXText.text = "0";
            if (totalSeaHunterText != null) totalSeaHunterText.text = "0";
            if (totalDDG51Text != null) totalDDG51Text.text = "0";
            if (ourUnitsDestroyedTextMulti != null) ourUnitsDestroyedTextMulti.text = "0";
            if (ourDestroyedUnitXText != null) ourDestroyedUnitXText.text = "0";
            if (ourDestroyedSeaHunterText != null) ourDestroyedSeaHunterText.text = "0";
            if (ourDestroyedDDG51Text != null) ourDestroyedDDG51Text.text = "0";
            if (enemyUnitsDestroyedTextMulti != null) enemyUnitsDestroyedTextMulti.text = "0";
            if (enemyDestroyedUnitXText != null) enemyDestroyedUnitXText.text = "0";
            if (enemyDestroyedSeaHunterText != null) enemyDestroyedSeaHunterText.text = "0";
            if (enemyDestroyedDDG51Text != null) enemyDestroyedDDG51Text.text = "0";
            if (damageDealtTextMulti != null) damageDealtTextMulti.text = "0";
            if (damageTakenTextMulti != null) damageTakenTextMulti.text = "0";
            if (winLossTextMulti != null) winLossTextMulti.text = "N/A";
            if (scoreTextMulti != null) scoreTextMulti.text = "0";
            return;
        }

        // Initialize accumulators for grand totals
        float currentYOffset = -50f;
        const float yDecrement = -200f;

        foreach (ScenarioDataMgr.ScenarioData scenarioData in ScenarioDataMgr.inst.scenarioDataList)
        {
            if (scenarioData == null)
            {
                Debug.LogWarning("Encountered a null scenarioData in the list. Skipping.");
                continue;
            }

            GameObject entryInstance = Instantiate(Score, MultiScoreList.transform);
            entryInstance.SetActive(true);
            entryInstance.name = $"ScenarioEntry_{scenarioData.scenarioNumber}";

            // Position entry
            RectTransform entryRect = entryInstance.GetComponent<RectTransform>();
            if (entryRect != null)
            {
                entryRect.anchoredPosition = new Vector2(entryRect.anchoredPosition.x, currentYOffset);
            }
            currentYOffset += yDecrement;
            // Ensure the entry is active
            Transform buttonTransform = FindDeepChild(entryInstance.transform, "EntryScenarioTitleButton");
            buttonTransform.GetComponentInChildren<ScenarioButton>().scenarioNumber = scenarioData.scenarioNumber;
            // Populate entry UI
            SetTextOnChild(entryInstance.transform, "EntryScenarioTitleText", $"Scenario {scenarioData.scenarioNumber}");
            SetTextOnChild(entryInstance.transform, "EntryTotalUnitsText", scenarioData.totalUnits.ToString());
            SetTextOnChild(entryInstance.transform, "EntryTotalUnitXText", scenarioData.totalJARI.ToString());
            SetTextOnChild(entryInstance.transform, "EntryTotalSeaHunterText", scenarioData.totalSeaHunter.ToString());
            SetTextOnChild(entryInstance.transform, "EntryTotalDDG51Text", scenarioData.totalDDG51.ToString());

            SetTextOnChild(entryInstance.transform, "EntryOurUnitsDestroyedText",
                (scenarioData.ourDestroyedJARI + scenarioData.ourDestroyedSeaHunter + scenarioData.ourDestroyedDDG51).ToString());
            SetTextOnChild(entryInstance.transform, "EntryOurUnitXText", scenarioData.ourDestroyedJARI.ToString());
            SetTextOnChild(entryInstance.transform, "EntryOurSeaHunterText", scenarioData.ourDestroyedSeaHunter.ToString());
            SetTextOnChild(entryInstance.transform, "EntryOurDDG51Text", scenarioData.ourDestroyedDDG51.ToString());

            SetTextOnChild(entryInstance.transform, "EntryEnemyUnitsDestroyedText", (scenarioData.enemyDestroyedJARI + scenarioData.enemyDestroyedSeaHunter + scenarioData.enemyDestroyedDDG51).ToString());
            SetTextOnChild(entryInstance.transform, "EntryEnemyUnitXText", scenarioData.enemyDestroyedJARI.ToString());
            SetTextOnChild(entryInstance.transform, "EntryEnemySeaHunterText", scenarioData.enemyDestroyedSeaHunter.ToString());
            SetTextOnChild(entryInstance.transform, "EntryEnemyDDG51Text", scenarioData.enemyDestroyedDDG51.ToString());

            SetTextOnChild(entryInstance.transform, "EntryDamageDealtText", scenarioData.damageDealt.ToString("F0"));
            SetTextOnChild(entryInstance.transform, "EntryDamageTakenText", scenarioData.damageTaken.ToString("F0"));
            SetTextOnChild(entryInstance.transform, "EntryWinLossText", scenarioData.winLoss ? "Win" : "Loss");
            SetTextOnChild(entryInstance.transform, "EntryScoreText", scenarioData.score.ToString("F0"));
            SetTextOnChild(entryInstance.transform, "FeedbackText", scenarioData.feedback);
        }


    }

    private void SetTextOnChild(Transform parent, string childName, string textValue)
    {
        if (parent == null)
        {
            Debug.LogWarning("Parent transform is null.");
            return;
        }

        // Find child by name (supports nested/inactive children)
        Transform child = FindDeepChild(parent, childName);

        if (child != null && child.TryGetComponent<TMP_Text>(out var tmpText))
        {
            tmpText.text = textValue;
            if (!child.gameObject.activeSelf)
                child.gameObject.SetActive(true); // Activate if hidden
        }
        else
        {
            Debug.LogWarning($"Child '{childName}' not found or missing TMP_Text.");
        }
    }
    private Transform FindDeepChild(Transform parent, string childName)
    {
        // Search all children (active and inactive)
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
                return child;
        }
        return null;
    }
    public void OnScoreListClicked()
    {
        lobbyState = LobbyState.MultiScorePanel;
    }
    public void BackButton()
    {
            lobbyState = LobbyState.ScorePanel;
    }
}