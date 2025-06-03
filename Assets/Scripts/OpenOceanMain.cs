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
using System.Net;

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
    [SerializeField] private PanelPlus loginPanel;
    [SerializeField] private PanelPlus mapSelectPanel;
    [SerializeField] private PanelPlus HostOrJoinPanel;
    [SerializeField] private PanelPlus MainGamePanel;
    [SerializeField] private PanelPlus SingleMultiplayerPanel;
    [SerializeField] private PanelPlus ScorePanel;
    [SerializeField] private RectTransform NetDebugConsolePanel;
    [SerializeField] private PanelPlus GamePausePanel;
    [SerializeField] private PanelPlus MultiScorePanel;
    [SerializeField] private PanelPlus ReplayPanel;

    [Header("Single / Multi player Screen")]
    [SerializeField] private Button SinglePlayerButton;
    [SerializeField] private Button MultiPlayerButton;
    [SerializeField] private Button SingleMultiQuitButton;

    [Header("Host / Join Screen")]
    public TMP_InputField ipAddressInputField;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private Button HostJoinQuitButton;

    [Header("Login Screen")]
    public TMP_InputField loginNameInputField;
    [SerializeField] public TMP_InputField loginCodeInputField;
    [SerializeField] public TMP_Text WrongCodeText;
    [SerializeField] public TMP_Text WrongNameText;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button LoginQuitButton;

    [Header("Map Select Screen")]
    [SerializeField] public TMP_Text mapNameText;
    [SerializeField] public TMP_Text mapDescriptionText;
    [SerializeField] private Button startButton;

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
    private const string SCORE_PANEL_TEXT_FEEDBACK = "Feedback";
    private const string NEXT_GAME_BUTTON_TEXT = "Next";
    private const string EXIT_BUTTON_TEXT = "Exit";

    public float playSessionDuration { get; private set; }

    [Header("Game Pause Panel")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private List<Button> menuButtons;

    [Header("Replay Panel")]
    [SerializeField] private Button replayExitButton;

    [Header("Lobby State and the rest")]
    [SerializeField] private LobbyState _lobbyState = LobbyState.None;
    public TrainingState currentTrainingState = TrainingState.None;
    [SerializeField] private GameObject NetworkManagerGo;


    private const string LOGIN_CODE_ADAPTIVE = "AAA";
    private const string LOGIN_CODE_NON_ADAPTIVE = "BBB";
    private const string LOGIN_CODE_PRE_TEST = "ABC";
    private const string LOGIN_CODE_POST_TEST = "XYZ";
    private const string INVALID_NAME_FORMAT_MSG = "Invalid name format. Expected 'Student <number>'.";
    private const string INVALID_LOGIN_CODE_MSG = "Invalid login code.";
    private static readonly Regex PlayerNameRegex = new Regex(@"^Student\s*\d+$", RegexOptions.Compiled);


    private void Awake()
    {
        inst = this;
        SetupButtonListeners();
    }

    private void SetupButtonListeners()
    {
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
        nextGameButton.onClick.AddListener(OnNextGameOrFeedbackClicked);


        nextGameOrExitButton.onClick.RemoveAllListeners();
        nextGameOrExitButton.onClick.AddListener(OnMultiScorePanelNextOrExitClicked);

        replayExitButton.onClick.RemoveAllListeners();
        replayExitButton.onClick.AddListener(OnReplayExitClicked);

        backButton.onClick.RemoveAllListeners();
        backButton.onClick.AddListener(BackButton);
    }


    void SetupIPAddressAndPort()
    {
        if (ipAddressInputField == null)
        {
            Debug.LogError("ipAddressInputField is not assigned. Using default IP.", this);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(ipAddress, port);
            return;
        }

        string trimmedIp = ipAddressInputField.text.Trim();
        if (IPAddress.TryParse(trimmedIp, out _))
        {
            ipAddress = trimmedIp;
        }
        else if (!string.IsNullOrWhiteSpace(trimmedIp))
        {
            Debug.LogWarning($"Invalid IP address format: '{trimmedIp}'. Using default IP: {ipAddress}", this);

        }

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

        if (!PlayerNameRegex.IsMatch(playerName))
        {
            if (WrongNameText != null)
            {
                WrongNameText.text = INVALID_NAME_FORMAT_MSG;
                StartCoroutine(ShowMessageForDuration(WrongNameText, 10f));
            }
            return false;
        }

        switch (playerCode)
        {
            case LOGIN_CODE_ADAPTIVE:
                currentTrainingState = TrainingState.Adaptive;
                if (GameMgr.inst != null) GameMgr.inst.difficultyLevel = 0.2f;
                break;
            case LOGIN_CODE_NON_ADAPTIVE:
                currentTrainingState = TrainingState.NonAdaptive;
                break;
            case LOGIN_CODE_PRE_TEST:
                currentTrainingState = TrainingState.PreTest;
                break;
            case LOGIN_CODE_POST_TEST:
                currentTrainingState = TrainingState.PostTest;
                break;
            default:
                if (WrongCodeText != null)
                {
                    WrongCodeText.text = INVALID_LOGIN_CODE_MSG;
                    StartCoroutine(ShowMessageForDuration(WrongCodeText, 10f));
                }
                return false;
        }

        Match numberMatch = Regex.Match(playerName, @"\d+$");
        if (numberMatch.Success && int.TryParse(numberMatch.Value, out int parsedPlayerNo))
        {
            playerNo = parsedPlayerNo;
        }
        else
        {
            if (IsDebugging) Debug.LogWarning($"Could not parse player number from name: '{playerName}'. Defaulting to 0.", this);
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
                if (isSinglePlayer) SinglePlayerSetup();
                else NetPlayersSetup();
            }
            else
            {
                Debug.LogError("Login failed. Check input fields or logs.", this);
            }
        });

        lobbyState = IsDebugging ? LobbyState.Login : LobbyState.SingleMultiPlayer;
    }

    void NetPlayersSetup()
    {
        if (IsDebugging) Debug.Log("Setting up network multiplayer ...", this);
        foreach (NetSetup ns in FindObjectsOfType<NetSetup>())
        {
            if (ns.GetComponent<NetworkObject>().IsLocalPlayer)
            {
                localNetSetup = ns;
                localNetSetupNetworkObject = ns.GetComponent<NetworkObject>();
                break;
            }
        }
        foreach (TactNetMgr tnm in FindObjectsOfType<TactNetMgr>())
        {
            if (tnm.GetComponent<NetworkObject>().IsLocalPlayer)
            {
                localTactNetMgr = tnm;
                localTactNetMgrNetworkObject = tnm.GetComponent<NetworkObject>();
                break;
            }
        }
    }

    void SinglePlayerSetup()
    {
        if (IsDebugging) Debug.Log("Setting up single player ...", this);
        TactPlayer localPlayer = PlayerMgr.inst.CreateSinglePlayer(playerName);
        PlayerMgr.inst.AddPlayer(localPlayer);
        PlayerMgr.inst.localPlayer = localPlayer;
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
            default:
                name = "Map Selection Pending";
                description = "Please complete login to determine training type.";
                Debug.LogWarning("UpdateMapSelectionUI called with TrainingState.None or unhandled state. Login might not be complete or code is invalid.", this);
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
            if (_lobbyState == value) return;

            LobbyState previousState = _lobbyState;
            _lobbyState = value;

            UpdatePanelVisibility(value);

            if (value == LobbyState.MapSelect)
            {
                UpdateMapSelectionUI();
            }

            HandleTimeScale(value, previousState);
            HandleScorePanelLogic(value, previousState);

            if (UIMgr.inst != null) UIMgr.inst.gameObject.SetActive(value == LobbyState.Play);
            if (GroupUIMgr.inst != null) GroupUIMgr.inst.gameObject.SetActive(value == LobbyState.Play);

            if (value == LobbyState.MultiScorePanel && previousState != LobbyState.Replay)
            {
                UpdateMultiScoreDisplay();
                
            }

            if (IsDebugging) Debug.Log($"Lobby state changed from {previousState} to {value}.", this);
            
        }
    }

    private void UpdatePanelVisibility(LobbyState currentLobbyState)
    {
        loginPanel.isVisible = (currentLobbyState == LobbyState.Login);
        mapSelectPanel.isVisible = (currentLobbyState == LobbyState.MapSelect);
        HostOrJoinPanel.isVisible = (currentLobbyState == LobbyState.HostOrJoin);
        MainGamePanel.isVisible = (currentLobbyState == LobbyState.Play);
        NetDebugConsolePanel.gameObject.SetActive(IsDebugging && IsNetDebugging);
        SingleMultiplayerPanel.isVisible = (currentLobbyState == LobbyState.SingleMultiPlayer);
        ScorePanel.isVisible = (currentLobbyState == LobbyState.ScorePanel);
        GamePausePanel.isVisible = (currentLobbyState == LobbyState.GamePaused);
        MultiScorePanel.isVisible = (currentLobbyState == LobbyState.MultiScorePanel);
        ReplayPanel.isVisible = (currentLobbyState == LobbyState.Replay);
    }

    private void HandleTimeScale(LobbyState currentLobbyState, LobbyState previousState)
    {
        if (currentLobbyState == LobbyState.Play)
        {
            if (previousState == LobbyState.GamePaused)
            {
                Time.timeScale = savedTimeScale;
            }


            else if (Time.timeScale == 0f)
            {
                Time.timeScale = 1f;
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
    }

    private void HandleScorePanelLogic(LobbyState currentLobbyState, LobbyState previousState)
    {
        if (currentLobbyState == LobbyState.ScorePanel && previousState == LobbyState.Play)
        {
            playSessionDuration = totalPlayTime;
            totalTrainingTime += playSessionDuration;
            if (IsDebugging) Debug.Log($"Play session ended. Duration: {playSessionDuration:F2}s. Total Training Time: {totalTrainingTime / 60f:F2}m.", this);
            totalPlayTime = 0f;

            gamesPlayedCount++;
            UpdateScorePanelButtonTexts();
        }
    }

    private void UpdateScorePanelButtonTexts()
    {

        TMP_Text singleScoreButtonText = nextGameButton.GetComponentInChildren<TMP_Text>();
        if (singleScoreButtonText != null)
        {
            if (currentTrainingState == TrainingState.Adaptive)
            {
                singleScoreButtonText.text = SCORE_PANEL_TEXT_FEEDBACK;
            }
            else if (currentTrainingState == TrainingState.NonAdaptive && ShouldEndSession())
            {
                singleScoreButtonText.text = SCORE_PANEL_TEXT_FEEDBACK;
            }
            else if ((currentTrainingState == TrainingState.PreTest || currentTrainingState == TrainingState.PostTest)&& ShouldEndSession())
            {
                singleScoreButtonText.text = EXIT_BUTTON_TEXT;
            }
            else
            {
                singleScoreButtonText.text = NEXT_GAME_BUTTON_TEXT;
            }
        }


        TMP_Text multiScoreButtonText = nextGameOrExitButton.GetComponentInChildren<TMP_Text>();
        if (multiScoreButtonText != null)
        {
            multiScoreButtonText.text = ShouldEndSession() ? EXIT_BUTTON_TEXT : NEXT_GAME_BUTTON_TEXT;
        }
    }


    void Update()
    {
        if (lobbyState == LobbyState.Play)
        {
            totalPlayTime += Time.unscaledDeltaTime;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (lobbyState == LobbyState.Play) lobbyState = LobbyState.GamePaused;
            else if (lobbyState == LobbyState.GamePaused) lobbyState = LobbyState.Play;
        }
    }

    public void OnMapSelected()
    {
        totalPlayTime = 0f;


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
            Debug.LogError("Cannot start map: Not single player and localNetSetup is null.");
            lobbyState = LobbyState.MapSelect;
            return;
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
        if (TactNetMgr.inst != null) TactNetMgr.inst.TactNetShutdown();
        if (NetworkManager.Singleton != null) NetworkManager.Singleton.Shutdown();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    public void OnResumeButton() => lobbyState = LobbyState.Play;
    public void OnMenuButton() => lobbyState = LobbyState.GamePaused;

    private void StartNextGameSession()
    {
        if (IsDebugging) Debug.Log($"Starting next game. Current state: {currentTrainingState}, Games: {gamesPlayedCount}/{gamePlayCountMAX}, Time: {totalTrainingTime / 60f:F2}m/{nonAdaptiveTrainingTime}m.", this);





        if (ResetScene.inst != null)
        {
            ResetScene.inst.ReloadScene();






        }
        else
        {
            Debug.LogError("ResetScene.inst is null. Cannot reload scene.");

            lobbyState = LobbyState.SingleMultiPlayer;
        }
    }

    private bool ShouldEndSession()
    {
        if (currentTrainingState == TrainingState.NonAdaptive)
        {
            return totalTrainingTime >= nonAdaptiveTrainingTime * 60f;
        }

        return gamesPlayedCount >= gamePlayCountMAX;
    }


    public void OnNextGameOrFeedbackClicked()
    {
        if (ReplayMgr.inst != null) ReplayMgr.inst.CompleteScenario();

        if (currentTrainingState == TrainingState.Adaptive)
        {
            if (IsDebugging) Debug.Log($"Proceeding to MultiScorePanel. Adaptive: {currentTrainingState == TrainingState.Adaptive}, ShouldEnd: {ShouldEndSession()}", this);
            lobbyState = LobbyState.MultiScorePanel;
        }
        else if (currentTrainingState == TrainingState.NonAdaptive && ShouldEndSession())
        {
            if (IsDebugging) Debug.Log($"Ending session. Non-Adaptive: {currentTrainingState == TrainingState.NonAdaptive}, ShouldEnd: {ShouldEndSession()}", this);
            lobbyState = LobbyState.MultiScorePanel;
        }
        else if ((currentTrainingState == TrainingState.PreTest || currentTrainingState == TrainingState.PostTest) && ShouldEndSession())
        {
            OnQuitButton();
        }
        else
        {

            if (IsDebugging) Debug.Log("Starting next game session directly.", this);
            StartNextGameSession();
        }
    }


    public void OnMultiScorePanelNextOrExitClicked()
    {
        if (ShouldEndSession())
        {
            if (IsDebugging) Debug.Log($"Session ended. Exiting. State: {currentTrainingState}, Games: {gamesPlayedCount}, Time: {totalTrainingTime / 60f:F2}m", this);
            OnQuitButton();
        }
        else
        {
            if (IsDebugging) Debug.Log($"Proceeding to next game from MultiScorePanel. State: {currentTrainingState}, Games: {gamesPlayedCount}, Time: {totalTrainingTime / 60f:F2}m", this);
            StartNextGameSession();
        }
    }


    private void ClearMultiScoreSummaryFields()
    {


        if (scenarioNumberText != null) scenarioNumberText.text = "0";
        if (totalUnitsText != null) totalUnitsText.text = "N/A";
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
    }

    private void UpdateMultiScoreDisplay()
    {
        if (MultiScoreList == null)
        {
            Debug.LogError("MultiScoreList GameObject (container) is not assigned.", this);
            return;
        }
        if (Score == null)
        {
            Debug.LogError("Score prefab/template is not assigned.", this);
            return;
        }


        foreach (Transform child in MultiScoreList.transform)
        {

            child.gameObject.SetActive(false);
        }

        if (ScenarioDataMgr.inst == null || ScenarioDataMgr.inst.scenarioDataList == null || ScenarioDataMgr.inst.scenarioDataList.Count == 0)
        {
            Debug.LogWarning("ScenarioDataMgr instance, list is null, or empty. Cannot display multi-score data.", this);
            ClearMultiScoreSummaryFields();

            return;
        }





        float currentYOffset = -50f;
        const float yDecrement = -200f;

        foreach (ScenarioDataMgr.ScenarioData scenarioData in ScenarioDataMgr.inst.scenarioDataList)
        {
            if (scenarioData == null)
            {
                Debug.LogWarning("Encountered a null scenarioData in the list. Skipping.", this);
                continue;
            }

            GameObject entryInstance = Instantiate(Score, MultiScoreList.transform);
            entryInstance.SetActive(true);
            entryInstance.name = $"ScenarioEntry_{scenarioData.scenarioNumber}";

            if (entryInstance.TryGetComponent<RectTransform>(out var entryRect))
            {
                entryRect.anchoredPosition = new Vector2(entryRect.anchoredPosition.x, currentYOffset);
            }
            currentYOffset += yDecrement;

            Transform buttonTransform = FindDeepChild(entryInstance.transform, "EntryScenarioTitleButton");
            if (buttonTransform != null && buttonTransform.TryGetComponent<ScenarioButton>(out var scenarioButton))
            {
                scenarioButton.scenarioNumber = scenarioData.scenarioNumber;
            }
            else
            {
                Debug.LogWarning($"Could not find ScenarioButton on 'EntryScenarioTitleButton' for scenario {scenarioData.scenarioNumber}", this);
            }

            SetTextOnChild(entryInstance.transform, "EntryScenarioTitleText", $"Scenario {scenarioData.scenarioNumber}");
            SetTextOnChild(entryInstance.transform, "EntryTotalUnitsText", scenarioData.totalUnits.ToString());
            SetTextOnChild(entryInstance.transform, "EntryTotalUnitXText", scenarioData.totalJARI.ToString());
            SetTextOnChild(entryInstance.transform, "EntryTotalSeaHunterText", scenarioData.totalSeaHunter.ToString());
            SetTextOnChild(entryInstance.transform, "EntryTotalDDG51Text", scenarioData.totalDDG51.ToString());

            int ourTotalDestroyed = scenarioData.ourDestroyedJARI + scenarioData.ourDestroyedSeaHunter + scenarioData.ourDestroyedDDG51;
            SetTextOnChild(entryInstance.transform, "EntryOurUnitsDestroyedText", ourTotalDestroyed.ToString());
            SetTextOnChild(entryInstance.transform, "EntryOurUnitXText", scenarioData.ourDestroyedJARI.ToString());
            SetTextOnChild(entryInstance.transform, "EntryOurSeaHunterText", scenarioData.ourDestroyedSeaHunter.ToString());
            SetTextOnChild(entryInstance.transform, "EntryOurDDG51Text", scenarioData.ourDestroyedDDG51.ToString());

            int enemyTotalDestroyed = scenarioData.enemyDestroyedJARI + scenarioData.enemyDestroyedSeaHunter + scenarioData.enemyDestroyedDDG51;
            SetTextOnChild(entryInstance.transform, "EntryEnemyUnitsDestroyedText", enemyTotalDestroyed.ToString());
            SetTextOnChild(entryInstance.transform, "EntryEnemyUnitXText", scenarioData.enemyDestroyedJARI.ToString());
            SetTextOnChild(entryInstance.transform, "EntryEnemySeaHunterText", scenarioData.enemyDestroyedSeaHunter.ToString());
            SetTextOnChild(entryInstance.transform, "EntryEnemyDDG51Text", scenarioData.enemyDestroyedDDG51.ToString());

            SetTextOnChild(entryInstance.transform, "EntryDamageDealtText", scenarioData.damageDealt.ToString("F0"));
            SetTextOnChild(entryInstance.transform, "EntryDamageTakenText", scenarioData.damageTaken.ToString("F0"));
            SetTextOnChild(entryInstance.transform, "EntryWinLossText", scenarioData.winLoss ? "Win" : "Loss");
            SetTextOnChild(entryInstance.transform, "EntryScoreText", scenarioData.score.ToString("F0") + "%");
            SetTextOnChild(entryInstance.transform, "FeedbackText", scenarioData.feedback);
        }
    }

    private void SetTextOnChild(Transform parent, string childName, string textValue)
    {
        if (parent == null)
        {
            Debug.LogWarning($"SetTextOnChild: Parent transform is null when trying to find '{childName}'.", this);
            return;
        }
        Transform child = FindDeepChild(parent, childName);
        if (child != null && child.TryGetComponent<TMP_Text>(out var tmpText))
        {
            tmpText.text = textValue;
        }
        else
        {
            Debug.LogWarning($"SetTextOnChild: Child '{childName}' not found under '{parent.name}' or missing TMP_Text component.", this);
        }
    }

    private Transform FindDeepChild(Transform parent, string childName)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName) return child;
        }
        return null;
    }

    public void OnReplayExitClicked()
    {
        if (ReplayMgr.inst != null)
        {
            ReplayMgr.inst.StopReplay();
            
        }
        else
        {
            Debug.LogError("ReplayMgr.inst is null. Cannot stop replay.", this);
            lobbyState = LobbyState.MultiScorePanel;
        }
    }
    public void BackButton() => lobbyState = LobbyState.ScorePanel;

    public void OnScenarioSelected(int scenarioNumber)
    {
        if (ReplayMgr.inst != null)
        {
            lobbyState = LobbyState.Replay;
            ReplayMgr.inst.StartReplay(scenarioNumber);
        }
        else
        {
            Debug.LogError("ReplayMgr.inst is null. Cannot start replay.", this);
        }
    }
    public void ResetGameState()
    {
        lobbyState = LobbyState.Play;
    }
}
