using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class ReplayCommand
{
    public float timestamp;
    public string commandType;
    public int[] entityIds;
    public Vector3 targetPosition;
    public int targetEntityId;
    public bool add;
}

public class ReplayMgr : MonoBehaviour
{
    [Serializable]
    public class ScenarioReplayData
    {
        public int scenarioNumber;
        public List<ReplayCommand> commands = new List<ReplayCommand>();
    }

    public static ReplayMgr inst;
    // private readonly Dictionary<int, List<ReplayCommand>> scenarioCommands = new();
    [SerializeField] private List<ScenarioReplayData> scenarioReplayDataList = new List<ScenarioReplayData>();
    private List<ReplayCommand> currentReplayCommands;
    public bool isReplaying = false;
    private float replayStartTime;
    private int nextCommandIndex = 0;
    private bool replayFinished = false;
    public int currentScenarioNumber = 1;
    public bool isRecording = false;

    public bool actualPlayerWon;
    public string actualWinReason;
    public float actualTimeTaken;

    private void Awake()
    {
        Debug.Log("ReplayMgr: Awake called.");
        if (inst == null)
        {
            inst = this;
            Debug.Log("ReplayMgr: Instance set.");
        }
        else
        {
            Debug.LogWarning("ReplayMgr: Instance already exists. Destroying this new one.");
            Destroy(gameObject);
        }
    }

    public void RecordCommand(ReplayCommand cmd)
    {
        Debug.Log($"ReplayMgr: RecordCommand called for scenario {currentScenarioNumber}. isRecording: {isRecording}, isReplaying: {isReplaying}");
        if (!isRecording || isReplaying)
        {
            Debug.Log($"ReplayMgr: Not recording or currently replaying. Command not recorded. isRecording: {isRecording}, isReplaying: {isReplaying}");
            return;
        }
        

        ScenarioReplayData currentData = scenarioReplayDataList.Find(data => data.scenarioNumber == currentScenarioNumber);
        if (currentData == null)
        {
            // This case should be less common if StartNewScenario correctly initializes the data.
            Debug.LogWarning($"ReplayMgr: ScenarioReplayData for scenario {currentScenarioNumber} not found by RecordCommand. Creating it now. Ensure StartNewScenario was called and initialized data for this scenario number.");
            currentData = new ScenarioReplayData { scenarioNumber = currentScenarioNumber };
            scenarioReplayDataList.Add(currentData);
            Debug.Log($"ReplayMgr: Created and added new ScenarioReplayData for scenario {currentScenarioNumber} within RecordCommand.");
        }
        else
        {
            Debug.Log($"ReplayMgr: Found existing ScenarioReplayData for scenario {currentScenarioNumber}. Command count before adding: {currentData.commands.Count}");
        }
        
        currentData.commands.Add(cmd);

        Debug.Log($"Recorded command for scenario {currentScenarioNumber}: {cmd.commandType} at {cmd.timestamp}. New command count: {currentData.commands.Count}");
    }

    public void StartNewScenario()
    {
        currentScenarioNumber = OpenOceanMain.inst.gamesPlayedCount + 1 ;
        
        ScenarioReplayData currentScenarioData = scenarioReplayDataList.Find(data => data.scenarioNumber == currentScenarioNumber);
        if (currentScenarioData == null)
        {
            currentScenarioData = new ScenarioReplayData { scenarioNumber = currentScenarioNumber };
            scenarioReplayDataList.Add(currentScenarioData);
            Debug.Log($"ReplayMgr: Initialized and added ScenarioReplayData for new scenario {currentScenarioNumber}. List count: {scenarioReplayDataList.Count}");
        }
        else
        {
            // If data for this scenario number already exists, new commands will be appended to it by RecordCommand.
            // If a fresh start for this scenario number is needed (e.g., clear old commands), that logic would go here.
            // Example: currentScenarioData.commands.Clear();
            Debug.Log($"ReplayMgr: ScenarioReplayData for scenario {currentScenarioNumber} already exists. List count: {scenarioReplayDataList.Count}. New commands will be appended.");
        }
        
        isRecording = true; // Enable recording for the current scenario.
        Debug.Log($"Starting recording for scenario {currentScenarioNumber}.");
    }


