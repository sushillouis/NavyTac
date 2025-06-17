using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;
using UnityEngine.Networking;
using System.Collections;

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

    private static readonly List<string> generalFeedbacks = new List<string>
    {
        "JARI USVs can force the enemy to reveal positions — use them early.",
        "Attack-Move (A + Right Click) prevents surprise deaths.",
        "SeaHunters can help protect DDG51s from flank attacks.",
        "Use SeaHunters to flank or intercept weakened enemies.",
        "Use JARIs to spot for missiles or long-range units.",
        "Let SeaHunters scout while DDG51s prepare to strike.",
        "Group SeaHunters separately for quick response tasks.",
        "Use Ctrl + 1–9 to assign units to control groups.",
        "Use high-speed units for map control and harassment.",
        "Diversify your formation — don’t overuse one unit type.",
        "JARI USVs are expendable — use them to test enemy defenses.",
        "Position DDG51s at the rear for cover fire support.",
        "Don’t stack all SeaHunters — spread them out.",
        "Keep SeaHunters close to the action but not in the front line.",
        "Maintain pressure with SeaHunters while rotating DDG51s.",
        "Always leave some units behind to defend your base.",
        "Send JARI USVs to scout ahead before moving main units.",
        "Build combined waves — scouts, support, then heavy hitters.",
        "Destroyers are slow — plan their movement ahead of time.",
        "Mix units to cover weaknesses — scouts reveal, heavies attack.",
        "Don’t let JARI USVs idle — keep them active on flanks.",
        "Avoid chasing fleeing enemies if your base is exposed.",
        "SeaHunters are balanced — use them to bridge between scouts and firepower.",
        "Don’t lead with SeaHunters — support your heavier ships.",
        "JARI USVs are fast — use them to bait or distract.",
        "Pair DDG51s with SeaHunters to create layered firepower.",
        "Use DDG51s to finish high-value targets, not to chase scouts.",
        "SeaHunters are versatile — don’t waste them on suicide runs.",
        "Destroyers are valuable — don’t lead the charge with them.",
        "Scout then retreat — don’t lose JARI USVs to unnecessary combat.",
        "Use a scout to trigger enemy fire before sending in DDG51s.",
        "Use SeaHunters to maintain battlefield vision.",
        "Adapt your unit use depending on who you're facing.",
        "Protect your base even when dominating offensively.",
        "Never clump all units — area attacks punish tight formations.",
        "Attack from two sides — it splits the enemy’s attention.",
        "Micro-manage each group for better survival rates.",
        "Don’t send your whole fleet down one route.",
        "Use JARI USVs for hit-and-run tactics."
    };

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
        }
    }

    public void CheckVictory()
    {
        if (OpenOceanMain.inst.lobbyState == LobbyState.Replay)
            return ;
        if(OpenOceanMain.inst.currentTrainingState == TrainingState.Tutorial)
        {
            playerWon = true; 
            aiWon = false; 
            damageDealt = 1000; 
            damageTaken = 0; 
            winReason = "Tutorial Completed"; 
        }
        else if (GameMgr.inst == null || GameMgr.inst.entityQuantities == null || EntityMgr.inst == null || EntityMgr.inst.entities == null)
        {
            Debug.LogWarning("GameMgr or EntityMgr is not initialized properly.");
            return;
        }
        if (!playerWon && !aiWon) return;

        OpenOceanMain.inst.lobbyState = LobbyState.ScorePanel;

        score = (float)(0.3 * (playerWon ? 1 : 0)) * 100 + 0.7f * (damageDealt / (damageDealt + damageTaken)) * 100;
        if (ReplayMgr.inst != null && ReplayMgr.inst.isRecording)
        {
            playerScores.Add(score); 
        }
        if (OpenOceanMain.inst.lobbyState != LobbyState.Replay)
        {
            ScenarioDataMgr.ScenarioData data = new ScenarioDataMgr.ScenarioData();
            data.scenarioNumber = OpenOceanMain.inst.gamesPlayedCount;
            data.totalUnits = GameMgr.inst.entityQuantities.Sum(eq => eq.unitCount); 
                                                                                     
            var initialCounts = GameMgr.inst.entityQuantities.ToDictionary(eq => eq.entityType, eq => eq.unitCount);
            data.totalJARI = initialCounts.GetValueOrDefault(EntityType.JARIUSV, 0);
            data.totalSeaHunter = initialCounts.GetValueOrDefault(EntityType.SeaHunter, 0);
            data.totalDDG51 = initialCounts.GetValueOrDefault(EntityType.DDG51, 0);

            var destroyedPlayer = GetDestroyedUnits(PlayerMgr.inst.localPlayer);
            var destroyedAI = GetDestroyedUnits(PlayerMgr.inst.player2);
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

    public string GetFeedback()
    {
        List<string> selectedFeedbacks = new();
        if (playerWon)
        {
            if (score >= 85)
            {
                selectedFeedbacks.Add("Avoid direct fights with JARI USVs — they’re scouts, not tanks.");
                selectedFeedbacks.Add("Flank with JARI USVs while heavier ships press forward.");
                if (winReason.Contains("allDestroyed"))
                    selectedFeedbacks.Add("Next time, see if you can do this while taking even less damage.");
            }
            else if (score >= 70)
            {
                selectedFeedbacks.Add("Use DDG51s for decisive strikes, not continuous harassment.");
                selectedFeedbacks.Add("Spread out your DDG51s to avoid splash damage.");
            }
            else 
            {
                selectedFeedbacks.Add("Retreat and regroup instead of losing all at once.");
                selectedFeedbacks.Add("Keep Destroyers protected behind lighter units.");
            }
        }
        else 
        {
            selectedFeedbacks.Add("Avoid moving DDG51s without a scout — they’re not expendable.");
            selectedFeedbacks.Add("Use terrain and spacing to avoid ambushes.");
            selectedFeedbacks.Add("Don’t clump Destroyers — it makes them vulnerable to area attacks.");
            selectedFeedbacks.Add("Send scouts before committing large units.");
        }

        if (winReason.Contains("baseDestroyed"))
            selectedFeedbacks.Add("Try combining base attacks with flanking units to distract defenders.");

        int feedbacksToPotentiallyAdd = 3 - selectedFeedbacks.Count;
        if (feedbacksToPotentiallyAdd > 0 && generalFeedbacks.Count > 0)
        {
            List<string> availableGeneralFeedbacks = generalFeedbacks.Except(selectedFeedbacks).ToList();

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
            System.Text.StringBuilder feedbackString = new System.Text.StringBuilder();
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


    private void LogVictoryMessage()
    {
        string message = playerWon ?
            $"PLAYER VICTORY! Damage Dealt: {damageDealt} | Taken: {damageTaken}" :
            $"AI VICTORY! Damage Dealt: {damageDealt} | Taken: {damageTaken}";

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

    public (string studentCsvPath, string studentDirectory, string commonCsvPath) GenerateLogPaths(string studentID, string sessionStartTimeString, string gameTypeFolder)
    {
        string studentFileName = $"{studentID}_{sessionStartTimeString}_{gameTypeFolder}.csv";
        string studentDirectory = Path.Combine(
        Application.persistentDataPath,
        studentID,
        gameTypeFolder
        );
        string studentCsvPath = Path.Combine(studentDirectory, studentFileName); 

        string commonCsvPath = Path.Combine(Application.persistentDataPath, CommonLogFileName);

        return (studentCsvPath, studentDirectory, commonCsvPath);
    }

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
        else if (studentID != "UnknownStudent") 
        {
        }


        string gameType = OpenOceanMain.inst.currentTrainingState.ToString(); 
        string result = playerWon ? "Win" : "Loss"; 
        float scorePercent = score; 

        float damageTaken = this.damageTaken; 
        float damageDealt = this.damageDealt; 

        Dictionary<EntityType, int> initialUnitCounts = GameMgr.inst.entityQuantities
            .ToDictionary(eq => eq.entityType, eq => eq.unitCount);


        Dictionary<EntityType, int> destroyedPlayerUnits = GetDestroyedUnits(PlayerMgr.inst.localPlayer);
        TactPlayer aiPlayer = PlayerMgr.inst.player2;
        Dictionary<EntityType, int> destroyedAIUnits = (aiPlayer != null) ? GetDestroyedUnits(aiPlayer) : new Dictionary<EntityType, int>();


        string playerBaseLocation = GetCardinalDirection(GameMgr.inst.posPlayer1);
        string aiBaseLocation = GetCardinalDirection(GameMgr.inst.posPlayer2);


        string winCondition = winReason; 
        float timeTaken = OpenOceanMain.inst.playSessionDuration; 
        int aiLevel = GameMgr.inst.difficultyLevel < 0.33f ? 1 : (GameMgr.inst.difficultyLevel < 0.66f ? 2 : 3);
        float aiDifficulty = GameMgr.inst.difficultyLevel; 

        string gameTypeFolder = GetGameTypeFolder(); 

        var (studentCsvPath, studentDirectory, commonCsvPath) = GenerateLogPaths(studentID, sessionStartTimeString, gameTypeFolder);

        WriteToCsv(studentCsvPath, studentDirectory, dateTimeNow, studentID, group, gameType, result, damageTaken, damageDealt, scorePercent, timeTaken, aiLevel, aiDifficulty, winCondition, playerBaseLocation, aiBaseLocation, initialUnitCounts, destroyedPlayerUnits, destroyedAIUnits);

        WriteToCsv(commonCsvPath, Application.persistentDataPath, dateTimeNow, studentID, group, gameType, result, damageTaken, damageDealt, scorePercent, timeTaken, aiLevel, aiDifficulty, winCondition, playerBaseLocation, aiBaseLocation, initialUnitCounts, destroyedPlayerUnits, destroyedAIUnits);
    }

    private void WriteToCsv(string csvPath, string directoryPath, string dateTimeNow, string studentID, string group, string gameType, string result, float damageTaken, float damageDealt, float scorePercent, float timeTaken, int aiLevel, float aiDifficulty, string winCondition, string playerBaseLocation, string aiBaseLocation, Dictionary<EntityType, int> initialUnitCounts, Dictionary<EntityType, int> destroyedPlayerUnits, Dictionary<EntityType, int> destroyedAIUnits)
    {
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
        catch (System.Exception ex)
        {
        }
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
        if (GameMgr.inst == null || GameMgr.inst.entityQuantities == null || EntityMgr.inst == null || EntityMgr.inst.entities == null)
        {
            return destroyed; 
        }
        foreach (EntityQuantity eq in GameMgr.inst.entityQuantities)
        {
            int remaining = EntityMgr.inst.entities.Count(e => e.entityType == eq.entityType && e.owner == owner);
            destroyed[eq.entityType] = eq.unitCount - remaining;
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
    private IEnumerator UploadToServer(string csvPath)
    {
        string url = "164.90.151.175/upload/";
        string csvContent = File.ReadAllText(csvPath);
        string filename = Path.GetFileName(csvPath);

        string jsonPayload = $"{{\"filename\":\"{filename}\", \"content\":\"{csvContent}\"}}";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(jsonBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.certificateHandler = new CustomCertificateHandler(); 

            yield return request.SendWebRequest();

            Debug.Log($"Response Code: {request.responseCode}");
            Debug.Log($"Response: {request.downloadHandler.text}");
        }
    }

    public class CustomCertificateHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true; 
        }
    }

    public float LastScenarioScore()
    {
        Debug.Log($"Player scores count: {playerScores.Count}");
        
        return playerScores.Count > 0 ? playerScores.Last() : 0f;
    }
}

