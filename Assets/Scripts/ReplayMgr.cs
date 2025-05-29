using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ReplayCommand
{
    public float timestamp;
    public string commandType; // e.g., "Move", "AttackMoveToPosition", "AttackMoveToEntity"
    public int[] entityIds;
    public Vector3 targetPosition;
    public int targetEntityId; // -1 if not applicable
    public bool add;
}

public class ReplayMgr : MonoBehaviour
{
    public static ReplayMgr inst;
    private List<ReplayCommand> recordedCommands = new List<ReplayCommand>();
    public bool isReplaying = false;
    private float replayStartTime;
    private int nextCommandIndex = 0;
    private bool replayFinished = false;
    
    // Store actual win condition from scenario data
    public bool actualPlayerWon;
    public string actualWinReason;

    private void Awake()
    {
        if (inst == null)
        {
            inst = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RecordCommand(ReplayCommand cmd)
    {
        recordedCommands.Add(cmd);
    }

    public void LogButtonPressed()
    {
        if (OpenOceanMain.inst != null)
        {
            
            OpenOceanMain.inst.lobbyState = LobbyState.Replay;

        }
        else
        {
            Debug.LogWarning("OpenOceanMain instance not found. Cannot set lobby state to Replay.");
        }
    }

    public void StartReplay()
    {
        if(OpenOceanMain.inst == null || OpenOceanMain.inst.lobbyState != LobbyState.Replay)
        {
            Debug.LogWarning("Cannot start replay - OpenOceanMain or lobby state is not set correctly");
            return;
        }
        replayFinished = false;
        Time.timeScale = 1f;
        GameMgr.inst.BuildEntityDictionary();

        // Stop AI and reset systems
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
        ResetScene.inst.ClearAllEntities();

        // Disable fog of war for replay
        FogWarMgr.inst.FOW = false;

        // Load last scenario
        ScenarioData lastScenario = GameMgr.inst.GetLastScenario();
        if (lastScenario != null)
        {
            // Capture actual win condition
            actualPlayerWon = lastScenario.winLoss;
            actualWinReason = lastScenario.winReason;

            GameMgr.inst.InitializeScenarioFromData(lastScenario);
        }
        else
        {
            Debug.LogWarning("No scenario data available for replay");
            return;
        }

        // Start replay
        isReplaying = true;
        replayStartTime = Time.time;
        nextCommandIndex = 0;
        CameraMgr.inst.ReplayCamera();
    }

    private void Update()
    {
        if (isReplaying && !replayFinished)
        {
            float currentReplayTime = Time.time - replayStartTime;
            
            // Execute commands based on their timestamps
            while (nextCommandIndex < recordedCommands.Count && 
                   recordedCommands[nextCommandIndex].timestamp <= currentReplayTime)
            {
                ExecuteCommand(recordedCommands[nextCommandIndex]);
                nextCommandIndex++;
            }

            // Check if win condition matches actual scenario
            if (CheckWinConditionMatchesActual())
            {
                Debug.Log("Replay win condition matches actual - stopping replay");
                StopReplayAndShowScores();
                return;
            }

            
        }
    }

    private bool CheckWinConditionMatchesActual()
    {
        // Make sure ScoreMgr is available
        if (ScoreMgr.inst == null) return false;
        
        // Check if either side has won in the replay
        if (ScoreMgr.inst.playerWon || ScoreMgr.inst.aiWon)
        {
            // Compare with actual recorded win condition
            return ScoreMgr.inst.playerWon == actualPlayerWon && 
                   ScoreMgr.inst.winReason == actualWinReason;
        }
        
        return false;
    }

    private void StopReplayAndShowScores()
    {
        isReplaying = false;
        replayFinished = true;
        OpenOceanMain.inst.lobbyState = LobbyState.MultiScorePanel;
    }

    private void ExecuteCommand(ReplayCommand cmd)
    {
        List<Entity> entities = new List<Entity>();
        foreach (int id in cmd.entityIds)
        {
            if (EntityMgr.inst.entitiesDict.TryGetValue(id, out Entity ent))
            {
                entities.Add(ent);
            }
        }

        if (entities.Count == 0) return;

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
        }
    }
}