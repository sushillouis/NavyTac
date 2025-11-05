using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SocialPlatforms.Impl;
[Serializable]
public class ReplayCommand
{
    public float timestamp;
    public float timeScale;
    public string commandType;
    public int[] entityIds;
    public Vector3 targetPosition;
    public int targetEntityId;
    public string targetEntityName;
    public string targetOwnerName;
    public bool add;
}
[Serializable]
public class EntityState
{
    public int entityId;
    public Vector3 position;
    public Quaternion rotation;
    public float health;
    public bool isActive;
    public string ownerName;
}
[Serializable]
public class Snapshot
{
    public float timestamp;
    public List<EntityState> entityStates = new List<EntityState>();
}
[Serializable]
public struct StringCount {
    public string key;
    public int count;
}
[Serializable]
public class ScenarioMeta {
    public int scenarioNumber;
    public int seed;
    public string startUtc;
    public float durationSeconds;
    public int totalEntities;
    public List<StringCount> byOwner = new();
    public List<StringCount> byEntityType = new();
    public List<int> playerBaseIds = new();
    public List<int> aiBaseIds = new();
    public List<int> neutralBaseIds = new();
    public Vector3 aiBasePosition;
    public Vector3 playerBasePosition;
    public Vector3 neutralBasePosition;
    public bool hasNeutralBase;
    public TrainingState trainingState;
    public float difficultyLevel;
}
public class ReplayMgr : MonoBehaviour
{
    [Serializable]
    public class ScenarioReplayData
    {
        public int scenarioNumber;
        public ScenarioMeta meta = new ScenarioMeta();
        public List<ReplayCommand> commands = new List<ReplayCommand>();
        public List<Snapshot> snapshots = new List<Snapshot>();
    }
    public static ReplayMgr inst;
    [SerializeField] private List<ScenarioReplayData> scenarioReplayDataList = new List<ScenarioReplayData>();
    private Dictionary<int, float> scenarioStartTimes = new Dictionary<int, float>();
    private Dictionary<int, float> scenarioDurations = new Dictionary<int, float>();

    private List<ReplayCommand> currentReplayCommands;
    private List<Snapshot> currentReplaySnapshots;
    public bool isReplaying = false;
    private float replayStartTime;
    private int nextCommandIndex = 0;
    private int lastAppliedSnapshotIndex = -1;
    public bool replayFinished = false;
    public int currentScenarioNumber = 1;
    public bool isRecording = false;
    [SerializeField] private float snapshotInterval = 5f; // Take snapshot every 5 seconds
    private float lastSnapshotTime = 0f;
    public bool actualPlayerWon;
    public string actualWinReason;
    public float actualTimeTaken;

    private void Awake()
    {
        inst = this;
    }

