using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyAIMgr : MonoBehaviour
{
    public int currentLevel = 1;
    public static EnemyAIMgr inst;
    public Entity opponentBase;
    public List<Entity> OpponentBases = new List<Entity>();
    public List<Entity> aiBases = new List<Entity>();
    public List<Entity> neutralBases = new List<Entity>();
    public List<Entity> aiEntitiesList = new List<Entity>();
    public List<Entity> playerEntitiesList = new List<Entity>();
    public List<Entity> neutralEntitiesList = new List<Entity>();
    private const float DefaultUpdateInterval = 0.5f;
    private readonly float updateInterval = DefaultUpdateInterval;
    private float lastUpdateTime;

    private BaseEnemyAI currentAI;
    private int _previousLevel = -1;

    void Awake()
    {
        if (inst == null)
        {
            inst = this;
        }
        else if (inst != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Update()
    {
        // Defer AI updates during tutorial, intro, or within the update interval.
        if (OpenOceanMain.inst.currentTrainingState == TrainingState.Tutorial ||
            GameMgr.inst.isIntroPlaying ||
            Time.time - lastUpdateTime < updateInterval)
        {
            return;
        }
        lastUpdateTime = Time.time;

        UpdateAllBasesLists();

        // Ensure AI has bases to operate from.
        if (!aiBases.Any())
        {
            HandleNoAIBases();
            return;
        }

        // Ensure there is an opponent base to target.
        opponentBase = FindOpponentBase();
        if (opponentBase == null)
        {
            HandleNoOpponentBase();
            return;
        }

        UpdateAllEntityLists();

        // If there are no AI entities, there's nothing to command.
        if (!aiEntitiesList.Any())
        {
            return;
        }

        UpdateAILevel();

        // Execute AI logic if an AI controller is active.
        if (currentAI != null)
        {
            currentAI.PruneEntityCooldowns(aiEntitiesList);
            currentAI.ProcessCombatBehavior(aiEntitiesList);
        }
    }

    private void UpdateAILevel()
    {
        if (currentLevel != _previousLevel)
        {
            if (currentAI != null)
            {
                currentAI.ResetState();
            }

            switch (currentLevel)
            {
                case 1:
                    currentAI = new Level1EnemyAI();
                    break;
                case 2:
                    currentAI = new Level2EnemyAI();
                    break;
                case 3:
                    currentAI = new Level3EnemyAI();
                    break;
                default:
                    //Debug.LogWarning($"Unhandled AI level: {currentLevel}");
                    currentAI = null;
                    break;
            }

            if (currentAI != null)
            {
                currentAI.Init(opponentBase, aiBases, playerEntitiesList, neutralBases, aiEntitiesList, neutralEntitiesList);
            }
            _previousLevel = currentLevel;
        }
        else if (currentAI != null)
        {
            // Continuously update the bases in case they change
            currentAI.Init(opponentBase, aiBases, playerEntitiesList, neutralBases, aiEntitiesList, neutralEntitiesList);
        }
    }


    private void CheckAndLogDestroyedAIBases()
    {
        for (int i = aiBases.Count - 1; i >= 0; i--)
        {
            Entity baseEntity = aiBases[i];
            if (baseEntity == null)
            {
                aiBases.RemoveAt(i);
            }
        }
    }
    private void HandleNoAIBases()
    {
        // Logic when no AI bases are found
    }
    private void HandleNoOpponentBase()
    {
        ScoreMgr.inst.aiWon = true;
        ScoreMgr.inst.CheckVictory();
    }
    private Entity FindOpponentBase()
    {
        if (OpponentBases.Count > 0)
        {
            return OpponentBases[0]; // Return the first opponent base found
        }

        return null;
    }
    public void UpdateAllBasesLists()
    {
        OpponentBases.Clear();
        aiBases.Clear();
        neutralBases.Clear();

        foreach (var entity in EntityMgr.inst.entities)
        {
            if (entity == null || entity.entityRole != EntityRole.Base)
            {
                continue;
            }

            if (entity.owner == PlayerMgr.inst.player1)
            {
                OpponentBases.Add(entity);
            }
            else if (entity.owner == PlayerMgr.inst.player2)
            {
                aiBases.Add(entity);
            }
            else if (entity.owner == PlayerMgr.inst.neutral)
            {
                neutralBases.Add(entity);
            }
        }
    }

    public void UpdateAllEntityLists()
    {
        playerEntitiesList.Clear();
        aiEntitiesList.Clear();
        neutralEntitiesList.Clear();

        foreach (var entity in EntityMgr.inst.entities)
        {
            if (entity == null || entity.owner == null || entity.entityClass == EntityClass.Missile || 
                entity.entityRole == EntityRole.Base || entity.Equals(null))
            {
                continue;
            }

            if (entity.owner == PlayerMgr.inst.player1)
            {
                if (!playerEntitiesList.Contains(entity))
                {
                    playerEntitiesList.Add(entity);
                }
            }
            else if (entity.owner == PlayerMgr.inst.player2)
            {
                if (!aiEntitiesList.Contains(entity))
                {
                    aiEntitiesList.Add(entity);
                }
            }
            else if (entity.owner == PlayerMgr.inst.neutral)
            {
                if (!neutralEntitiesList.Contains(entity))
                {
                    neutralEntitiesList.Add(entity);
                }
            }
        }
    }
}