    public void LogButtonPressed()
    {
        Debug.Log("ReplayMgr: LogButtonPressed called.");
        if (OpenOceanMain.inst != null)
        {
            OpenOceanMain.inst.lobbyState = LobbyState.Replay;
            Debug.Log("ReplayMgr: Set OpenOceanMain lobby state to Replay.");
        }
        else
        {
            Debug.LogWarning("ReplayMgr: OpenOceanMain instance not found. Cannot set lobby state to Replay.");
        }
    }

    public void StartReplay(int scenarioNumber)
    {
        Debug.Log($"ReplayMgr: StartReplay called for scenario {scenarioNumber}.");
        if (OpenOceanMain.inst == null || OpenOceanMain.inst.lobbyState != LobbyState.Replay)
        {
            Debug.LogWarning($"ReplayMgr: Cannot start replay - OpenOceanMain instance is null or lobby state is not Replay. Current state: {(OpenOceanMain.inst != null ? OpenOceanMain.inst.lobbyState.ToString() : "N/A")}");
            return;
        }

        ScenarioReplayData replayData = scenarioReplayDataList.Find(data => data.scenarioNumber == scenarioNumber);
        if (replayData != null)
        {
            currentReplayCommands = replayData.commands;
            Debug.Log($"ReplayMgr: Starting replay for scenario {scenarioNumber} with {currentReplayCommands.Count} commands.");
        }
        else
        {
            Debug.LogWarning($"ReplayMgr: No replay commands found for scenario {scenarioNumber}. Creating empty list.");
            currentReplayCommands = new List<ReplayCommand>();
        }

        replayFinished = false;
        Time.timeScale = 1f;
        Debug.Log("ReplayMgr: Replay flags reset, Time.timeScale set to 1.");

        Debug.Log("ReplayMgr: Resetting game systems for replay.");
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
        if (ResetScene.inst != null) ResetScene.inst.ClearAllEntities(); else Debug.LogWarning("ReplayMgr: ResetScene.inst is null.");
        if(EnemyAIMgr.inst != null) 
        {
            
            EnemyAIMgr.inst.ResetLevel2State();
        }
        isRecording = false;

        ScenarioData scenario = GameMgr.inst.GetScenario(scenarioNumber);
        Debug.Log(scenario != null
            ? $"ReplayMgr: Loaded scenario {scenarioNumber} with win condition: {scenario.winLoss}, reason: {scenario.winReason}"
            : $"ReplayMgr: No scenario data found for scenario {scenarioNumber}");
        if (scenario != null)
        {
            actualPlayerWon = scenario.winLoss;
            actualWinReason = scenario.winReason;
            actualTimeTaken = scenario.totalTime;
            Debug.Log($"ReplayMgr: Captured actual win condition for scenario {scenarioNumber}: PlayerWon={actualPlayerWon}, WinReason='{actualWinReason}'");

            GameMgr.inst.InitializeScenarioFromData(scenario);
            Debug.Log($"ReplayMgr: Scenario {scenarioNumber} initialized from data.");
        }
        else
        {
            Debug.LogWarning($"ReplayMgr: No scenario data available for replay of scenario {scenarioNumber}. Aborting replay start.");
            return;
        }

        isReplaying = true;

        nextCommandIndex = 0;
        Debug.Log($"ReplayMgr: Replay started. isReplaying={isReplaying}, replayStartTime={replayStartTime}, nextCommandIndex={nextCommandIndex}");
        if (CameraMgr.inst != null) CameraMgr.inst.ReplayCamera(); else Debug.LogWarning("ReplayMgr: CameraMgr.inst is null, cannot set replay camera.");
        replayStartTime = Time.unscaledTime;
    }