    public void StartNewScenario()
    {
        currentScenarioNumber = OpenOceanMain.inst.gamesPlayedCount + 1;
        ScenarioMeta scenarioMeta = new ScenarioMeta
        {
            scenarioNumber = currentScenarioNumber,
            seed = GameMgr.inst.selectedSeed,
            startUtc = DateTime.UtcNow.ToString("o"),
            durationSeconds = 0f,
            totalEntities = EntityMgr.inst != null ? EntityMgr.inst.entities.Count : 0,
            byOwner = new List<StringCount>(),
            byEntityType = new List<StringCount>(),
            playerBaseIds = new List<int>(),
            aiBaseIds = new List<int>(),
            neutralBaseIds = new List<int>(),
            hasNeutralBase = false
        };


        if (EntityMgr.inst != null)
        {
            foreach (var ent in EntityMgr.inst.entities)
            {
                if (EntityMgr.inst != null)
                {
                    var ents = EntityMgr.inst.entities.Where(e => e != null).ToList();

                    scenarioMeta.byOwner = ents
                        .GroupBy(e => e.owner != null ? e.owner.name : "None")
                        .Select(g => new StringCount { key = g.Key, count = g.Count() })
                        .ToList();

                    scenarioMeta.byEntityType = ents
                        .GroupBy(e => e.entityType.ToString())
                        .Select(g => new StringCount { key = g.Key, count = g.Count() })
                        .ToList();
                    // Collect base IDs based on owner
                    if (ent.entityRole == EntityRole.Base)
                    {
                        if (ent.owner == PlayerMgr.inst?.player1)
                        {
                            scenarioMeta.playerBaseIds.Add(ent.entityId);
                        }
                        else if (ent.owner == PlayerMgr.inst?.player2)
                        {
                            scenarioMeta.aiBaseIds.Add(ent.entityId);
                        }
                        else if (ent.owner == PlayerMgr.inst?.neutral || ent.isNeutral || ent.owner == null)
                        {
                            scenarioMeta.neutralBaseIds.Add(ent.entityId);
                        }
                    }
                }
                scenarioMeta.hasNeutralBase = scenarioMeta.neutralBaseIds.Count > 0;
                scenarioMeta.totalEntities = EntityMgr.inst.entities.Count;
                scenarioMeta.trainingState = OpenOceanMain.inst?.currentTrainingState ?? TrainingState.PreTest;
                scenarioMeta.difficultyLevel = GameMgr.inst.difficultyLevel;
            }
            scenarioMeta.aiBasePosition = scenarioMeta.aiBaseIds.Count > 0
                ? EntityMgr.inst.entitiesDict[scenarioMeta.aiBaseIds[0]].transform.position
                : Vector3.zero;
            scenarioMeta.playerBasePosition = scenarioMeta.playerBaseIds.Count > 0
                ? EntityMgr.inst.entitiesDict[scenarioMeta.playerBaseIds[0]].transform.position
                : Vector3.zero;
            scenarioMeta.neutralBasePosition = scenarioMeta.neutralBaseIds.Count > 0
                ? EntityMgr.inst.entitiesDict[scenarioMeta.neutralBaseIds[0]].transform.position
                : Vector3.zero;
            scenarioStartTimes[currentScenarioNumber] = Time.time;
            scenarioDurations[currentScenarioNumber] = 0f;
            lastSnapshotTime = 0f; // Reset snapshot timer

            ScenarioReplayData currentScenarioData = scenarioReplayDataList.Find(data => data.scenarioNumber == currentScenarioNumber);
            if (currentScenarioData == null)
            {
                currentScenarioData = new ScenarioReplayData { scenarioNumber = currentScenarioNumber };
                currentScenarioData.meta = scenarioMeta;
                scenarioReplayDataList.Add(currentScenarioData);
            }
            else
            {
                // Update meta in case StartNewScenario is called again for same scenario
                currentScenarioData.meta = scenarioMeta;
            }

            isRecording = true;
        }
    }

    public void RecordCommand(ReplayCommand cmd)
    {
        if (!isRecording || isReplaying)
        {
            return;
        }

        if (scenarioStartTimes.TryGetValue(currentScenarioNumber, out float startTime))
        {
            cmd.timestamp = Time.time - startTime;
        }

        if (scenarioDurations.ContainsKey(currentScenarioNumber))
        {
            scenarioDurations[currentScenarioNumber] = Mathf.Max(
                scenarioDurations[currentScenarioNumber],
                cmd.timestamp
            );
        }

        ScenarioReplayData currentData = scenarioReplayDataList.Find(data => data.scenarioNumber == currentScenarioNumber);
        if (currentData == null)
        {
            currentData = new ScenarioReplayData { scenarioNumber = currentScenarioNumber };
            scenarioReplayDataList.Add(currentData);
        }

        currentData.commands.Add(cmd);
    }

