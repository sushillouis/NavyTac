using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro;
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
    public float TrainingTime = 15f;


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
    [SerializeField] public Button SkipButton;

    [Header("Replay Panel")]
    [SerializeField] private Button replayExitButton;

    [Header("Lobby State and the rest")]
    [SerializeField] private LobbyState _lobbyState = LobbyState.None;
    public TrainingState currentTrainingState = TrainingState.None;
    [SerializeField] private GameObject NetworkManagerGo;

    private const string LOGIN_CODE_TUTORIAL = "TUT";
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
        if (hostButton != null)
        {
            hostButton.onClick.RemoveAllListeners();
            hostButton.onClick.AddListener(() =>
            {
                SetupIPAddressAndPort();
                NetworkManager.Singleton.StartHost();
                lobbyState = LobbyState.Login;
            });
        }

        if (clientButton != null)
        {
            clientButton.onClick.RemoveAllListeners();
            clientButton.onClick.AddListener(() =>
            {
                SetupIPAddressAndPort();
                NetworkManager.Singleton.StartClient();
                lobbyState = LobbyState.Login;
            });
        }

        if (HostJoinQuitButton != null)
        {
            HostJoinQuitButton.onClick.RemoveAllListeners();
            HostJoinQuitButton.onClick.AddListener(OnQuitButton);
        }

        if (SinglePlayerButton != null)
        {
            SinglePlayerButton.onClick.RemoveAllListeners();
            SinglePlayerButton.onClick.AddListener(OnSinglePlayer);
        }

        if (MultiPlayerButton != null)
        {
            MultiPlayerButton.onClick.RemoveAllListeners();
            MultiPlayerButton.onClick.AddListener(OnMultiPlayer);
        }

        if (SingleMultiQuitButton != null)
        {
            SingleMultiQuitButton.onClick.RemoveAllListeners();
            SingleMultiQuitButton.onClick.AddListener(OnQuitButton);
        }
        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnStartButton);
        }
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(OnResumeButton);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(OnQuitButton);
        }

        if (menuButtons != null)
        {
            foreach (Button button in menuButtons)
            {
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(OnMenuButton);
                }
            }
        }

        if (nextGameButton != null)
        {
            nextGameButton.onClick.RemoveAllListeners();
            nextGameButton.onClick.AddListener(OnNextGameOrFeedbackClicked);
        }

        if (nextGameOrExitButton != null)
        {
            nextGameOrExitButton.onClick.RemoveAllListeners();
            nextGameOrExitButton.onClick.AddListener(OnMultiScorePanelNextOrExitClicked);
        }

        if (replayExitButton != null)
        {
            replayExitButton.onClick.RemoveAllListeners();
            replayExitButton.onClick.AddListener(OnReplayExitClicked);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(BackButton);
        }
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
            return false;
        }

        string rawPlayerName = loginNameInputField.text.Trim();
        string rawPlayerCode = loginCodeInputField.text.Trim();

        if (WrongNameText != null) WrongNameText.gameObject.SetActive(false);
        if (WrongCodeText != null) WrongCodeText.gameObject.SetActive(false);

        Regex playerNameParsingRegex = new Regex(@"^Student\s*(\d+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        Match nameParseMatch = playerNameParsingRegex.Match(rawPlayerName);

        if (!nameParseMatch.Success)
        {
            if (WrongNameText != null)
            {
                WrongNameText.text = INVALID_NAME_FORMAT_MSG;
                StartCoroutine(ShowMessageForDuration(WrongNameText, 10f));
            }
            return false;
        }

        string numberPart = nameParseMatch.Groups[1].Value;
        playerName = "Student" + numberPart;

        if (int.TryParse(numberPart, out int parsedPlayerNo))
        {
            playerNo = parsedPlayerNo;
        }
        else
        {
            playerNo = 0;
        }

        playerCode = rawPlayerCode.ToUpperInvariant();

        switch (playerCode)
        {
            case LOGIN_CODE_ADAPTIVE:
                currentTrainingState = TrainingState.Adaptive;
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
            case LOGIN_CODE_TUTORIAL:
                currentTrainingState = TrainingState.Tutorial;
                break;
            default:
                if (WrongCodeText != null)
                {
                    WrongCodeText.text = INVALID_LOGIN_CODE_MSG;
                    StartCoroutine(ShowMessageForDuration(WrongCodeText, 10f));
                }
                return false;
        }

        if (GameMgr.inst != null)
        {
            Random.InitState(GameMgr.inst.GetSelectedSeed());
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
            case TrainingState.Tutorial:
                name = "Objective";
                description = "Tutorial: Learn the basics of the game. Follow the instructions.";
                break;
            default:
                name = "Objective";
                description = "Destroy enemy base and entities while protecting your own. Attack!";
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

            if (value == LobbyState.MultiScorePanel)
            {
                UpdateMultiScorePanelButtonTexts();
                if (previousState != LobbyState.Replay)
                {
                    UpdateMultiScoreDisplay();
                }
            }

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
            if (IsDebugging) Debug.Log($"Play session ended. Duration: {playSessionDuration:F2}s. Total Training Time: {totalTrainingTime / 60f:F2}m.", this);
            totalPlayTime = 0f;

            gamesPlayedCount++;
            UpdateScorePanelButtonTexts();
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
        if (Time.timeScale == 0f)
        {
            totalTrainingTime += Time.unscaledDeltaTime;
        }
        else
        {
            totalTrainingTime += Time.deltaTime;
        }
    }

    public void OnStartButton()
    {
        totalPlayTime = 0f;
        Time.timeScale = 1;
        GameMgr.inst.OpenOcean1x1();
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
        if (currentTrainingState == TrainingState.NonAdaptive || currentTrainingState == TrainingState.Adaptive)
        {
            Debug.Log(totalTrainingTime + " >= " + TrainingTime * 60f + " ? " + (totalTrainingTime >= TrainingTime * 60f));
            return totalTrainingTime >= TrainingTime * 60f;
        }
        return gamesPlayedCount >= gamePlayCountMAX;
    }

    // Button in Score Panel to either start next game or provide feedback
    public void OnNextGameOrFeedbackClicked()
    {
        if (ReplayMgr.inst != null) ReplayMgr.inst.CompleteScenario();

        // If currentTrainingState is Tutorial, quit
        if (currentTrainingState == TrainingState.Tutorial)
        {
            OnQuitButton();
        }
        else if (currentTrainingState == TrainingState.Adaptive || currentTrainingState == TrainingState.NonAdaptive)
        {
            lobbyState = LobbyState.MultiScorePanel;
        }
        else if ((currentTrainingState == TrainingState.PreTest || currentTrainingState == TrainingState.PostTest) && ShouldEndSession())
        {
            OnQuitButton();
        }
        else
        {
            StartNextGameSession();
        }
    }
    private bool hasReplayedOnce = false; // Track if a replay has already occurred
    public void OnMultiScorePanelNextOrExitClicked()
    {
        UpdateMultiScorePanelButtonTexts();
        float lastScore = ScoreMgr.inst.LastScenarioScore();

        // Enforce one replay if score < 60 and in Adaptive mode
        if (lastScore < 60f && currentTrainingState == TrainingState.Adaptive && !hasReplayedOnce)
        {
            hasReplayedOnce = true;
            if (IsDebugging) Debug.Log("Score < 60 in Adaptive. Forcing replay.", this);
            ReplayCurrentScenario();
            return;
        }

        else if (ShouldEndSession())
        {
            if (IsDebugging) Debug.Log($"Session ended. Exiting. State: {currentTrainingState}, Games: {gamesPlayedCount}, Time: {totalTrainingTime / 60f:F2}m", this);
            OnQuitButton();
        }
        else
        {
            if (IsDebugging) Debug.Log($"Proceeding to next game from MultiScorePanel. State: {currentTrainingState}, Games: {gamesPlayedCount}, Time: {totalTrainingTime / 60f:F2}m", this);
            hasReplayedOnce = false; // Reset for next scenario
            StartNextGameSession();
        }
    }


    private void UpdateScorePanelButtonTexts()
    {
        // This method relies on a private class field `_isMandatoryReplayPending`.

        TMP_Text singleScoreButtonText = nextGameButton.GetComponentInChildren<TMP_Text>();
        if (singleScoreButtonText != null)
        {
            if (currentTrainingState == TrainingState.Adaptive || currentTrainingState == TrainingState.NonAdaptive)
            {
                singleScoreButtonText.text = SCORE_PANEL_TEXT_FEEDBACK;
            }
            else if ((currentTrainingState == TrainingState.PreTest || currentTrainingState == TrainingState.PostTest) && ShouldEndSession())
            {
                singleScoreButtonText.text = EXIT_BUTTON_TEXT;
            }
            else if (currentTrainingState == TrainingState.Tutorial)
            {
                singleScoreButtonText.text = EXIT_BUTTON_TEXT;
            }
            else
            {
                singleScoreButtonText.text = NEXT_GAME_BUTTON_TEXT;
            }
        }


    }
    private void UpdateMultiScorePanelButtonTexts()
    {
        TMP_Text multiScoreButtonText = nextGameOrExitButton.GetComponentInChildren<TMP_Text>();
        if (multiScoreButtonText != null)
        {
            if (ScoreMgr.inst.LastScenarioScore() < 60f && currentTrainingState == TrainingState.Adaptive && !hasReplayedOnce)
            {
                multiScoreButtonText.text = "Replay";
            }
            else if (ShouldEndSession())
            {
                multiScoreButtonText.text = EXIT_BUTTON_TEXT;
            }
            else
            {
                multiScoreButtonText.text = NEXT_GAME_BUTTON_TEXT;
            }
        }
    }


    private void UpdateMultiScoreDisplay()
    {
        if (MultiScoreList == null)
        {
            return;
        }
        if (Score == null)
        {
            return;
        }
        foreach (Transform child in MultiScoreList.transform)
        {
            child.gameObject.SetActive(false);
        }

        float currentYOffset = -50f;
        const float yDecrement = -200f;

        foreach (ScenarioDataMgr.ScenarioData scenarioData in ScenarioDataMgr.inst.scenarioDataList)
        {
            if (scenarioData == null)
            {
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
    }
    public void ReplayCurrentScenario()
    {
        if (ReplayMgr.inst != null)
        {
            lobbyState = LobbyState.Replay;
            ReplayMgr.inst.ReplayLastScenario();
        }
    }
    public void ResetGameState()
    {
        lobbyState = LobbyState.Play;
    }
}
