using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class ScoreMgr : MonoBehaviour
{
    public static ScoreMgr inst; 
    public float damageDealt; 
    public float damageTaken; 
    public bool playerWon; 
    public float score; 
    public bool aiWon; 
    public String winReason; 
    public List<float> playerScores = new List<float>(); 

    private string sessionStartTimeString; 
    private const string CommonLogFileName = "AllGamesLog.csv"; 

    [SerializeField] private GeminiRtsFeedback geminiFeedback;

    // Unified API endpoint for both S3 and DynamoDB
    private const string API_URL = "https://1nht1d5r0h.execute-api.us-east-2.amazonaws.com/default/UnityS3Upload";

    [System.Serializable]
    public class FeedbackData
    {
        public List<string> generalFeedbacks;
        public VictoryFeedbacks victoryFeedbacks;
        public List<string> defeatFeedbacks;
        public List<string> baseDestroyedFeedbacks;
    }

    [System.Serializable]
    private class UploadPayload
    {
        public string filename;
        public string content;
        public bool base64;
    }

    [System.Serializable]
    public class VictoryFeedbacks
    {
        public List<string> highScore;
        public List<string> mediumScore;
        public List<string> lowScore;
        public List<string> allDestroyed;
    }

    private FeedbackData feedbackData;

    private void Awake()
    {
        if (inst != null && inst != this)
        {
            Destroy(gameObject); 
        }
        else
        {
            inst = this; 
            sessionStartTimeString = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm");
            LoadFeedbackData();
            if (geminiFeedback == null)
            {
                geminiFeedback = FindObjectOfType<GeminiRtsFeedback>();
            }
        }
    }

    private void LoadFeedbackData()
    {
        try
        {
            TextAsset feedbackJson = Resources.Load<TextAsset>("Scoring");
            if (feedbackJson != null)
            {
                feedbackData = JsonUtility.FromJson<FeedbackData>(feedbackJson.text);
            }
            else
            {
                CreateFallbackFeedbackData();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading feedback data: {e.Message}. Using fallback data.");
            CreateFallbackFeedbackData();
        }
    }

    private void CreateFallbackFeedbackData()
    {
        feedbackData = new FeedbackData
        {
            generalFeedbacks = new List<string>
            {
                "JARI USVs can force the enemy to reveal positions — use them early.",
                "Attack-Move (A + Right Click) prevents surprise deaths.",
                "Use terrain and spacing to avoid ambushes."
            },
            victoryFeedbacks = new VictoryFeedbacks
            {
                highScore = new List<string> { "Excellent performance!" },
                mediumScore = new List<string> { "Good job, but room for improvement." },
                lowScore = new List<string> { "Focus on unit preservation." },
                allDestroyed = new List<string> { "Try to minimize damage next time." }
            },
            defeatFeedbacks = new List<string> { "Analyze your strategy and try again." },
            baseDestroyedFeedbacks = new List<string> { "Protect your base better." }
        };
    }

    public void CheckVictory()
    {
        if (OpenOceanMain.inst.lobbyState == LobbyState.Replay)
            return;
        if (ReplayMgr.inst != null && ReplayMgr.inst.isReplaying)
            return;
        if (OpenOceanMain.inst.currentTrainingState == TrainingState.Tutorial)
        {
            playerWon = true; 
            aiWon = false; 
            damageDealt = 1000; 
            damageTaken = 0; 
            winReason = "Tutorial Completed"; 
        }
        else if (ScenarioGenerator.inst == null || ScenarioGenerator.inst.entityQuantities == null || EntityMgr.inst == null || EntityMgr.inst.entities == null)
        {
            Debug.LogWarning("GameMgr or EntityMgr is not initialized properly.");
            return;
        }
        if (!playerWon && !aiWon) return;

        if (!(ReplayMgr.inst != null && ReplayMgr.inst.isReplaying))
        {
            OpenOceanMain.inst.lobbyState = LobbyState.ScorePanel;
        }

        score = (float)(0.3 * (playerWon ? 1 : 0)) * 100 + 0.7f * (damageDealt / (damageDealt + damageTaken)) * 100;
        if (ReplayMgr.inst != null && ReplayMgr.inst.isRecording && !ReplayMgr.inst.isReplaying)
        {
            playerScores.Add(score); 
        }
        if (OpenOceanMain.inst.lobbyState != LobbyState.Replay && !(ReplayMgr.inst != null && ReplayMgr.inst.isReplaying))
        {
            ScenarioDataMgr.ScenarioData data = new ScenarioDataMgr.ScenarioData();
            data.scenarioNumber = OpenOceanMain.inst.gamesPlayedCount;
            data.totalUnits = ScenarioGenerator.inst.entityQuantities.Sum(eq => eq.unitCount); 

            Dictionary<EntityType, int> initialCounts = ScenarioGenerator.inst.entityQuantities.ToDictionary(eq => eq.entityType, eq => eq.unitCount);
            data.totalJARI = initialCounts.GetValueOrDefault(EntityType.JARIUSV, 0);
            data.totalSeaHunter = initialCounts.GetValueOrDefault(EntityType.SeaHunter, 0);
            data.totalDDG51 = initialCounts.GetValueOrDefault(EntityType.DDG51, 0);

            Dictionary<EntityType, int> destroyedPlayer = GetDestroyedUnits(PlayerMgr.inst.localPlayer);
            Dictionary<EntityType, int> destroyedAI = GetDestroyedUnits(PlayerMgr.inst.player2);
            data.ourUnitsDestroyed = destroyedPlayer.Values.Sum(); 
            data.ourDestroyedJARI = destroyedPlayer.GetValueOrDefault(EntityType.JARIUSV, 0);
            data.ourDestroyedSeaHunter = destroyedPlayer.GetValueOrDefault(EntityType.SeaHunter, 0);
            data.ourDestroyedDDG51 = destroyedPlayer.GetValueOrDefault(EntityType.DDG51, 0);
            data.enemyUnitsDestroyed = destroyedAI.Values.Sum(); 
            data.enemyDestroyedJARI = destroyedAI.GetValueOrDefault(EntityType.JARIUSV, 0);
            data.enemyDestroyedSeaHunter = destroyedAI.GetValueOrDefault(EntityType.SeaHunter, 0);
            data.enemyDestroyedDDG51 = destroyedAI.GetValueOrDefault(EntityType.DDG51, 0);

            data.damageTaken = damageTaken;
            data.damageDealt = damageDealt;
            data.winLoss = playerWon;
            data.winReason = winReason; 
            data.score = score;
            data.feedback = GetFeedback();

            ScenarioDataMgr.inst.scenarioDataList.Add(data);
            Debug.Log($"Game data for scenario {data.scenarioNumber} logged successfully.");
            LogGameData(); 
            UpdateScoreDisplay(); 
            LogVictoryMessage(); 
            FXMgr.inst.ResetEffects();
            GameMgr.inst.StoreCurrentScenario();

            GenerateDynamicFeedbackForScenario(data);
        }
    }

    private void UpdateScoreDisplay()
    {
        if (OpenOceanMain.inst.damageDealtText != null)
            OpenOceanMain.inst.damageDealtText.text = $"{damageDealt:0}";

        if (OpenOceanMain.inst.damageTakenText != null)
            OpenOceanMain.inst.damageTakenText.text = $"{damageTaken:0}";

        if (OpenOceanMain.inst.winnerText != null)
        {
            if (playerWon)
            {
                OpenOceanMain.inst.winnerText.text = "Victory!";
                OpenOceanMain.inst.winnerText.color = Color.green;
            }
            else
            {
                OpenOceanMain.inst.winnerText.text = "Defeat!";
                OpenOceanMain.inst.winnerText.color = Color.red;
            }
        }

        if (OpenOceanMain.inst.scoreText != null)
            OpenOceanMain.inst.scoreText.text = $"{score:0.##}%";

        if (OpenOceanMain.inst.ourUnitsDestroyedText != null)
            OpenOceanMain.inst.ourUnitsDestroyedText.text = $"{GetDestroyedUnits(PlayerMgr.inst.localPlayer).Values.Sum()}";

        if (OpenOceanMain.inst.enemyUnitsDestroyedText != null)
            OpenOceanMain.inst.enemyUnitsDestroyedText.text = $"{GetDestroyedUnits(PlayerMgr.inst.player2).Values.Sum()}";

        if (OpenOceanMain.inst.winConditionText != null)
            OpenOceanMain.inst.winConditionText.text = winReason;

        if (OpenOceanMain.inst.feedbackText != null)
        {
            OpenOceanMain.inst.feedbackText.text = ""; 
        }
    }

    // ───────────────────────── Dynamic AI Feedback ─────────────────────────

    private void GenerateDynamicFeedbackForScenario(ScenarioDataMgr.ScenarioData data)
    {
        if (data == null) return;

        data.isGeminiFeedbackReady = false;
        EnsureFallbackFeedback(data);

        if (!ShouldUseGeminiFeedback() || geminiFeedback == null)
        {
            data.isGeminiFeedbackReady = true;
            OpenOceanMain.inst?.SetMultiScoreLoading(false);
            UpdateScorePanelFeedback(data.feedback);
            RefreshMultiScorePanelIfVisible();
            return;
        }

        StartCoroutine(GenerateDynamicFeedbackCoroutine(data));
    }

    private IEnumerator GenerateDynamicFeedbackCoroutine(ScenarioDataMgr.ScenarioData data)
    {
        OpenOceanMain.inst?.SetMultiScoreLoading(true);

        string csvData = BuildCsvForCurrentScenario();
        string jsonData = string.Empty;

#if !UNITY_WEBGL || UNITY_EDITOR
        if (ReplayMgr.inst != null)
        {
            string path = ReplayMgr.inst.GetLatestReplayFilePath(OpenOceanMain.inst.gamesPlayedCount);
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                jsonData = File.ReadAllText(path);
            }
        }
#endif

        if (string.IsNullOrWhiteSpace(jsonData))
        {
            jsonData = "{\"commands\":[],\"snapshots\":[]}";
        }

        if (string.IsNullOrWhiteSpace(csvData))
        {
            Debug.Log("Skipping AI feedback: missing CSV data.");
            data.isGeminiFeedbackReady = true;
            OpenOceanMain.inst?.SetMultiScoreLoading(false);
            UpdateScorePanelFeedback(data.feedback);
            RefreshMultiScorePanelIfVisible();
            yield break;
        }

        UpdateScorePanelFeedback("Generating AI feedback...");

        string feedback = null;
        bool done = false;

        RunFeedbackAsync(jsonData, csvData, (result, ex) =>
        {
            if (ex != null)
            {
                Debug.LogError($"Dynamic feedback generation failed: {ex.Message}");
            }
            else
            {
                feedback = result;
            }
            done = true;
        });

        while (!done)
        {
            yield return null;
        }

        if (!string.IsNullOrWhiteSpace(feedback))
        {
            data.feedback = feedback;
        }
        else
        {
            EnsureFallbackFeedback(data);
        }

        data.isGeminiFeedbackReady = true;
        OpenOceanMain.inst?.SetMultiScoreLoading(false);
        UpdateScorePanelFeedback(data.feedback);
        RefreshMultiScorePanelIfVisible();
    }

    private async void RunFeedbackAsync(string jsonData, string csvData, Action<string, Exception> callback)
    {
        try
        {
            string result = await geminiFeedback.GenerateFeedbackAsync(jsonData, csvData, UpdateScorePanelFeedback);
            callback?.Invoke(result, null);
        }
        catch (Exception e)
        {
            callback?.Invoke(null, e);
        }
    }

    private static void RefreshMultiScorePanelIfVisible()
    {
        if (OpenOceanMain.inst != null && OpenOceanMain.inst.lobbyState == LobbyState.MultiScorePanel)
        {
            OpenOceanMain.inst.RefreshMultiScorePanel();
        }
    }

    private static bool ShouldUseGeminiFeedback()
    {
        return OpenOceanMain.inst != null && OpenOceanMain.inst.currentTrainingState == TrainingState.Adaptive;
    }

    private static void UpdateScorePanelFeedback(string feedback)
    {
        if (OpenOceanMain.inst?.feedbackText != null)
        {
            OpenOceanMain.inst.feedbackText.text = string.IsNullOrWhiteSpace(feedback) ? string.Empty : feedback;
        }
    }

    private void EnsureFallbackFeedback(ScenarioDataMgr.ScenarioData data)
    {
        if (string.IsNullOrWhiteSpace(data.feedback))
        {
            data.feedback = GetFeedback();
        }
    }

    // ───────────────────────── CSV Building (In-Memory) ─────────────────────────

    private string BuildCsvForCurrentScenario()
    {
        string dateTimeNow = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); 
        string studentID = OpenOceanMain.inst.playerName ?? "UnknownStudent"; 

        string group = "Non-Adaptive"; 
        if (studentID != null && studentID.StartsWith("Student", StringComparison.OrdinalIgnoreCase))
        {
            string numericPart = studentID.Substring("Student".Length);
            if (int.TryParse(numericPart, out int studentIdNumber))
            {
                group = (studentIdNumber % 2 == 0) ? "Adaptive" : "Non-Adaptive";
            }
        }

        string gameType = OpenOceanMain.inst.currentTrainingState.ToString(); 
        string result = playerWon ? "Win" : "Loss"; 
        float scorePercent = score; 
        float dmgTaken = this.damageTaken; 
        float dmgDealt = this.damageDealt; 

        Dictionary<EntityType, int> initialUnitCounts = ScenarioGenerator.inst.entityQuantities
            .ToDictionary(eq => eq.entityType, eq => eq.unitCount);
        Dictionary<EntityType, int> destroyedPlayerUnits = GetDestroyedUnits(PlayerMgr.inst.localPlayer);
        TactPlayer aiPlayer = PlayerMgr.inst.player2;
        Dictionary<EntityType, int> destroyedAIUnits = (aiPlayer != null) ? GetDestroyedUnits(aiPlayer) : new Dictionary<EntityType, int>();

        string playerBaseLocation = GetCardinalDirection(ScenarioGenerator.inst?.posPlayer1List?.FirstOrDefault() ?? Vector3.zero);
        string aiBaseLocation = GetCardinalDirection(ScenarioGenerator.inst?.posPlayer2List?.FirstOrDefault() ?? Vector3.zero);

        string winCondition = winReason; 
        float timeTaken = OpenOceanMain.inst.playSessionDuration; 
        int aiLevel = ScenarioGenerator.inst.CurrentDifficultyLevel < 0.33f ? 1 : (ScenarioGenerator.inst.CurrentDifficultyLevel < 0.66f ? 2 : 3);
        float aiDifficulty = ScenarioGenerator.inst.CurrentDifficultyLevel; 

        return BuildCsvContent(dateTimeNow, studentID, group, gameType, result, dmgTaken, dmgDealt,
            scorePercent, timeTaken, aiLevel, aiDifficulty, winCondition, playerBaseLocation, aiBaseLocation,
            initialUnitCounts, destroyedPlayerUnits, destroyedAIUnits);
    }

    private string BuildCsvContent(string dateTimeNow, string studentID, string group, string gameType,
        string result, float damageTaken, float damageDealt, float scorePercent, float timeTaken,
        int aiLevel, float aiDifficulty, string winCondition, string playerBaseLocation, string aiBaseLocation,
        Dictionary<EntityType, int> initialUnitCounts, Dictionary<EntityType, int> destroyedPlayerUnits,
        Dictionary<EntityType, int> destroyedAIUnits)
    {
        var allEntityTypes = initialUnitCounts.Keys
            .Union(destroyedPlayerUnits.Keys)
            .Union(destroyedAIUnits.Keys)
            .Distinct()
            .OrderBy(et => et.ToString())
            .ToList();

        StringBuilder header = new StringBuilder();
        header.Append("DateTime,StudentID,Group,GameType,Result,DamageTaken,DamageDealt,ScorePercent,TimeTaken,AILevel,AIDifficulty,WinCondition,PlayerBaseLocation,AIBaseLocation");
        foreach (var unitType in allEntityTypes)
        {
            header.Append($",Initial_{unitType},DestroyedPlayer_{unitType},DestroyedAI_{unitType}");
        }

        StringBuilder row = new StringBuilder();
        row.Append($"{dateTimeNow},{studentID},{group},{gameType},{result},{damageTaken:0.##},{damageDealt:0.##},{scorePercent:0.##},{timeTaken:0.##},{aiLevel},{aiDifficulty:0.##},{winCondition},{playerBaseLocation},{aiBaseLocation}");
        foreach (var unitType in allEntityTypes)
        {
            int initialCount = initialUnitCounts.TryGetValue(unitType, out var ic) ? ic : 0;
            int destroyedPlayerCount = destroyedPlayerUnits.TryGetValue(unitType, out var dpc) ? dpc : 0;
            int destroyedAICount = destroyedAIUnits.TryGetValue(unitType, out var dac) ? dac : 0;
            row.Append($",{initialCount},{destroyedPlayerCount},{destroyedAICount}");
        }

        return header.ToString() + "\n" + row.ToString();
    }

    // ───────────────────────── Feedback ─────────────────────────

    public string GetFeedback()
    {
        if (feedbackData == null)
        {
            return string.Empty;
        }

        List<string> selectedFeedbacks = new();
        
        if (playerWon)
        {
            if (score >= 85)
            {
                selectedFeedbacks.AddRange(feedbackData.victoryFeedbacks.highScore);
                if (winReason.Contains("allDestroyed"))
                    selectedFeedbacks.AddRange(feedbackData.victoryFeedbacks.allDestroyed);
            }
            else if (score >= 70)
            {
                selectedFeedbacks.AddRange(feedbackData.victoryFeedbacks.mediumScore);
            }
            else 
            {
                selectedFeedbacks.AddRange(feedbackData.victoryFeedbacks.lowScore);
            }
        }
        else 
        {
            selectedFeedbacks.AddRange(feedbackData.defeatFeedbacks);
        }

        if (winReason.Contains("baseDestroyed"))
            selectedFeedbacks.AddRange(feedbackData.baseDestroyedFeedbacks);

        int feedbacksToPotentiallyAdd = 3 - selectedFeedbacks.Count;
        if (feedbacksToPotentiallyAdd > 0 && feedbackData.generalFeedbacks != null && feedbackData.generalFeedbacks.Count > 0)
        {
            List<string> availableGeneralFeedbacks = feedbackData.generalFeedbacks.Except(selectedFeedbacks).ToList();

            for (int i = 0; i < feedbacksToPotentiallyAdd && availableGeneralFeedbacks.Count > 0; i++)
            {
                int randomIndex = UnityEngine.Random.Range(0, availableGeneralFeedbacks.Count);
                selectedFeedbacks.Add(availableGeneralFeedbacks[randomIndex]);
                availableGeneralFeedbacks.RemoveAt(randomIndex);
            }
        }

        if (selectedFeedbacks.Any())
        {
            var feedbacksToDisplay = selectedFeedbacks.Take(3);
            StringBuilder feedbackString = new StringBuilder();
            int index = 1;
            foreach (var fb in feedbacksToDisplay)
            {
                feedbackString.AppendLine($"{index}. {fb}");
                index++;
            }
            return feedbackString.ToString();
        }
        else
        {
            return string.Empty; 
        }
    }

    // ───────────────────────── Logging ─────────────────────────

    private void LogVictoryMessage()
    {
        string message = playerWon ?
            $"PLAYER VICTORY! Damage Dealt: {damageDealt} | Taken: {damageTaken}" :
            $"AI VICTORY! Damage Dealt: {damageDealt} | Taken: {damageTaken}";
        Debug.Log(message);
    }

    public void ResetScores()
    {
        damageDealt = 0;
        damageTaken = 0;
        playerWon = false;
        aiWon = false;
        score = 0;
        winReason = string.Empty; 
    }

    private string GetGameTypeFolder() 
    {
        string playerCode = OpenOceanMain.inst.playerCode;
        if (playerCode == "AAA") return "Adaptive";
        if (playerCode == "BBB") return "Non-Adaptive";
        if (playerCode == "ABC") return "Pre-Test";
        if (playerCode == "XYZ") return "Post-Test";
        return "UnknownGameType"; 
    }

    public (string studentCsvPath, string studentDirectory, string commonCsvPath) GenerateLogPaths(string studentID, string sessionStartTimeStr, string gameTypeFolder)
    {
        string studentFileName = $"{studentID}_{sessionStartTimeStr}_{gameTypeFolder}.csv";
        string studentDirectory = Path.Combine(Application.persistentDataPath, studentID, gameTypeFolder);
        string studentCsvPath = Path.Combine(studentDirectory, studentFileName); 
        string commonCsvPath = Path.Combine(Application.persistentDataPath, CommonLogFileName);
        return (studentCsvPath, studentDirectory, commonCsvPath);
    }

    // ───────────────────────── LogGameData ─────────────────────────

    public void LogGameData()
    {
        string dateTimeNow = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); 

        string studentID = OpenOceanMain.inst.playerName ?? "UnknownStudent"; 
        string group = "Non-Adaptive"; 
        if (studentID != null && studentID.StartsWith("Student", StringComparison.OrdinalIgnoreCase))
        {
            string numericPart = studentID.Substring("Student".Length);
            if (int.TryParse(numericPart, out int studentIdNumber))
            {
                group = (studentIdNumber % 2 == 0) ? "Adaptive" : "Non-Adaptive";
            }
            else
            {
                group = "Non-Adaptive";
            }
        }

        string gameType = OpenOceanMain.inst.currentTrainingState.ToString(); 
        string result = playerWon ? "Win" : "Loss"; 
        float scorePercent = score; 
        float damageTaken = this.damageTaken; 
        float damageDealt = this.damageDealt; 

        Dictionary<EntityType, int> initialUnitCounts = ScenarioGenerator.inst.entityQuantities
            .ToDictionary(eq => eq.entityType, eq => eq.unitCount);
        Dictionary<EntityType, int> destroyedPlayerUnits = GetDestroyedUnits(PlayerMgr.inst.localPlayer);
        TactPlayer aiPlayer = PlayerMgr.inst.player2;
        Dictionary<EntityType, int> destroyedAIUnits = (aiPlayer != null) ? GetDestroyedUnits(aiPlayer) : new Dictionary<EntityType, int>();

        string playerBaseLocation = GetCardinalDirection(ScenarioGenerator.inst?.posPlayer1List?.FirstOrDefault() ?? Vector3.zero);
        string aiBaseLocation = GetCardinalDirection(ScenarioGenerator.inst?.posPlayer2List?.FirstOrDefault() ?? Vector3.zero);

        string winCondition = winReason; 
        float timeTaken = OpenOceanMain.inst.playSessionDuration; 
        int aiLevel = ScenarioGenerator.inst.CurrentDifficultyLevel < 0.33f ? 1 : (ScenarioGenerator.inst.CurrentDifficultyLevel < 0.66f ? 2 : 3);
        float aiDifficulty = ScenarioGenerator.inst.CurrentDifficultyLevel; 

        string gameTypeFolder = GetGameTypeFolder(); 

        // Build CSV content in memory (works on all platforms including WebGL)
        string csvContent = BuildCsvContent(dateTimeNow, studentID, group, gameType, result,
            damageTaken, damageDealt, scorePercent, timeTaken, aiLevel, aiDifficulty,
            winCondition, playerBaseLocation, aiBaseLocation,
            initialUnitCounts, destroyedPlayerUnits, destroyedAIUnits);

        // Write to local files only on non-WebGL platforms