    public void StartReplay(int scenarioNumber)
    {
        if (OpenOceanMain.inst == null || OpenOceanMain.inst.lobbyState != LobbyState.Replay)
        {
            return;
        }

        ScenarioReplayData replayData = scenarioReplayDataList.Find(data => data.scenarioNumber == scenarioNumber);
        if (replayData != null)
        {
            currentReplayCommands = replayData.commands;
            currentReplaySnapshots = replayData.snapshots;
        }
        else
        {
            currentReplayCommands = new List<ReplayCommand>();
            currentReplaySnapshots = new List<Snapshot>();
        }

        if (scenarioDurations.TryGetValue(scenarioNumber, out float duration))
        {
            actualTimeTaken = duration;
        }
        else
        {
            actualTimeTaken = currentReplayCommands.Count > 0
                ? currentReplayCommands.Max(c => c.timestamp)
                : 0f;
        }

        replayFinished = false;
        Time.timeScale = 2f;

        if (AIMgr.inst != null) AIMgr.inst.StopAllCoroutines();
        if (DistanceMgr.inst != null) DistanceMgr.inst.Initialize();
        if (FogWarMgr.inst != null) FogWarMgr.inst.ResetFog();
        if (CameraMgr.inst != null) CameraMgr.inst.ResetCamera();
        if (ScoreMgr.inst != null) ScoreMgr.inst.ResetScores();
        if (MinimapMgr.inst != null) MinimapMgr.inst.ResetMinimap();
        if (LineMgr.inst != null) LineMgr.inst.DestroyAllLines();
        if (WeaponsMgr.inst != null) WeaponsMgr.inst.DestroyAllWeaponsImmediately();
        if (FXMgr.inst != null) FXMgr.inst.ResetEffects();
        if (EntityMgr.inst != null) EntityMgr.inst.Reset();
        if (ResetScene.inst != null) ResetScene.inst.ClearAllEntities();
        if (EnemyAIMgr.inst != null) EnemyAIMgr.inst.ResetAI();
        if (CaptureNeutralBaseMgr.inst != null) CaptureNeutralBaseMgr.inst.ResetCapture();
        // if (EnemyAIMgr.inst != null) EnemyAIMgr.inst.ResetLevel2State();

        
        ScenarioData scenario = GameMgr.inst.GetScenario(scenarioNumber);

        if (scenario != null)
        {
            actualPlayerWon = scenario.winLoss;
            actualWinReason = scenario.winReason;
            GameMgr.inst.InitializeScenarioFromData(scenario);
            ApplyGreyOverlays();
            isRecording = false;
            isReplaying = true;

        }
        else
        {
            return;
        }

        
        nextCommandIndex = 0;
        lastAppliedSnapshotIndex = -1;
        replayStartTime = Time.time;

        if (CameraMgr.inst != null) CameraMgr.inst.ReplayCamera();
    }

    private void Update()
    {
        // Handle periodic snapshot recording during gameplay
        if (isRecording && !isReplaying && scenarioStartTimes.ContainsKey(currentScenarioNumber))
        {
            float currentTime = Time.time - scenarioStartTimes[currentScenarioNumber];
        }

        if (isReplaying && !replayFinished && currentReplayCommands != null)
        {
            float currentReplayTime = Time.time - replayStartTime;
            while (nextCommandIndex < currentReplayCommands.Count &&
                   currentReplayCommands[nextCommandIndex].timestamp <= currentReplayTime)
            {
                ReplayCommand commandToExecute = currentReplayCommands[nextCommandIndex];
                ExecuteCommand(commandToExecute);
                nextCommandIndex++;
            }
        }
        // Check for end of replay
        // check if some one has one the game
        if (isReplaying && !replayFinished)
        {
            if (ScoreMgr.inst != null )
            {
                if(ScoreMgr.inst.playerWon == true || ScoreMgr.inst.aiWon == true)
                {
                    if (CheckWinConditionMatchesActual())
                    {
                        StopReplayAndShowScores();
                    }
                    else
                    {
                        // Mismatch in win conditions; continue replay
                        // check for commands left
                        if (nextCommandIndex >= currentReplayCommands.Count)
                        {
                            StopReplayAndShowScores();
                        }
                        // check fot time left
                        else
                        {
                            float currentReplayTime = Time.time - replayStartTime;
                            if (currentReplayTime >= actualTimeTaken)
                            {
                                StopReplayAndShowScores();
                            }
                        }
                    }
                }
            }
        }
            
    }

    private bool CheckWinConditionMatchesActual()
    {
        if (ScoreMgr.inst == null)
        {
            return false;
        }

        return (ScoreMgr.inst.playerWon == actualPlayerWon) &&
               (ScoreMgr.inst.winReason == actualWinReason);
    }

    private void StopReplayAndShowScores()
    {
        

        // Restore the original win/loss state to prevent data contamination
        if (ScoreMgr.inst != null)
        {
            ScoreMgr.inst.playerWon = actualPlayerWon;
            ScoreMgr.inst.winReason = actualWinReason;
            ScoreMgr.inst.aiWon = !actualPlayerWon;
        }

        if (OpenOceanMain.inst != null)
        {
            OpenOceanMain.inst.lobbyState = LobbyState.MultiScorePanel;
            
        }
    }

