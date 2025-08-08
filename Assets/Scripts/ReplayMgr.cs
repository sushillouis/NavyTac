using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

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

public class ReplayMgr : MonoBehaviour
{
    [Serializable]
    public class ScenarioReplayData
    {
        public int scenarioNumber;
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
    private bool replayFinished = false;
    public int currentScenarioNumber = 1;
    public bool isRecording = false;

    // Hybrid replay settings
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

        scenarioStartTimes[currentScenarioNumber] = Time.time;
        scenarioDurations[currentScenarioNumber] = 0f;
        lastSnapshotTime = 0f; // Reset snapshot timer

        ScenarioReplayData currentScenarioData = scenarioReplayDataList.Find(data => data.scenarioNumber == currentScenarioNumber);
        if (currentScenarioData == null)
        {
            currentScenarioData = new ScenarioReplayData { scenarioNumber = currentScenarioNumber };
            scenarioReplayDataList.Add(currentScenarioData);
        }

        isRecording = true;

        TakeSnapshot();
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

    private void TakeSnapshot()
    {
        if (!isRecording || isReplaying || EntityMgr.inst == null)
        {
            return;
        }

        float currentTime = Time.time;
        if (scenarioStartTimes.TryGetValue(currentScenarioNumber, out float startTime))
        {
            float timestamp = currentTime - startTime;
            
            ScenarioReplayData currentData = scenarioReplayDataList.Find(data => data.scenarioNumber == currentScenarioNumber);
            if (currentData == null)
            {
                currentData = new ScenarioReplayData { scenarioNumber = currentScenarioNumber };
                scenarioReplayDataList.Add(currentData);
            }

            Snapshot snapshot = new Snapshot
            {
                timestamp = timestamp
            };

            // Capture state of all entities
            foreach (Entity entity in EntityMgr.inst.entities)
            {
                if (entity != null && entity.gameObject != null)
                {
                    EntityState entityState = new EntityState
                    {
                        entityId = entity.entityId,
                        position = entity.transform.position,
                        rotation = entity.transform.rotation,
                        health = entity.health,
                        isActive = entity.gameObject.activeSelf,
                        ownerName = entity.owner?.name ?? "None"
                    };
                    snapshot.entityStates.Add(entityState);
                }
            }

            currentData.snapshots.Add(snapshot);
            lastSnapshotTime = timestamp;
        }
    }

    private void ApplyNearestSnapshot(float currentReplayTime)
    {
        if (currentReplaySnapshots == null || currentReplaySnapshots.Count == 0 || EntityMgr.inst == null)
        {
            return;
        }

        // Find the most recent snapshot that is not in the future
        Snapshot nearestSnapshot = null;
        for (int i = currentReplaySnapshots.Count - 1; i >= 0; i--)
        {
            if (currentReplaySnapshots[i].timestamp <= currentReplayTime)
            {
                nearestSnapshot = currentReplaySnapshots[i];
                break;
            }
        }

        if (nearestSnapshot != null && lastAppliedSnapshotIndex != currentReplaySnapshots.IndexOf(nearestSnapshot))
        {
            ApplySnapshot(nearestSnapshot);
            lastAppliedSnapshotIndex = currentReplaySnapshots.IndexOf(nearestSnapshot);
        }
    }

    private void ApplySnapshot(Snapshot snapshot)
    {
        if (snapshot == null || EntityMgr.inst == null)
        {
            return;
        }

        foreach (EntityState entityState in snapshot.entityStates)
        {
            if (EntityMgr.inst.entitiesDict.TryGetValue(entityState.entityId, out Entity entity))
            {
                if (entity != null && entity.gameObject != null)
                {
                    // Apply position and rotation
                    entity.transform.position = entityState.position;
                    entity.transform.rotation = entityState.rotation;
                    
                    // Apply health
                    entity.health = entityState.health;
                    
                    // Apply active state
                    entity.gameObject.SetActive(entityState.isActive);
                }
            }
        }
    }

    public void LogButtonPressed()
    {
        if (OpenOceanMain.inst != null)
        {
            OpenOceanMain.inst.lobbyState = LobbyState.Replay;
        }
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
        Time.timeScale = 1f;

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
        // if (EnemyAIMgr.inst != null) EnemyAIMgr.inst.ResetLevel2State();

        isRecording = false;

        ScenarioData scenario = GameMgr.inst.GetScenario(scenarioNumber);

        if (scenario != null)
        {
            actualPlayerWon = scenario.winLoss;
            actualWinReason = scenario.winReason;
            GameMgr.inst.InitializeScenarioFromData(scenario);
            
            // Apply grey overlays to all entities for replay
            ApplyGreyOverlays();
        }
        else
        {
            return;
        }

        isReplaying = true;
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
            if (currentTime - lastSnapshotTime >= snapshotInterval)
            {
                TakeSnapshot();
            }
        }

