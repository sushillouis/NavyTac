using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public class ScoreMgr : MonoBehaviour
{
    public static ScoreMgr inst;
    public float damageDealt;
    public float damageTaken;
    public bool playerWon;
    public float score;
    public bool aiWon;
    public String winReason;
    private void Awake()
    {
        if (inst != null && inst != this)
            Destroy(gameObject);
        else
            inst = this;
    }

    public void CheckVictory()
    {
        if (!playerWon && !aiWon) return;
        OpenOceanMain.inst.lobbyState = OpenOceanMain.LobbyState.ScorePanel;
        score = (float)(0.5 * (playerWon ? 1 : 0))*100 + 0.5f * (damageDealt / (damageDealt + damageTaken)) * 100;
        Debug.Log($"Score: {score}");
        LogGameData();
        UpdateScoreDisplay();
        LogVictoryMessage();
        FXMgr.inst.ResetEffects();
        
    }

    private void UpdateScoreDisplay()
    {
        if (OpenOceanMain.inst.damageDealtText != null)
            OpenOceanMain.inst.damageDealtText.text = $"{damageDealt:0}";

        if (OpenOceanMain.inst.damageTakenText != null)
            OpenOceanMain.inst.damageTakenText.text = $"{damageTaken:0}";

        if (OpenOceanMain.inst.winnerText != null)
            OpenOceanMain.inst.winnerText.text = playerWon ? "Player Victory!" : "AI Victory!";
        if (OpenOceanMain.inst.scoreText != null)
            OpenOceanMain.inst.scoreText.text = $"{score:0.##}%";
        if (OpenOceanMain.inst.ourUnitsDestroyedText != null)
            OpenOceanMain.inst.ourUnitsDestroyedText.text = $"{GetDestroyedUnits(PlayerMgr.inst.localPlayer).Values.Sum()}";
        if (OpenOceanMain.inst.enemyUnitsDestroyedText != null)
            OpenOceanMain.inst.enemyUnitsDestroyedText.text = $"{GetDestroyedUnits(PlayerMgr.inst.players.Find(p => p.name == "Ai")).Values.Sum()}";
        if (OpenOceanMain.inst.winConditionText != null)
            OpenOceanMain.inst.winConditionText.text = winReason;
    }



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
    }
    public void LogGameData()
    {
        // 0. Date & Time
        string dateTimeNow = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // 1. Basic Info
        string studentID = OpenOceanMain.inst.playerName;
        string group = "Non-Adaptive"; // Default group
        if (studentID != null && studentID.StartsWith("Student", StringComparison.OrdinalIgnoreCase))
        {
            string numericPart = studentID.Substring("Student".Length);
            if (int.TryParse(numericPart, out int studentIdNumber))
            {
            group = (studentIdNumber % 2 == 0) ? "Adaptive" : "Non-Adaptive";
            }
            else
            {
            Debug.LogWarning($"Numeric part of StudentID '{numericPart}' could not be parsed. Defaulting group to 'Non-Adaptive'.");
            group = "Non-Adaptive"; // Ensure group is set in this case too
            }
        }
        else
        {
            Debug.LogWarning($"StudentID '{studentID}' could not be parsed as an integer. Defaulting group to 'Non-Adaptive'.");
        }

        string gameType = OpenOceanMain.inst.currentTrainingState.ToString();
        string result = playerWon ? "Win" : "Loss";
        float scorePercent = score;

        // 2. Damage & Score
        float damageTaken = this.damageTaken;
        float damageDealt = this.damageDealt;

        // 3. Unit Counts (Initial counts from GameMgr)
        Dictionary<EntityType, int> initialUnitCounts = GameMgr.inst.entityQuantities
            .ToDictionary(eq => eq.entityType, eq => eq.unitCount);


        // 4. Destroyed Units (Initial Counts - Remaining)
        Dictionary<EntityType, int> destroyedPlayerUnits = GetDestroyedUnits(PlayerMgr.inst.localPlayer);
        TactPlayer aiPlayer = PlayerMgr.inst.players.Find(p => p.name == "Ai");
        Dictionary<EntityType, int> destroyedAIUnits = (aiPlayer != null) ? GetDestroyedUnits(aiPlayer) : new Dictionary<EntityType, int>();


        // 5. Base Locations (Cardinal Directions)
        string playerBaseLocation = GetCardinalDirection(GameMgr.inst.posPlayer1);
        string aiBaseLocation = GetCardinalDirection(GameMgr.inst.posPlayer2);


        // 6. Metadata
        string winCondition = winReason;
        float timeTaken = Time.timeSinceLevelLoad;
        int aiLevel = (EnemyAIMgr.inst != null) ? EnemyAIMgr.inst.currentLevel : -1;
        float aiDifficulty = GameMgr.inst.difficultyLevel;

        // Prepare CSV path
        string csvPath = System.IO.Path.Combine(Application.persistentDataPath, "GameLogs.csv");
        bool fileExists = System.IO.File.Exists(csvPath);

        try
        {
            using (var writer = new System.IO.StreamWriter(csvPath, true))
            {
                var allEntityTypes = initialUnitCounts.Keys
                                    .Union(destroyedPlayerUnits.Keys)
                                    .Union(destroyedAIUnits.Keys)
                                    .Distinct()
                                    .OrderBy(et => et.ToString());

                if (!fileExists || new System.IO.FileInfo(csvPath).Length == 0)
                {
                    // Write header if file doesn't exist or is empty
                    writer.Write("DateTime,StudentID,Group,GameType,Result,DamageTaken,DamageDealt,ScorePercent,TimeTaken,AILevel,AIDifficulty,WinCondition,PlayerBaseLocation,AIBaseLocation"); // Added "Group" to Header

                    foreach (var unitType in allEntityTypes)
                    {
                        writer.Write($",Initial_{unitType},DestroyedPlayer_{unitType},DestroyedAI_{unitType}");
                    }
                    writer.WriteLine();
                }

                // Write data row
                writer.Write($"{dateTimeNow},{studentID},{group},{gameType},{result},{damageTaken:0.##},{damageDealt:0.##},{scorePercent:0.##},{timeTaken:0.##},{aiLevel},{aiDifficulty:0.##},{winCondition},{playerBaseLocation},{aiBaseLocation}"); // Added group to Data Row

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
            Debug.LogError($"Error writing to log file {csvPath}: {ex.Message}");
        }
    }

    // Helper to get cardinal direction (assuming Z is North/South, X is East/West)
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

// Helper methods
private Dictionary<EntityType, int> GetDestroyedUnits(TactPlayer owner)
{
    Dictionary<EntityType, int> destroyed = new();
    foreach (EntityQuantity eq in GameMgr.inst.entityQuantities)
    {
        int remaining = EntityMgr.inst.entities.Count(e => e.entityType == eq.entityType && e.owner == owner);
        destroyed[eq.entityType] = eq.unitCount - remaining;
    }
    return destroyed;
}

private Vector3 FindBasePosition(TactPlayer owner)
{
    Entity baseEntity = EntityMgr.inst.entities
        .FirstOrDefault(e => e.owner == owner && e.entityRole == EntityRole.Base);
    return baseEntity?.transform.position ?? Vector3.zero;
}
}