    private void ExecuteCommand(ReplayCommand cmd)
    {
        // Defensive checks: ensure cmd is not null and entityIds is safe to iterate
        if (cmd == null)
        {
            return;
        }

        int[] ids = cmd.entityIds ?? Array.Empty<int>();

        List<Entity> entities = new List<Entity>();
        foreach (int id in ids)
        {
            if (EntityMgr.inst != null && EntityMgr.inst.entitiesDict.TryGetValue(id, out Entity ent))
            {
                if (ent != null) entities.Add(ent);
            }
        }

        // If the command referenced entity ids but none were found, skip executing it
        if (entities.Count == 0 && ids.Length > 0)
        {
            return;
        }

        string cmdType = cmd.commandType ?? string.Empty;

        switch (cmdType)
        {
            case "Move":
                if (AIMgr.inst != null)
                {
                    AIMgr.inst.HandleMove(entities, cmd.targetPosition, cmd.add, isLocalCommand: false);
                }
                break;

            case "AttackMoveToPosition":
                if (AIMgr.inst != null)
                {
                    AIMgr.inst.HandleAttackMove(entities, cmd.targetPosition, null, cmd.add, isLocalCommand: false);
                }

                break;

            case "AttackMoveToEntity":
                Entity targetEnt = null;
                if (EntityMgr.inst != null)
                {
                    EntityMgr.inst.entitiesDict.TryGetValue(cmd.targetEntityId, out targetEnt);
                }

                if (targetEnt != null)
                {
                    AIMgr.inst.HandleAttackMove(entities, cmd.targetPosition, targetEnt, cmd.add, isLocalCommand: false);
                }
                break;

            default:
                // Unknown or empty commandType: ignore
                break;
        }
    }

    [Serializable]
    public class ReplayCommandList
    {
        public ScenarioMeta meta;
        public List<ReplayCommand> commands;
        public List<Snapshot> snapshots;
    }
    [Serializable]
    private class UploadPayload
    {
        public string filename;
        public string content;
    }
    public string GetLatestReplayFilePath(int scenarioNumber)
    {
        string studentID = OpenOceanMain.inst.playerName ?? "UnknownStudent";
        string gameType = GetGameTypeFolder();
        string directoryPath = Path.Combine(Application.persistentDataPath, studentID, gameType);

        if (!Directory.Exists(directoryPath))
        {
            return null;
        }

        var files = Directory.GetFiles(directoryPath, $"scenario{scenarioNumber}_*.json");
        if (files.Length > 0)
        {
            return files.OrderByDescending(f => new FileInfo(f).CreationTime).First();
        }
        return null;
    }

    public void CompleteScenario()
    {

        isRecording = false;

        if (scenarioStartTimes.TryGetValue(currentScenarioNumber, out float startTime))
        {
            scenarioDurations[currentScenarioNumber] = Time.time - startTime;
        }

        SaveScenarioCommands(currentScenarioNumber);
    }

    private void SaveScenarioCommands(int scenarioNumber)
    {
        ScenarioReplayData replayData = scenarioReplayDataList.Find(data => data.scenarioNumber == scenarioNumber);
        if (replayData == null)
        {
            Debug.LogWarning($"No replay data found for scenario {scenarioNumber}");
            return;
        }

        string studentID = OpenOceanMain.inst?.playerName ?? "UnknownStudent";
        string gameType = GetGameTypeFolder();

        string directoryPath = Path.Combine(
            Application.persistentDataPath,
            studentID,
            gameType
        );

        try
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to create directory for replay files: {e.Message}");
            // Still attempt to upload even if local save fails
        }

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string fileName = $"scenario{scenarioNumber}_{timestamp}.json";
        string filePath = Path.Combine(directoryPath, fileName);

        ReplayCommandList commandList = new ReplayCommandList
        {
            meta = replayData.meta,
            commands = replayData.commands ?? new List<ReplayCommand>(),
            snapshots = replayData.snapshots ?? new List<Snapshot>()
        };

        string json = JsonUtility.ToJson(commandList, true);

        // Save locally first (best-effort)
        try
        {
            File.WriteAllText(filePath, json);
            Debug.Log($"Saved replay locally: {filePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save replay locally: {e.Message}");
        }