#if !UNITY_WEBGL || UNITY_EDITOR
        var (studentCsvPath, studentDirectory, commonCsvPath) = GenerateLogPaths(studentID, sessionStartTimeString, gameTypeFolder);

        WriteToCsv(studentCsvPath, studentDirectory, dateTimeNow, studentID, group, gameType, result,
            damageTaken, damageDealt, scorePercent, timeTaken, aiLevel, aiDifficulty,
            winCondition, playerBaseLocation, aiBaseLocation,
            initialUnitCounts, destroyedPlayerUnits, destroyedAIUnits);

        WriteToCsv(commonCsvPath, Application.persistentDataPath, dateTimeNow, studentID, group, gameType, result,
            damageTaken, damageDealt, scorePercent, timeTaken, aiLevel, aiDifficulty,
            winCondition, playerBaseLocation, aiBaseLocation,
            initialUnitCounts, destroyedPlayerUnits, destroyedAIUnits);
#endif

        // Upload to S3 and DynamoDB (works on all platforms — uses in-memory CSV, no file I/O)
        string baseName = $"{studentID}_{sessionStartTimeString}_{gameTypeFolder}_Game{OpenOceanMain.inst.gamesPlayedCount}.csv";
        string remotePath = $"{studentID}/{gameTypeFolder}/{baseName}";

        StartCoroutine(UploadToS3(csvContent, studentID, gameTypeFolder, baseName));
        StartCoroutine(SaveScoreToDynamoDB(dateTimeNow, studentID, group, gameType, result,
            damageTaken, damageDealt, scorePercent, timeTaken, aiLevel, aiDifficulty,
            winCondition, playerBaseLocation, aiBaseLocation,
            initialUnitCounts, destroyedPlayerUnits, destroyedAIUnits, remotePath));
    }

    // ───────────────────────── WriteToCsv (Non-WebGL Only) ─────────────────────────

    private void WriteToCsv(string csvPath, string directoryPath, string dateTimeNow, string studentID,
        string group, string gameType, string result, float damageTaken, float damageDealt,
        float scorePercent, float timeTaken, int aiLevel, float aiDifficulty, string winCondition,
        string playerBaseLocation, string aiBaseLocation,
        Dictionary<EntityType, int> initialUnitCounts,
        Dictionary<EntityType, int> destroyedPlayerUnits,
        Dictionary<EntityType, int> destroyedAIUnits)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return;