        if (isReplaying && !replayFinished && currentReplayCommands != null)
        {
            float currentReplayTime = Time.time - replayStartTime;

            if (currentReplayTime > actualTimeTaken + 10f)
            {
                StopReplayAndShowScores();
                return;
            }

            // Apply snapshots for state correction
            ApplyNearestSnapshot(currentReplayTime);

            while (nextCommandIndex < currentReplayCommands.Count &&
                   currentReplayCommands[nextCommandIndex].timestamp <= currentReplayTime)
            {
                ReplayCommand commandToExecute = currentReplayCommands[nextCommandIndex];
                ExecuteCommand(commandToExecute);
                nextCommandIndex++;
            }

            if (nextCommandIndex >= currentReplayCommands.Count && !replayFinished)
            {
                // End the replay when all commands are executed, regardless of win condition match
                // We'll restore the correct win state in StopReplayAndShowScores()
                StopReplayAndShowScores();
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
        isReplaying = false;
        replayFinished = true;

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
        if(cmd.commandType == "TimeScaleChange")
        {
            Time.timeScale = cmd.timeScale;
            return;
        }
        List<Entity> entities = new List<Entity>();
        foreach (int id in cmd.entityIds)
        {
            if (EntityMgr.inst.entitiesDict.TryGetValue(id, out Entity ent))
            {
                entities.Add(ent);
            }
        }

        if (entities.Count == 0 && cmd.entityIds.Length > 0)
        {
            return;
        }
        Time.timeScale = cmd.timeScale;
        switch (cmd.commandType)
        {
            case "Move":
                AIMgr.inst.HandleMove(entities, cmd.targetPosition, cmd.add, isLocalCommand: false);
                
                break;

            case "AttackMoveToPosition":
                // AIMgr.inst.HandleAttackMove(entities, cmd.targetPosition, null, cmd.add, isLocalCommand: false);

                break;

            case "AttackMoveToEntity":
                Entity targetEnt = EntityMgr.inst.entitiesDict.TryGetValue(cmd.targetEntityId, out Entity tEnt) ? tEnt : null;
                if (targetEnt != null)
                {
                    // AIMgr.inst.HandleAttackMove(entities, cmd.targetPosition, targetEnt, cmd.add, isLocalCommand: false);
                }
                break;

            default:
                break;
        }
    }

    [Serializable]
    public class ReplayCommandList
    {
        public List<ReplayCommand> commands;
        public List<Snapshot> snapshots;
    }

    public void CompleteScenario()
    {
        // Take final snapshot before stopping recording
        if (isRecording)
        {
            TakeSnapshot();
        }
        
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
            return;
        }

        string studentID = OpenOceanMain.inst.playerName ?? "UnknownStudent";
        string gameType = GetGameTypeFolder();

        string directoryPath = Path.Combine(
            Application.persistentDataPath,
            studentID,
            gameType
        );

        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string fileName = $"scenario{scenarioNumber}_{timestamp}.json";
        string filePath = Path.Combine(directoryPath, fileName);

        ReplayCommandList commandList = new ReplayCommandList
        {
            commands = replayData.commands,
            snapshots = replayData.snapshots
        };
        string json = JsonUtility.ToJson(commandList, true);
        File.WriteAllText(filePath, json);
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

    // Hybrid replay scrubbing support - jump to specific time
    public void ScrubToTime(float targetTime)
    {
        if (!isReplaying || currentReplayCommands == null || currentReplaySnapshots == null)
        {
            return;
        }

        // Clamp target time to valid range
        targetTime = Mathf.Clamp(targetTime, 0f, actualTimeTaken);

        // Find the closest snapshot before or at the target time
        Snapshot targetSnapshot = null;
        int snapshotIndex = -1;
        for (int i = currentReplaySnapshots.Count - 1; i >= 0; i--)
        {
            if (currentReplaySnapshots[i].timestamp <= targetTime)
            {
                targetSnapshot = currentReplaySnapshots[i];
                snapshotIndex = i;
                break;
            }
        }

        // Apply the snapshot to reset game state
        if (targetSnapshot != null)
        {
            ApplySnapshot(targetSnapshot);
            lastAppliedSnapshotIndex = snapshotIndex;
        }

        // Find the first command after the snapshot
        nextCommandIndex = 0;
        float snapshotTime = targetSnapshot?.timestamp ?? 0f;
        
        for (int i = 0; i < currentReplayCommands.Count; i++)
        {
            if (currentReplayCommands[i].timestamp > snapshotTime)
            {
                nextCommandIndex = i;
                break;
            }
        }

        // Execute commands from snapshot time to target time
        while (nextCommandIndex < currentReplayCommands.Count &&
               currentReplayCommands[nextCommandIndex].timestamp <= targetTime)
        {
            ExecuteCommand(currentReplayCommands[nextCommandIndex]);
            nextCommandIndex++;
        }

        // Update replay start time to account for the scrub
        replayStartTime = Time.time - targetTime;
    }

    // Get total number of snapshots for current scenario
    public int GetSnapshotCount()
    {
        return currentReplaySnapshots?.Count ?? 0;
    }

    // Get snapshot timestamps for UI scrubbing controls
    public List<float> GetSnapshotTimestamps()
    {
        List<float> timestamps = new List<float>();
        if (currentReplaySnapshots != null)
        {
            foreach (Snapshot snapshot in currentReplaySnapshots)
            {
                timestamps.Add(snapshot.timestamp);
            }
        }
        return timestamps;
    }

    // Get current replay progress (0.0 to 1.0)
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
}