        // Start upload to server (non-blocking). Upload coroutine will log success/failure.
        try
        {
            StartCoroutine(UploadToServer(fileName, json));
            Debug.Log($"Started upload coroutine for: {fileName}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to start upload coroutine: {e.Message}");
        }
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

    public void StopReplay()
    {
        isReplaying = false;
        replayFinished = true;

        if (OpenOceanMain.inst != null)
        {
            OpenOceanMain.inst.lobbyState = LobbyState.MultiScorePanel;
        }
    }

    public void ReplayLastScenario()
    {
        if (OpenOceanMain.inst == null || OpenOceanMain.inst.lobbyState != LobbyState.Replay)
        {
            return;
        }

        int lastScenarioNumber = OpenOceanMain.inst.gamesPlayedCount;
        StartReplay(lastScenarioNumber);
    }

    private void ApplyGreyOverlays()
    {
        if (EntityMgr.inst == null) return;

        List<Entity> allEntities = EntityMgr.inst.entities;
        foreach (Entity e in allEntities)
        {
            if (e.TryGetComponent<GreyOverlayGenerator>(out var greyOverlay))
            {
                greyOverlay.ApplyGreyOverlay();
            }
        }
    }

    public float GetReplayProgress()
    {
        if (!isReplaying || actualTimeTaken <= 0)
        {
            return 0f;
        }

        float currentReplayTime = Time.time - replayStartTime;
        return Mathf.Clamp01(currentReplayTime / actualTimeTaken);
    }

    // Get detailed replay statistics
    public string GetReplayStatistics()
    {
        if (currentReplayCommands == null || currentReplaySnapshots == null)
        {
            return "No replay data available";
        }
        int commandCount = currentReplayCommands.Count;
        int snapshotCount = currentReplaySnapshots.Count;
        float duration = actualTimeTaken;
        float avgCommandInterval = commandCount > 1 ? duration / (commandCount - 1) : 0f;
        return $"Commands: {commandCount}, Snapshots: {snapshotCount}, Duration: {duration:F1}s, Avg Command Interval: {avgCommandInterval:F2}s";
    }

    // Load replay data from saved JSON file
    public bool LoadReplayFromFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"Replay file not found: {filePath}");
                return false;
            }

            string json = File.ReadAllText(filePath);
            ReplayCommandList loadedData = JsonUtility.FromJson<ReplayCommandList>(json);

            if (loadedData != null)
            {
                // Create a new scenario replay data entry
                ScenarioReplayData replayData = new ScenarioReplayData
                {
                    scenarioNumber = -1, // Mark as loaded from file
                    meta = loadedData.meta ?? new ScenarioMeta(),
                    commands = loadedData.commands ?? new List<ReplayCommand>(),
                    snapshots = loadedData.snapshots ?? new List<Snapshot>()
                };

                // Clear existing data and add loaded data
                scenarioReplayDataList.Clear();
                scenarioReplayDataList.Add(replayData);

                Debug.Log($"Successfully loaded replay with {replayData.commands.Count} commands and {replayData.snapshots.Count} snapshots");
                return true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load replay file: {e.Message}");
        }

        return false;
    }
    private IEnumerator UploadToServer(string filename, string jsonContent)
    {
        string url = "https://www.cse.unr.edu/~yvohra/Study/upload/upload.php";

        // Use the payload class to correctly serialize the JSON
        UploadPayload payload = new UploadPayload
        {
            filename = filename,
            content = jsonContent
        };
        string jsonPayload = JsonUtility.ToJson(payload);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(jsonBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.certificateHandler = new CustomCertificateHandler(); // Handles self-signed certs

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // Check the server's text response for "SUCCESS"
                if (request.downloadHandler.text.StartsWith("SUCCESS"))
                {
                    Debug.Log($"Replay upload successful! Server response: {request.downloadHandler.text}");
                }
                else
                {
                    Debug.LogWarning($"Replay upload complete, but server reported an error: {request.downloadHandler.text}");
                }
            }
            else
            {
                Debug.LogError($"Replay upload failed! Error: {request.error} | Server response: {request.downloadHandler.text}");
            }
        }
    }


    public class CustomCertificateHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true;
        }
    }
}