#else
        try
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            bool fileExists = File.Exists(csvPath); 
            bool isEmpty = !fileExists || new FileInfo(csvPath).Length == 0; 

            using (var writer = new StreamWriter(csvPath, true)) 
            {
                var allEntityTypes = initialUnitCounts.Keys
                    .Union(destroyedPlayerUnits.Keys)
                    .Union(destroyedAIUnits.Keys)
                    .Distinct()
                    .OrderBy(et => et.ToString());

                if (isEmpty)
                {
                    writer.Write("DateTime,StudentID,Group,GameType,Result,DamageTaken,DamageDealt,ScorePercent,TimeTaken,AILevel,AIDifficulty,WinCondition,PlayerBaseLocation,AIBaseLocation");
                    foreach (var unitType in allEntityTypes)
                    {
                        writer.Write($",Initial_{unitType},DestroyedPlayer_{unitType},DestroyedAI_{unitType}");
                    }
                    writer.WriteLine(); 
                }

                writer.Write($"{dateTimeNow},{studentID},{group},{gameType},{result},{damageTaken:0.##},{damageDealt:0.##},{scorePercent:0.##},{timeTaken:0.##},{aiLevel},{aiDifficulty:0.##},{winCondition},{playerBaseLocation},{aiBaseLocation}");
                foreach (var unitType in allEntityTypes)
                {
                    int initialCount = initialUnitCounts.TryGetValue(unitType, out var ic) ? ic : 0;
                    int destroyedPlayerCount = destroyedPlayerUnits.TryGetValue(unitType, out var dpc) ? dpc : 0;
                    int destroyedAICount = destroyedAIUnits.TryGetValue(unitType, out var dac) ? dac : 0;
                    writer.Write($",{initialCount},{destroyedPlayerCount},{destroyedAICount}");
                }
                writer.WriteLine(); 
            }
            Debug.Log($"Game data logged to {csvPath}"); 
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to write CSV: {e.Message}");
        }
