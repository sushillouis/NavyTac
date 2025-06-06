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

public class ReplayMgr : MonoBehaviour
{
    [Serializable]
    public class ScenarioReplayData
    {
        public int scenarioNumber;
        public List<ReplayCommand> commands = new List<ReplayCommand>();
    }

    public static ReplayMgr inst;
    [SerializeField] private List<ScenarioReplayData> scenarioReplayDataList = new List<ScenarioReplayData>();

    private Dictionary<int, float> scenarioStartTimes = new Dictionary<int, float>();
    private Dictionary<int, float> scenarioDurations = new Dictionary<int, float>();

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
        if (inst == null)
        {
            inst = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartNewScenario()
    {
        currentScenarioNumber = OpenOceanMain.inst.gamesPlayedCount + 1;

        scenarioStartTimes[currentScenarioNumber] = Time.time;
        scenarioDurations[currentScenarioNumber] = 0f;

        ScenarioReplayData currentScenarioData = scenarioReplayDataList.Find(data => data.scenarioNumber == currentScenarioNumber);
        if (currentScenarioData == null)
        {
            currentScenarioData = new ScenarioReplayData { scenarioNumber = currentScenarioNumber };
            scenarioReplayDataList.Add(currentScenarioData);
        }

        isRecording = true;
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
        }
        else
        {
            currentReplayCommands = new List<ReplayCommand>();
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
        if (EnemyAIMgr.inst != null) EnemyAIMgr.inst.ResetLevel2State();

        isRecording = false;

        ScenarioData scenario = GameMgr.inst.GetScenario(scenarioNumber);

        if (scenario != null)
        {
            actualPlayerWon = scenario.winLoss;
            actualWinReason = scenario.winReason;
            GameMgr.inst.InitializeScenarioFromData(scenario);
        }
        else
        {
            return;
        }

        isReplaying = true;
        nextCommandIndex = 0;
        replayStartTime = Time.time;

        if (CameraMgr.inst != null) CameraMgr.inst.ReplayCamera();
    }

    private void Update()
    {
        if (isReplaying && !replayFinished && currentReplayCommands != null)
        {
            float currentReplayTime = Time.time - replayStartTime;

            if (currentReplayTime > actualTimeTaken + 10f)
            {
                StopReplayAndShowScores();
                return;
            }

            while (nextCommandIndex < currentReplayCommands.Count &&
                   currentReplayCommands[nextCommandIndex].timestamp <= currentReplayTime)
            {
                ReplayCommand commandToExecute = currentReplayCommands[nextCommandIndex];
                ExecuteCommand(commandToExecute);
                nextCommandIndex++;
            }

            if (nextCommandIndex >= currentReplayCommands.Count && !replayFinished)
            {
                if (CheckWinConditionMatchesActual())
                {
                    StopReplayAndShowScores();
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
        isReplaying = false;
        replayFinished = true;

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
                AIMgr.inst.HandleAttackMove(entities, cmd.targetPosition, null, cmd.add, isLocalCommand: false);

                break;

            case "AttackMoveToEntity":
                Entity targetEnt = EntityMgr.inst.entitiesDict.TryGetValue(cmd.targetEntityId, out Entity tEnt) ? tEnt : null;
                if (targetEnt != null)
                {
                    AIMgr.inst.HandleAttackMove(entities, cmd.targetPosition, targetEnt, cmd.add, isLocalCommand: false);
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
            commands = replayData.commands
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
}
