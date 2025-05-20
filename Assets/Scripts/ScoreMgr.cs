using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;
using UnityEngine.Networking;
using System.Collections;// Added for Path and Directory operations

/// <summary>
/// Manages the game score, victory conditions, and logging of game data.
/// </summary>
public class ScoreMgr : MonoBehaviour
{
    public static ScoreMgr inst; // Singleton instance of ScoreMgr
    public float damageDealt; // Total damage dealt by the player
    public float damageTaken; // Total damage taken by the player
    public bool playerWon; // Flag indicating if the player won
    public float score; // Calculated score for the game
    public bool aiWon; // Flag indicating if the AI won
    public String winReason; // Reason for winning (e.g., "All enemy units destroyed")

    private string sessionStartTimeString; // To store session start time, used in log filenames
    private const string CommonLogFileName = "AllGamesLog.csv"; // Name for the common log file for all games

    private static readonly List<string> generalFeedbacks = new List<string>
    {
        "JARI USVs can force the enemy to reveal positions — use them early.",
        "Attack-Move (Space + Right Click) prevents surprise deaths.",
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

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// Implements the Singleton pattern.
    /// </summary>
    private void Awake()
    {
        // Singleton pattern implementation
        if (inst != null && inst != this)
        {
            Destroy(gameObject); // Destroy duplicate instance
        }
        else
        {
            inst = this; // Set the singleton instance
            // Store session start time in a format suitable for filenames and readability
            sessionStartTimeString = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm");
        }
    }

    /// <summary>
    /// Checks if a victory condition has been met and proceeds with post-game actions.
    /// </summary>
    public void CheckVictory()
    {
        // If neither player nor AI has won, do nothing
        if (!playerWon && !aiWon) return;

        // Set the lobby state to show the score panel
        OpenOceanMain.inst.lobbyState = OpenOceanMain.LobbyState.ScorePanel;

        // Calculate the score based on win status and damage ratio
        score = (float)(0.5 * (playerWon ? 1 : 0)) * 100 + 0.5f * (damageDealt / (damageDealt + damageTaken)) * 100;
        //Debug.Log($"Score: {score}"); // Log the calculated score

        LogGameData(); // Log detailed game data to CSV files
        UpdateScoreDisplay(); // Update the UI elements with score and game stats
        LogVictoryMessage(); // Log a simple victory/defeat message to the console
        FXMgr.inst.ResetEffects(); // Reset any visual effects

    }

    /// <summary>
    /// Updates the UI elements on the score panel with the game results.
    /// </summary>
    private void UpdateScoreDisplay()
    {
        // Update damage dealt text
        if (OpenOceanMain.inst.damageDealtText != null)
            OpenOceanMain.inst.damageDealtText.text = $"{damageDealt:0}";

        // Update damage taken text
        if (OpenOceanMain.inst.damageTakenText != null)
            OpenOceanMain.inst.damageTakenText.text = $"{damageTaken:0}";

        // Update winner text
        if (OpenOceanMain.inst.winnerText != null)
            OpenOceanMain.inst.winnerText.text = playerWon ? "Player Victory!" : "AI Victory!";

        // Update score text
        if (OpenOceanMain.inst.scoreText != null)
            OpenOceanMain.inst.scoreText.text = $"{score:0.##}%";

        // Update player's destroyed units text
        if (OpenOceanMain.inst.ourUnitsDestroyedText != null)
            OpenOceanMain.inst.ourUnitsDestroyedText.text = $"{GetDestroyedUnits(PlayerMgr.inst.localPlayer).Values.Sum()}";

        // Update AI's destroyed units text
        if (OpenOceanMain.inst.enemyUnitsDestroyedText != null)
            OpenOceanMain.inst.enemyUnitsDestroyedText.text = $"{GetDestroyedUnits(PlayerMgr.inst.players.Find(p => p.name == "Ai")).Values.Sum()}";

        // Update win condition text
        if (OpenOceanMain.inst.winConditionText != null)
            OpenOceanMain.inst.winConditionText.text = winReason;

        // Update feedback text (Adaptive mode only)
        if (OpenOceanMain.inst.feedbackText != null)
        {
            bool isAdaptive = GetGameTypeFolder() == "Adaptive";
            List<string> selectedFeedbacks = new();

            if (isAdaptive)
            {
                // Select specific feedback based on score and win condition
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
                    else // 50-70 (Note: score for win is >= 50)
                    {
                        selectedFeedbacks.Add("Retreat and regroup instead of losing all at once.");
                        selectedFeedbacks.Add("Keep Destroyers protected behind lighter units.");
                    }
                }
                else // Loss (score for loss will be < 50)
                {
                    selectedFeedbacks.Add("Avoid moving DDG51s without a scout — they’re not expendable.");
                    selectedFeedbacks.Add("Use terrain and spacing to avoid ambushes.");
                    selectedFeedbacks.Add("Don’t clump Destroyers — it makes them vulnerable to area attacks.");
                    selectedFeedbacks.Add("Send scouts before committing large units.");
                }

                // Add winReason-specific feedback
                if (winReason.Contains("baseDestroyed"))
                    selectedFeedbacks.Add("Try combining base attacks with flanking units to distract defenders.");

                // Add general feedback if needed to reach up to 3 items
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
                    var feedbacksToDisplay = selectedFeedbacks.Take(3).Select(fb => "• " + fb).ToList();
                    OpenOceanMain.inst.feedbackText.text = "Feedback\n" + string.Join("\n", feedbacksToDisplay);
                }
                else
                {
                    OpenOceanMain.inst.feedbackText.text = ""; // No feedback to show
                }
            }
            else
            {
                OpenOceanMain.inst.feedbackText.text = ""; // Hide feedback for non-adaptive
            }
        }
    }


    /// <summary>
    /// Logs a victory or defeat message to the console.
    /// </summary>
    private void LogVictoryMessage()
    {
        string message = playerWon ?
            $"PLAYER VICTORY! Damage Dealt: {damageDealt} | Taken: {damageTaken}" :
            $"AI VICTORY! Damage Dealt: {damageDealt} | Taken: {damageTaken}";

        //Debug.Log(message); // Log the message
    }

    /// <summary>
    /// Resets the score-related variables for a new game.
    /// </summary>
    public void ResetScores()
    {
        damageDealt = 0;
        damageTaken = 0;
        playerWon = false;
        aiWon = false;
    }

    /// <summary>
    /// Determines the folder name for logging based on the player code.
    /// This helps categorize logs (e.g., Adaptive, Non-Adaptive).
    /// </summary>
    /// <returns>A string representing the folder name for the game type.</returns>
    private string GetGameTypeFolder() // Renamed for clarity, returns descriptive folder name
    {
        string playerCode = OpenOceanMain.inst.playerCode;
        if (playerCode == "AAA") return "Adaptive";
        if (playerCode == "BBB") return "Non-Adaptive";
        if (playerCode == "ABC") return "Pre-Test";
        if (playerCode == "XYZ") return "Post-Test";
        return "UnknownGameType"; // Default or fallback if player code is not recognized
    }

    /// <summary>
    /// Gathers all relevant game data and logs it to CSV files.
    /// Logs to both a student-specific file and a common log file.
    /// </summary>
    public void LogGameData()
    {
        // 0. Date & Time
        string dateTimeNow = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); // Current date and time

        // 1. Basic Info
        string studentID = OpenOceanMain.inst.playerName ?? "UnknownStudent"; // Player's ID, defaults if null
        string group = "Non-Adaptive"; // Default group assignment
        // Determine group (Adaptive/Non-Adaptive) based on student ID pattern
        if (studentID != null && studentID.StartsWith("Student", StringComparison.OrdinalIgnoreCase))
        {
            string numericPart = studentID.Substring("Student".Length);
            if (int.TryParse(numericPart, out int studentIdNumber))
            {
                group = (studentIdNumber % 2 == 0) ? "Adaptive" : "Non-Adaptive"; // Even ID = Adaptive, Odd ID = Non-Adaptive
            }
            else
            {
                //Debug.LogWarning($"Numeric part of StudentID '{numericPart}' could not be parsed. Defaulting group to 'Non-Adaptive'.");
                group = "Non-Adaptive";
            }
        }
        else if (studentID != "UnknownStudent") // Avoid warning for default "UnknownStudent"
        {
            //Debug.LogWarning($"StudentID '{studentID}' does not follow 'StudentX' pattern. Defaulting group to 'Non-Adaptive'.");
        }


        string gameType = OpenOceanMain.inst.currentTrainingState.ToString(); // Current game mode or training state
        string result = playerWon ? "Win" : "Loss"; // Game result
        float scorePercent = score; // Calculated score percentage

        // 2. Damage & Score
        float damageTaken = this.damageTaken; // Damage taken by player
        float damageDealt = this.damageDealt; // Damage dealt by player

        // 3. Unit Counts (Initial counts from GameMgr)
        // Dictionary mapping entity type to its initial count
        Dictionary<EntityType, int> initialUnitCounts = GameMgr.inst.entityQuantities
            .ToDictionary(eq => eq.entityType, eq => eq.unitCount);


        // 4. Destroyed Units (Initial Counts - Remaining)
        // Calculate destroyed units for the player
        Dictionary<EntityType, int> destroyedPlayerUnits = GetDestroyedUnits(PlayerMgr.inst.localPlayer);
        TactPlayer aiPlayer = PlayerMgr.inst.players.Find(p => p.name == "Ai"); // Find the AI player instance
        // Calculate destroyed units for the AI
        Dictionary<EntityType, int> destroyedAIUnits = (aiPlayer != null) ? GetDestroyedUnits(aiPlayer) : new Dictionary<EntityType, int>();


        // 5. Base Locations (Cardinal Directions)
        // Determine player and AI base locations in cardinal directions
        string playerBaseLocation = GetCardinalDirection(GameMgr.inst.posPlayer1);
        string aiBaseLocation = GetCardinalDirection(GameMgr.inst.posPlayer2);


        // 6. Metadata
        string winCondition = winReason; // Reason for winning
        float timeTaken = OpenOceanMain.inst.playSessionDuration; // Time elapsed for the play session
        int aiLevel = (EnemyAIMgr.inst != null) ? EnemyAIMgr.inst.currentLevel : -1; // Current AI difficulty level
        float aiDifficulty = GameMgr.inst.difficultyLevel; // AI difficulty setting from GameMgr

        // Prepare CSV path for student-specific file
        string gameTypeFolder = GetGameTypeFolder(); // Get the folder name based on game type (e.g., "Adaptive")

        // Construct filename for student-specific log, incorporating student ID, session start time, and game type folder name
        string studentFileName = $"{studentID}_{sessionStartTimeString}_{gameTypeFolder}.csv";
        // Construct directory path for student-specific log
        string studentDirectory = Path.Combine(Application.persistentDataPath, gameTypeFolder);
        string studentCsvPath = Path.Combine(studentDirectory, studentFileName); // Full path to student-specific CSV

        // Prepare CSV path for common file (logs all games)
        string commonCsvPath = Path.Combine(Application.persistentDataPath, CommonLogFileName);

        // Log to student-specific file
        WriteToCsv(studentCsvPath, studentDirectory, dateTimeNow, studentID, group, gameType, result, damageTaken, damageDealt, scorePercent, timeTaken, aiLevel, aiDifficulty, winCondition, playerBaseLocation, aiBaseLocation, initialUnitCounts, destroyedPlayerUnits, destroyedAIUnits);

        // Log to common file
        WriteToCsv(commonCsvPath, Application.persistentDataPath, dateTimeNow, studentID, group, gameType, result, damageTaken, damageDealt, scorePercent, timeTaken, aiLevel, aiDifficulty, winCondition, playerBaseLocation, aiBaseLocation, initialUnitCounts, destroyedPlayerUnits, destroyedAIUnits);
    }

    /// <summary>
    /// Writes the collected game data to a specified CSV file.
    /// Creates the directory and file if they don't exist.
    /// Appends a header row if the file is new or empty.
    /// </summary>
    /// <param name="csvPath">The full path to the CSV file.</param>
    /// <param name="directoryPath">The path to the directory where the CSV file will be stored.</param>
    /// <param name="dateTimeNow">Current date and time string.</param>
    /// <param name="studentID">Player's ID.</param>
    /// <param name="group">Player's group (e.g., Adaptive/Non-Adaptive).</param>
    /// <param name="gameType">Type of game played.</param>
    /// <param name="result">Game result (Win/Loss).</param>
    /// <param name="damageTaken">Damage taken by the player.</param>
    /// <param name="damageDealt">Damage dealt by the player.</param>
    /// <param name="scorePercent">Player's score percentage.</param>
    /// <param name="timeTaken">Time taken to complete the game.</param>
    /// <param name="aiLevel">AI difficulty level.</param>
    /// <param name="aiDifficulty">AI difficulty setting.</param>
    /// <param name="winCondition">Reason for winning.</param>
    /// <param name="playerBaseLocation">Cardinal direction of player's base.</param>
    /// <param name="aiBaseLocation">Cardinal direction of AI's base.</param>
    /// <param name="initialUnitCounts">Dictionary of initial unit counts by type.</param>
    /// <param name="destroyedPlayerUnits">Dictionary of player's destroyed units by type.</param>
    /// <param name="destroyedAIUnits">Dictionary of AI's destroyed units by type.</param>
    private void WriteToCsv(string csvPath, string directoryPath, string dateTimeNow, string studentID, string group, string gameType, string result, float damageTaken, float damageDealt, float scorePercent, float timeTaken, int aiLevel, float aiDifficulty, string winCondition, string playerBaseLocation, string aiBaseLocation, Dictionary<EntityType, int> initialUnitCounts, Dictionary<EntityType, int> destroyedPlayerUnits, Dictionary<EntityType, int> destroyedAIUnits)
    {
        try
        {
            // Ensure the directory exists, create it if not
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            bool fileExists = File.Exists(csvPath); // Check if the CSV file already exists
            bool isEmpty = !fileExists || new FileInfo(csvPath).Length == 0; // Check if the file is new or empty


            using (var writer = new StreamWriter(csvPath, true)) // Open the CSV file in append mode
            {
                // Get a distinct, ordered list of all entity types involved in the game
                var allEntityTypes = initialUnitCounts.Keys
                                    .Union(destroyedPlayerUnits.Keys)
                                    .Union(destroyedAIUnits.Keys)
                                    .Distinct()
                                    .OrderBy(et => et.ToString());

                if (isEmpty)
                {
                    // Write header row if file doesn't exist or is empty
                    writer.Write("DateTime,StudentID,Group,GameType,Result,DamageTaken,DamageDealt,ScorePercent,TimeTaken,AILevel,AIDifficulty,WinCondition,PlayerBaseLocation,AIBaseLocation");

                    // Add headers for each unit type (initial, player destroyed, AI destroyed)
                    foreach (var unitType in allEntityTypes)
                    {
                        writer.Write($",Initial_{unitType},DestroyedPlayer_{unitType},DestroyedAI_{unitType}");
                    }
                    writer.WriteLine(); // End the header line
                }

                // Write data row
                writer.Write($"{dateTimeNow},{studentID},{group},{gameType},{result},{damageTaken:0.##},{damageDealt:0.##},{scorePercent:0.##},{timeTaken:0.##},{aiLevel},{aiDifficulty:0.##},{winCondition},{playerBaseLocation},{aiBaseLocation}");

                // Write data for each unit type
                foreach (var unitType in allEntityTypes)
                {
                    int initialCount = initialUnitCounts.TryGetValue(unitType, out var ic) ? ic : 0;
                    int destroyedPlayerCount = destroyedPlayerUnits.TryGetValue(unitType, out var dpc) ? dpc : 0;
                    int destroyedAICount = destroyedAIUnits.TryGetValue(unitType, out var dac) ? dac : 0;

                    writer.Write($",{initialCount},{destroyedPlayerCount},{destroyedAICount}");
                }
                writer.WriteLine(); // End the data row
            }
            StartCoroutine(UploadToServer(csvPath));
            //Debug.Log($"Game data logged to {csvPath}"); // Confirmation log
        }
        catch (System.Exception ex)
        {
            //Debug.LogError($"Error writing to log file {csvPath}: {ex.Message}"); // Log any errors during file writing
        }
    }


    /// <summary>
    /// Helper method to determine the cardinal direction of a position.
    /// Assumes Z is North/South and X is East/West.
    /// </summary>
    /// <param name="position">The 3D position vector.</param>
    /// <param name="threshold">A threshold to determine if the position is close to the center.</param>
    /// <returns>A string representing the cardinal direction (e.g., "North", "East", "Center").</returns>
    private string GetCardinalDirection(Vector3 position, float threshold = 10.0f)
    {
        if (position == Vector3.zero) return "Unknown"; // If position is zero vector, return "Unknown"

        float absX = Mathf.Abs(position.x); // Absolute X coordinate
        float absZ = Mathf.Abs(position.z); // Absolute Z coordinate

        // If both X and Z are within the threshold, consider it "Center"
        if (absX < threshold && absZ < threshold) return "Center";

        // Determine primary direction based on which coordinate (X or Z) is larger
        if (absZ >= absX)
        {
            return position.z > 0 ? "North" : "South"; // Positive Z is North, negative Z is South
        }
        else
        {
            return position.x > 0 ? "East" : "West"; // Positive X is East, negative X is West
        }
    }

    /// <summary>
    /// Helper method to calculate the number of destroyed units for a given player.
    /// </summary>
    /// <param name="owner">The player (TactPlayer) whose destroyed units are to be counted.</param>
    /// <returns>A dictionary mapping EntityType to the count of destroyed units of that type.</returns>
    private Dictionary<EntityType, int> GetDestroyedUnits(TactPlayer owner)
    {
        Dictionary<EntityType, int> destroyed = new(); // Initialize dictionary for destroyed units
        // Null checks for required managers and lists to prevent errors
        if (GameMgr.inst == null || GameMgr.inst.entityQuantities == null || EntityMgr.inst == null || EntityMgr.inst.entities == null)
        {
            //Debug.LogError("Required managers or lists are null in GetDestroyedUnits.");
            return destroyed; // Return empty dictionary to prevent further errors
        }
        // Iterate through the initial entity quantities defined in GameMgr
        foreach (EntityQuantity eq in GameMgr.inst.entityQuantities)
        {
            // Count remaining entities of the current type owned by the specified player
            int remaining = EntityMgr.inst.entities.Count(e => e.entityType == eq.entityType && e.owner == owner);
            // Calculate destroyed units: initial count - remaining count
            destroyed[eq.entityType] = eq.unitCount - remaining;
        }
        return destroyed; // Return the dictionary of destroyed units
    }

    /// <summary>
    /// Helper method to find the position of a player's base.
    /// </summary>
    /// <param name="owner">The player (TactPlayer) whose base position is to be found.</param>
    /// <returns>The Vector3 position of the base, or Vector3.zero if not found or if managers are null.</returns>
    private Vector3 FindBasePosition(TactPlayer owner)
    {
        // Null checks for EntityMgr and its entities list
        if (EntityMgr.inst == null || EntityMgr.inst.entities == null)
        {
            //Debug.LogError("EntityMgr or entities list is null in FindBasePosition.");
            return Vector3.zero; // Return zero vector if essential components are missing
        }
        // Find the first entity that is owned by the player and has the role of "Base"
        Entity baseEntity = EntityMgr.inst.entities
            .FirstOrDefault(e => e.owner == owner && e.entityRole == EntityRole.Base);
        // Return the base entity's position, or Vector3.zero if no base entity is found
        return baseEntity?.transform.position ?? Vector3.zero;
    }
    private IEnumerator UploadToServer(string csvPath)
{
    string url = "https://www.cse.unr.edu/~yvohra/Study/upload.php";
    string csvContent = File.ReadAllText(csvPath);
    string filename = Path.GetFileName(csvPath);

    // Create a JSON payload
    string jsonPayload = $"{{\"filename\":\"{filename}\", \"content\":\"{csvContent}\"}}";

    // Send as raw JSON
    using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
    {
        byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(jsonBytes);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.certificateHandler = new CustomCertificateHandler(); // Bypass SSL if needed

        yield return request.SendWebRequest();

        Debug.Log($"Response Code: {request.responseCode}");
        Debug.Log($"Response: {request.downloadHandler.text}");
    }
}

// Add this class to bypass SSL errors
public class CustomCertificateHandler : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        return true; // Accept all certificates (remove in production)
    }
}
}