    private void Update()
    {
        if (isReplaying && !replayFinished && currentReplayCommands != null)
        {
            float currentReplayTime = Time.unscaledTime - replayStartTime;
            if (currentReplayTime > actualTimeTaken + 10f)
            {
                Debug.LogWarning($"ReplayMgr: Current replay time {currentReplayTime:F2} exceeds actual time taken {actualTimeTaken:F2}. Stopping replay.");
                StopReplayAndShowScores();
                return;
            }
            // ADDED: General status log at the beginning of Update when replaying
            float effectiveReplayLoopTime = currentReplayTime * Time.timeScale;

            Debug.Log($"ReplayMgr Update: IsReplaying={isReplaying}, ReplayFinished={replayFinished}, EffectiveReplayTime={effectiveReplayLoopTime:F2} (UnscaledTime={currentReplayTime:F2}, TimeScale={Time.timeScale:F2}), NextCmdIndex={nextCommandIndex}, TotalCmds={(currentReplayCommands != null ? currentReplayCommands.Count : 0)}");

            while (nextCommandIndex < currentReplayCommands.Count &&
                   currentReplayCommands[nextCommandIndex].timestamp <= effectiveReplayLoopTime)
            {
                ReplayCommand commandToExecute = currentReplayCommands[nextCommandIndex];
                // MODIFIED: Added command type to the existing log for better context
                Debug.Log($"ReplayMgr: Executing command {nextCommandIndex + 1}/{currentReplayCommands.Count} (Type: {commandToExecute.commandType}) at replay time {currentReplayTime:F2} (command timestamp: {commandToExecute.timestamp:F2})");
                ExecuteCommand(commandToExecute);
                nextCommandIndex++;
            }

            if (nextCommandIndex >= currentReplayCommands.Count && !replayFinished) 
            {
                // MODIFIED: Enhanced log before checking win condition for more context
                Debug.Log($"ReplayMgr: All {currentReplayCommands.Count} commands processed (nextCommandIndex is {nextCommandIndex}). Current ReplayTime: {currentReplayTime:F2}. Preparing to check win condition.");
                
                // The original log "ReplayMgr: All commands executed - checking win condition." is now covered by the enhanced one above.
                if (CheckWinConditionMatchesActual()) // Note: CheckWinConditionMatchesActual() already has its own detailed log
                {
                    // MODIFIED: Enhanced log for win condition match
                    Debug.Log("ReplayMgr: Win condition check: PASSED (matches actual recorded outcome). Stopping replay and showing scores.");
                    StopReplayAndShowScores();
                }
                else
                {
                    // MODIFIED: Enhanced log for win condition mismatch or ScoreMgr not ready
                    Debug.LogWarning("ReplayMgr: Win condition check: FAILED (does NOT match actual recorded outcome) OR ScoreMgr not ready. Replay will continue. Verify game logic, ScoreMgr state, and actual win conditions recorded for the scenario.");
                }
            }
        }
    }

    private bool CheckWinConditionMatchesActual()
    {
        if (ScoreMgr.inst == null)
        {
            Debug.LogWarning("ReplayMgr: ScoreMgr.inst is null. Cannot check win condition.");
            return false;
        }

        bool winConditionMatches =
            (ScoreMgr.inst.playerWon == actualPlayerWon) &&
            (ScoreMgr.inst.winReason == actualWinReason);

        Debug.Log($"ReplayMgr: Checking win condition. Actual: PlayerWon={actualPlayerWon}, Reason='{actualWinReason}'. Current ScoreMgr: PlayerWon={ScoreMgr.inst.playerWon}, Reason='{ScoreMgr.inst.winReason}'. Match: {winConditionMatches}");
        return winConditionMatches;
    }

    private void StopReplayAndShowScores()
    {
        Debug.Log("ReplayMgr: StopReplayAndShowScores called.");
        isReplaying = false;
        replayFinished = true;
        if (OpenOceanMain.inst != null)
        {
            OpenOceanMain.inst.lobbyState = LobbyState.MultiScorePanel;
            Debug.Log("ReplayMgr: Set OpenOceanMain lobby state to MultiScorePanel.");
        }
        else
        {
            Debug.LogWarning("ReplayMgr: OpenOceanMain.inst is null. Cannot set lobby state.");
        }
    }