#endif
    }

    // ───────────────────────── Upload to S3 (No File I/O) ─────────────────────────

    private IEnumerator UploadToS3(string csvContent, string studentID, string gameTypeFolder, string baseName)
    {
        UploadPayload payload = new UploadPayload
        {
            filename = $"{studentID}/{gameTypeFolder}/{baseName}",
            content = csvContent,
            base64 = false
        };

        string jsonPayload = JsonUtility.ToJson(payload);

        using (UnityWebRequest request = new UnityWebRequest(API_URL, "POST"))
        {
            byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(jsonBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"S3 Upload successful! Response: {request.downloadHandler.text}");
            }
            else
            {
                Debug.LogError($"S3 Upload failed! Error: {request.error} | Response: {request.downloadHandler.text}");
            }
        }
    }

    // ───────────────────────── Save to DynamoDB (Manual JSON) ─────────────────────────

    private IEnumerator SaveScoreToDynamoDB(string dateTimeNow, string studentID, string group,
        string gameType, string result, float damageTaken, float damageDealt, float scorePercent,
        float timeTaken, int aiLevel, float aiDifficulty, string winCondition,
        string playerBaseLocation, string aiBaseLocation,
        Dictionary<EntityType, int> initialUnitCounts,
        Dictionary<EntityType, int> destroyedPlayerUnits,
        Dictionary<EntityType, int> destroyedAIUnits,
        string csvFilePath)
    {
        // Build unit breakdown as raw JSON — JsonUtility cannot serialize Dictionary
        StringBuilder ubJson = new StringBuilder();
        ubJson.Append("{");
        bool first = true;

        var allEntityTypes = initialUnitCounts.Keys
            .Union(destroyedPlayerUnits.Keys)
            .Union(destroyedAIUnits.Keys)
            .Distinct();

        foreach (var unitType in allEntityTypes)
        {
            if (!first) ubJson.Append(",");
            first = false;
            int init = initialUnitCounts.GetValueOrDefault(unitType, 0);
            int dp = destroyedPlayerUnits.GetValueOrDefault(unitType, 0);
            int da = destroyedAIUnits.GetValueOrDefault(unitType, 0);
            ubJson.Append($"\"{unitType}\":{{\"initial\":{init},\"destroyedPlayer\":{dp},\"destroyedAI\":{da}}}");
        }
        ubJson.Append("}");

        // Build full JSON payload manually to properly include the dictionary
        string jsonPayload = "{" +
            $"\"action\":\"saveScore\"," +
            $"\"studentID\":\"{EscapeJson(studentID)}\"," +
            $"\"dateTime\":\"{EscapeJson(dateTimeNow)}\"," +
            $"\"group\":\"{EscapeJson(group)}\"," +
            $"\"gameType\":\"{EscapeJson(gameType)}\"," +
            $"\"result\":\"{EscapeJson(result)}\"," +
            $"\"score\":{scorePercent}," +
            $"\"damageDealt\":{damageDealt}," +
            $"\"damageTaken\":{damageTaken}," +
            $"\"timeTaken\":{timeTaken}," +
            $"\"aiLevel\":{aiLevel}," +
            $"\"aiDifficulty\":{aiDifficulty}," +
            $"\"winCondition\":\"{EscapeJson(winCondition)}\"," +
            $"\"playerBaseLocation\":\"{EscapeJson(playerBaseLocation)}\"," +
            $"\"aiBaseLocation\":\"{EscapeJson(aiBaseLocation)}\"," +
            $"\"ourUnitsDestroyed\":{destroyedPlayerUnits.Values.Sum()}," +
            $"\"enemyUnitsDestroyed\":{destroyedAIUnits.Values.Sum()}," +
            $"\"totalUnits\":{initialUnitCounts.Values.Sum()}," +
            $"\"scenarioNumber\":{OpenOceanMain.inst.gamesPlayedCount}," +
            $"\"csvFilePath\":\"{EscapeJson(csvFilePath)}\"," +
            $"\"unitBreakdown\":{ubJson}" +
        "}";

        using (UnityWebRequest request = new UnityWebRequest(API_URL, "POST"))
        {
            byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(jsonBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"DynamoDB Score saved! Response: {request.downloadHandler.text}");
            }
            else
            {
                Debug.LogError($"DynamoDB Save failed! Error: {request.error} | Response: {request.downloadHandler.text}");
            }
        }
    }

    // ───────────────────────── Helpers ─────────────────────────

    private static string EscapeJson(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
    }

    private string GetCardinalDirection(Vector3 position, float threshold = 10.0f)
    {
        if (position == Vector3.zero) return "Unknown"; 

        float absX = Mathf.Abs(position.x); 
        float absZ = Mathf.Abs(position.z); 

        if (absX < threshold && absZ < threshold) return "Center";

        if (absZ >= absX)
        {
            return position.z > 0 ? "North" : "South"; 
        }
        else
        {
            return position.x > 0 ? "East" : "West"; 
        }
    }

    private Dictionary<EntityType, int> GetDestroyedUnits(TactPlayer owner)
    {
        Dictionary<EntityType, int> destroyed = new(); 
        if (ScenarioGenerator.inst == null || ScenarioGenerator.inst.entityQuantities == null || EntityMgr.inst == null || EntityMgr.inst.entities == null)
        {
            return destroyed; 
        }

        Dictionary<EntityType, int> currentCounts = EntityMgr.inst.entities
            .Where(e => e.owner == owner)
            .GroupBy(e => e.entityType)
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (EntityQuantity eq in ScenarioGenerator.inst.entityQuantities)
        {
            int initialCount = eq.unitCount;
            int currentCount = currentCounts.GetValueOrDefault(eq.entityType, 0);
            destroyed[eq.entityType] = Math.Max(0, initialCount - currentCount);
        }

        return destroyed; 
    }

    private Vector3 FindBasePosition(TactPlayer owner)
    {
        if (EntityMgr.inst == null || EntityMgr.inst.entities == null)
        {
            return Vector3.zero; 
        }
        Entity baseEntity = EntityMgr.inst.entities
            .FirstOrDefault(e => e.owner == owner && e.entityRole == EntityRole.Base);
        return baseEntity?.transform.position ?? Vector3.zero;
    }

    public float LastScenarioScore()
    {
        Debug.Log($"Player scores count: {playerScores.Count}");
        return playerScores.Count > 0 ? playerScores.Last() : 0f;
    }

    public class CustomCertificateHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true;
        }
    }
}