    private void ExecuteCommand(ReplayCommand cmd)
    {
        Debug.Log($"ReplayMgr: Executing command type '{cmd.commandType}' for entity IDs: [{string.Join(", ", cmd.entityIds)}], Timestamp: {cmd.timestamp}");
        List<Entity> entities = new();
        foreach (int id in cmd.entityIds)
        {
            if (EntityMgr.inst.entitiesDict.TryGetValue(id, out Entity ent))
            {
                entities.Add(ent);
            }
            else
            {
                Debug.LogWarning($"ReplayMgr: Entity with ID {id} not found for command '{cmd.commandType}'.");
            }
        }

        if (entities.Count == 0 && cmd.entityIds.Length > 0)
        {
            Debug.LogWarning($"ReplayMgr: No valid entities found for command '{cmd.commandType}'. Skipping execution.");
            return;
        }
        if (entities.Count == 0 && cmd.entityIds.Length == 0) 
        {
            Debug.Log($"ReplayMgr: Command '{cmd.commandType}' does not involve specific entities or no entities provided.");
        }

        switch (cmd.commandType)
        {
            case "Move":
                Debug.Log($"ReplayMgr: Handling Move command. TargetPosition: {cmd.targetPosition}, Add: {cmd.add}");
                AIMgr.inst.HandleMove(entities, cmd.targetPosition, cmd.add, isLocalCommand: false);
                break;
            case "AttackMoveToPosition":
                Debug.Log($"ReplayMgr: Handling AttackMoveToPosition command. TargetPosition: {cmd.targetPosition}, Add: {cmd.add}");
                AIMgr.inst.HandleAttackMove(entities, cmd.targetPosition, null, cmd.add, isLocalCommand: false);
                break;
            case "AttackMoveToEntity":
                Entity targetEnt = EntityMgr.inst.entitiesDict.TryGetValue(cmd.targetEntityId, out Entity tEnt) ? tEnt : null;
                if (targetEnt != null)
                {
                    Debug.Log($"ReplayMgr: Handling AttackMoveToEntity command. TargetEntityID: {cmd.targetEntityId}, TargetPosition: {cmd.targetPosition}, Add: {cmd.add}");
                    AIMgr.inst.HandleAttackMove(entities, cmd.targetPosition, targetEnt, cmd.add, isLocalCommand: false);
                }
                else
                {
                    Debug.LogWarning($"ReplayMgr: Target entity with ID {cmd.targetEntityId} not found for AttackMoveToEntity command.");
                }
                break;
            default:
                Debug.LogWarning($"ReplayMgr: Unknown command type '{cmd.commandType}' encountered.");
                break;
        }
    }

    [Serializable]
    public class ReplayCommandList
    {
        public List<ReplayCommand> commands;
    }

    public void CompleteScenario()
    {
        Debug.Log($"Completing scenario {currentScenarioNumber}. Recording stopped.");
        isRecording = false;
        SaveScenarioCommands(currentScenarioNumber);
    }

    private void SaveScenarioCommands(int scenarioNumber)
    {
        ScenarioReplayData replayData = scenarioReplayDataList.Find(data => data.scenarioNumber == scenarioNumber);
        if (replayData != null && replayData.commands != null && replayData.commands.Count > 0)
        {
            ReplayCommandList commandList = new ReplayCommandList { commands = replayData.commands };
            string json = JsonUtility.ToJson(commandList);
            string filePath = Path.Combine(Application.persistentDataPath, $"scenario_{scenarioNumber}_commands.json");
            File.WriteAllText(filePath, json);
            Debug.Log($"Saved scenario {scenarioNumber} commands to {filePath}");
        }
        else
        {
            Debug.LogWarning($"ReplayMgr: No commands found for scenario {scenarioNumber} to save, or scenario data not found.");
        }
    }
}

