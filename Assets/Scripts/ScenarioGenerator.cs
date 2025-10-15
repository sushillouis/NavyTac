using System;
using System.Collections.Generic;
using UnityEngine;

public class ScenarioGenerator : MonoBehaviour
{
    public static ScenarioGenerator inst;

    [Header("Scenario Entity Configuration")]
    [SerializeField] public List<EntityQuantity> entityQuantities = new();
    public Dictionary<EntityType, int> entityDict;

    [Header("Player Start Positions")]
    public List<Vector3> posPlayer1List = new();
    public List<float> headingPlayer1List = new();
    public List<Vector3> posPlayer2List = new();
    public List<float> headingPlayer2List = new();

    [Range(0f, 1f)]
    public float difficultyLevel;
    public Dictionary<string, float> difficultyRanges = new()
    {
        {"easy", 0.333f},
        {"medium", 0.667f},
        {"hard", 1.00f}
    };
    private Difficulty currentDifficulty;

    private int[,] positionRelations = new int[4, 3]
    {
        {1, 3, 2},
        {0, 2, 3},
        {3, 0, 1},
        {2, 1, 0}
    };

    public List<ScenarioData> allScenarios = new();

    private void Awake()
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
        
        BuildEntityDictionary();
    }

    #region Difficulty Management

    public void DetermineDifficulty()
    {
        if (OpenOceanMain.inst == null) 
        {
            // Default difficulty if OpenOceanMain is not available
            difficultyLevel = 0.333f; // Easy difficulty
            currentDifficulty = Difficulty.Easy;
            return;
        }

        switch (OpenOceanMain.inst.currentTrainingState)
        {
            case TrainingState.Tutorial:
                difficultyLevel = 0.1f; // Very easy for tutorial
                break;
            case TrainingState.PreTest:
                difficultyLevel = 0.333f; // Easy difficulty for pre-test
                break;
            case TrainingState.PostTest:
                if (OpenOceanMain.inst.gamePlayCountMAX > 0)
                {
                    float progress = (float)OpenOceanMain.inst.gamesPlayedCount / OpenOceanMain.inst.gamePlayCountMAX;
                    difficultyLevel = progress < 0.6f ? 0.2f : 0.5f;
                }
                else
                {
                    difficultyLevel = 0.2f;
                }
                break;
            case TrainingState.Adaptive:
                difficultyLevel = ComputeAdaptiveDifficulty();
                break;
            case TrainingState.NonAdaptive:
                difficultyLevel = 0.5f; // Medium difficulty for non-adaptive
                break;
            default:
                difficultyLevel = 0.333f; // Default to easy
                break;
        }

        // Ensure difficulty level is within valid range
        difficultyLevel = Mathf.Clamp(difficultyLevel, 0.05f, 1f);

        if (difficultyLevel <= difficultyRanges["easy"]) currentDifficulty = Difficulty.Easy;
        else if (difficultyLevel <= difficultyRanges["medium"]) currentDifficulty = Difficulty.Medium;
        else currentDifficulty = Difficulty.Hard;
    }

    private float ComputeAdaptiveDifficulty()
    {
        // Start with a reasonable default if no previous difficulty is set
        if (difficultyLevel <= 0f)
        {
            difficultyLevel = 0.333f; // Start with Easy difficulty
        }

        if (ScoreMgr.inst != null)
        {
            if (ScoreMgr.inst.playerScores.Count == 0)
            {
                return difficultyLevel;
            }
            
            float lastScore = ScoreMgr.inst.playerScores[^1];
            float adjustment = 0.07f * lastScore / 100f;
            difficultyLevel += adjustment;
        }
        
        return Mathf.Clamp(difficultyLevel, 0.05f, 1f);
    }

    #endregion

    #region Scenario Generation

    public ScenarioData GenerateScenario(TrainingState trainingState)
    {
        DetermineDifficulty();

        ScenarioData scenario = new ScenarioData
        {
            scenarioNumber = allScenarios.Count + 1,
            difficultyLevel = difficultyLevel,
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        if (trainingState == TrainingState.Tutorial)
        {
            GenerateTutorialScenario(scenario);
        }
        else
        {
            GenerateStandardScenario(scenario, trainingState);
        }

        return scenario;
    }

    private void GenerateTutorialScenario(ScenarioData scenario)
    {
        // Set all entity counts to 1 for tutorial
        scenario.entityQuantities = new List<EntityQuantity>();
        foreach (EntityQuantity eq in entityQuantities)
        {
            scenario.entityQuantities.Add(new EntityQuantity { entityType = eq.entityType, unitCount = 1 });
        }

        // Simple positioning for tutorial
        scenario.Player1Positions.Add(new Vector3(0, 0, 0));
        scenario.Player1Headings.Add(0f); // Facing North

        scenario.Player2Positions.Add(new Vector3(0, 0, 7000));
        scenario.Player2Headings.Add(180f); // Facing South
    }

    private void GenerateStandardScenario(ScenarioData scenario, TrainingState trainingState)
    {
        // Adjust unit counts based on training state
        if (trainingState == TrainingState.Adaptive)
        {
            AdjustAdaptiveUnitCounts();
        }
        else
        {
            AdjustNonAdaptiveUnitCounts();
        }

        scenario.entityQuantities = new List<EntityQuantity>(entityQuantities);

        // Generate positions
        GeneratePlayerPositions(scenario);
    }

    private void GeneratePlayerPositions(ScenarioData scenario)
    {
        StartingPosition[] allPositions = new StartingPosition[]
        {
            new() { position = new(0, 0, -7000), heading = 0 },
            new() { position = new(0, 0, 7000),  heading = 180 },
            new() { position = new(-7000, 0, 0), heading = 90 },
            new() { position = new(7000, 0, 0),  heading = 270 }
        };

        // Assign player 1 position
        int player1Index = UnityEngine.Random.Range(0, allPositions.Length);
        StartingPosition p1StartPos = allPositions[player1Index];
        scenario.Player1Positions.Add(p1StartPos.position);
        scenario.Player1Headings.Add(p1StartPos.heading);

        // Assign player 2 position based on difficulty
        List<int> player2ValidIndices = GetValidPlayer2Positions(player1Index);
        int player2AssignedIndex = player1Index;
        if (player2ValidIndices.Count > 0)
        {
            player2AssignedIndex = player2ValidIndices[UnityEngine.Random.Range(0, player2ValidIndices.Count)];
        }
        else
        {
            for (int i = 0; i < allPositions.Length; ++i)
            {
                if (i != player1Index)
                {
                    player2AssignedIndex = i;
                    break;
                }
            }
        }

        StartingPosition p2StartPos = allPositions[player2AssignedIndex];
        scenario.Player2Positions.Add(p2StartPos.position);
        scenario.Player2Headings.Add(p2StartPos.heading);
    }

    #endregion

    #region Unit Count Adjustments

    private void AdjustAdaptiveUnitCounts()
    {
        foreach (EntityQuantity eq in entityQuantities)
        {
            if (eq.entityType == EntityType.Rig_Balder)
            {
                eq.unitCount = 1;
                continue;
            }
            // Calculate unit count based on difficulty level, ensuring minimum of 1
            int calculatedCount = Mathf.RoundToInt(21.25f * difficultyLevel - 1.25f);
            eq.unitCount = Mathf.Max(1, calculatedCount);
        }
        BuildEntityDictionary();
    }

    private void AdjustNonAdaptiveUnitCounts()
    {
        foreach (EntityQuantity eq in entityQuantities)
        {
            if (eq.entityType == EntityType.Rig_Balder)
            {
                eq.unitCount = 1;
                continue;
            }
            eq.unitCount = currentDifficulty switch
            {
                Difficulty.Easy => UnityEngine.Random.Range(3, 6),
                Difficulty.Medium => UnityEngine.Random.Range(6, 11),
                Difficulty.Hard => UnityEngine.Random.Range(11, 21),
                _ => eq.unitCount
            };
        }
        BuildEntityDictionary();
    }

    #endregion

    #region Entity Speed Adjustments

    public void AdjustAdaptiveEntitySpeed()
    {
        float range = 2f;
        float speedFactor = 1f + difficultyLevel * range;

        foreach (GameObject prefabGo in EntityMgr.inst.entityPrefabs)
        {
            var prefab = prefabGo.GetComponent<Entity>();
            if (prefab == null || prefab.entityType == EntityType.Rig_Balder) continue;

            // SetAccelerationAndTurnRate(prefab, speedFactor);
        }
    }

    public void AdjustNonAdaptiveEntitySpeed()
    {
        float baseFactor = currentDifficulty switch
        {
            Difficulty.Easy => 1f,
            Difficulty.Medium => 2f,
            Difficulty.Hard => 3f,
            _ => 1f
        };

        foreach (GameObject prefabGo in EntityMgr.inst.entityPrefabs)
        {
            var prefab = prefabGo.GetComponent<Entity>();
            if (prefab == null || prefab.entityType == EntityType.Rig_Balder) continue;

            // SetAccelerationAndTurnRate(prefab, baseFactor);
        }
    }

    // private void SetAccelerationAndTurnRate(Entity entity, float speedFactor)
    // {
    //     // entity.maxSpeed = entity.originalMaxSpeed * speedFactor;
    //     float stopDistance = 100f;
    //     if (entity.maxSpeed > 0.01f)
    //     {
    //         entity.acceleration = (entity.maxSpeed * entity.maxSpeed) / (2f * stopDistance);
    //         if (entity.entityType == EntityType.AntiShipMissile)
    //         {
    //             //  entity.turnRate = entity.originalTurnRate;
    //         }
    //         else
    //         {
    //             entity.turnRate = Mathf.Rad2Deg * (entity.acceleration / entity.maxSpeed);
    //         }
    //     }
    //     else
    //     {
    //         entity.acceleration = 0f;
    //         entity.turnRate = 0f;
    //     }
    // }

    #endregion

    #region Spawning

    public void SpawnScenario(ScenarioData scenario)
    {
        // Copy scenario data to local lists for spawning
        entityQuantities = new List<EntityQuantity>(scenario.entityQuantities);
        posPlayer1List = new List<Vector3>(scenario.Player1Positions);
        headingPlayer1List = new List<float>(scenario.Player1Headings);
        posPlayer2List = new List<Vector3>(scenario.Player2Positions);
        headingPlayer2List = new List<float>(scenario.Player2Headings);
        difficultyLevel = scenario.difficultyLevel;

        BuildEntityDictionary();

        if (scenario.Player1Positions.Count == 1 && scenario.Player1Positions[0] == Vector3.zero &&
            scenario.Player2Positions.Count == 1 && scenario.Player2Positions[0] == new Vector3(0, 0, 7000))
        {
            // This is a tutorial scenario
            SpawnTutorialEntities();
        }
        else
        {
            // Standard scenario
            SpawnStandardEntities();
        }

        ApplyGreyOverlays();
    }

    public void SpawnTutorialEntities()
    {
        if (PlayerMgr.inst != null && SpawnEntityMgr.inst != null)
        {
            SpawnEntityMgr.inst.SpawnEntitiesFromDictionary(posPlayer1List[0], headingPlayer1List[0], PlayerMgr.inst.player1);
            SpawnEntityMgr.inst.SpawnEntitiesFromDictionary(posPlayer2List[0], headingPlayer2List[0], PlayerMgr.inst.player2);
            EntityMgr.inst.CreateEntity(EntityType.DDG51, new Vector3(0, 0, 3000), new Vector3(0, headingPlayer2List[0], 0), player: PlayerMgr.inst.player2);
        }
    }

    public void SpawnStandardEntities()
    {
        if (PlayerMgr.inst == null || SpawnEntityMgr.inst == null) return;

        // Spawn players
        SpawnWithExistingPositions();

        // Spawn neutral entities based on difficulty
        SpawnNeutralEntities();
    }

    public void SpawnWithExistingPositions()
    {
        if (PlayerMgr.inst != null && SpawnEntityMgr.inst != null)
        {
            for (int i = 0; i < posPlayer1List.Count; i++)
            {
                SpawnEntityMgr.inst.SpawnEntitiesFromDictionary(posPlayer1List[i], headingPlayer1List[i], PlayerMgr.inst.player1);
            }
            for (int i = 0; i < posPlayer2List.Count; i++)
            {
                SpawnEntityMgr.inst.SpawnEntitiesFromDictionary(posPlayer2List[i], headingPlayer2List[i], PlayerMgr.inst.player2);
            }
        }
    }

    private void SpawnNeutralEntities()
    {
        StartingPosition[] allPositions = new StartingPosition[]
        {
            new() { position = new(0, 0, -7000), heading = 0 },
            new() { position = new(0, 0, 7000),  heading = 180 },
            new() { position = new(-7000, 0, 0), heading = 90 },
            new() { position = new(7000, 0, 0),  heading = 270 }
        };

        // Find positions already used by players
        List<int> usedIndices = new();
        for (int i = 0; i < allPositions.Length; i++)
        {
            for (int j = 0; j < posPlayer1List.Count; j++)
            {
                if (Vector3.Distance(allPositions[i].position, posPlayer1List[j]) < 100f)
                {
                    usedIndices.Add(i);
                    break;
                }
            }
            for (int j = 0; j < posPlayer2List.Count; j++)
            {
                if (Vector3.Distance(allPositions[i].position, posPlayer2List[j]) < 100f)
                {
                    usedIndices.Add(i);
                    break;
                }
            }
        }

        List<int> neutralIndices = new();
        for (int i = 0; i < allPositions.Length; ++i)
        {
            if (!usedIndices.Contains(i))
            {
                neutralIndices.Add(i);
            }
        }

        // Spawn neutral entities based on difficulty
        var neutralPlayer = PlayerMgr.inst?.neutral;
        if (neutralPlayer != null && SpawnEntityMgr.inst != null)
        {
            if (currentDifficulty == Difficulty.Medium && neutralIndices.Count > 0)
            {
                // Medium: spawn at one place
                SpawnEntityMgr.inst.SpawnEntitiesFromDictionary(
                    allPositions[neutralIndices[0]].position,
                    allPositions[neutralIndices[0]].heading,
                    neutralPlayer
                );
            }
            else if (currentDifficulty == Difficulty.Hard && neutralIndices.Count > 0)
            {
                // Hard: spawn at two places if possible
                for (int i = 0; i < Mathf.Min(2, neutralIndices.Count); i++)
                {
                    SpawnEntityMgr.inst.SpawnEntitiesFromDictionary(
                        allPositions[neutralIndices[i]].position,
                        allPositions[neutralIndices[i]].heading,
                        neutralPlayer
                    );
                }
            }
            // Easy: do not spawn neutral entities
        }
    }

    private void ApplyGreyOverlays()
    {
        List<Entity> allEntities = EntityMgr.inst.entities;
        foreach (Entity e in allEntities)
        {
            if (e.TryGetComponent<GreyOverlayGenerator>(out var greyOverlay))
            {
                greyOverlay.ApplyGreyOverlay();
            }
        }
    }

    #endregion

    #region Utility Methods

    private List<int> GetValidPlayer2Positions(int player1Index)
    {
        List<int> validPositions = new();
        switch (currentDifficulty)
        {
            case Difficulty.Easy:
                validPositions.Add(positionRelations[player1Index, 0]);
                break;
            case Difficulty.Medium:
            case Difficulty.Hard:
                for (int i = 0; i < 4; i++)
                    if (i != player1Index) validPositions.Add(i);
                break;
        }
        return validPositions;
    }

    public void BuildEntityDictionary()
    {
        entityDict = new();
        foreach (EntityQuantity eq in entityQuantities)
        {
            if (eq.entityType == EntityType.Rig_Balder)
            {
                entityDict[eq.entityType] = Mathf.Min(eq.unitCount, 1);
                continue;
            }
            
            // Ensure unit count is at least 0 (no negative counts)
            int safeUnitCount = Mathf.Max(0, eq.unitCount);
            
            if (entityDict.ContainsKey(eq.entityType))
                entityDict[eq.entityType] += safeUnitCount;
            else
                entityDict.Add(eq.entityType, safeUnitCount);
        }
    }

    public void GenerateTwoVsTwoScenario()
    {
        // Define all possible starting positions and headings
        StartingPosition northStart = new() { position = new(0, 0, -7000), heading = 0 };
        StartingPosition southStart = new() { position = new(0, 0, 7000), heading = 180 };
        StartingPosition westStart = new() { position = new(-7000, 0, 0), heading = 90 };
        StartingPosition eastStart = new() { position = new(7000, 0, 0), heading = 270 };

        // Clear existing lists to prepare for the new scenario
        posPlayer1List.Clear();
        headingPlayer1List.Clear();
        posPlayer2List.Clear();
        headingPlayer2List.Clear();

        // Assign two starting positions and headings for Player 1
        posPlayer1List.Add(northStart.position);
        headingPlayer1List.Add(northStart.heading);
        posPlayer1List.Add(westStart.position);
        headingPlayer1List.Add(westStart.heading);

        // Assign the remaining two starting positions and headings for Player 2
        posPlayer2List.Add(southStart.position);
        headingPlayer2List.Add(southStart.heading);
        posPlayer2List.Add(eastStart.position);
        headingPlayer2List.Add(eastStart.heading);

        // Ensure the entity dictionary reflects the current unit counts
        BuildEntityDictionary();

        // Spawn the entities for both players at their designated starting locations
        SpawnWithExistingPositions();
    }

    #endregion

    #region Scenario Data Management

    public void StoreCurrentScenario()
    {
        if (ReplayMgr.inst != null && ReplayMgr.inst.isReplaying) return;

        ScenarioData data = new()
        {
            scenarioNumber = allScenarios.Count + 1,
            entityQuantities = new(entityQuantities),
            Player1Positions = new(posPlayer1List),
            Player1Headings = new(headingPlayer1List),
            Player2Positions = new(posPlayer2List),
            Player2Headings = new(headingPlayer2List),
            difficultyLevel = difficultyLevel,
            winLoss = ScoreMgr.inst != null && ScoreMgr.inst.playerWon,
            winReason = ScoreMgr.inst != null ? ScoreMgr.inst.winReason : "Unknown",
            score = ScoreMgr.inst != null ? ScoreMgr.inst.score : 0f,
            totalTime = OpenOceanMain.inst.playSessionDuration,
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        SaveScenario(data);
    }

    public void SaveScenario(ScenarioData data) => allScenarios.Add(data);

    public ScenarioData GetScenario(int scenarioNumber) =>
        allScenarios.Find(s => s.scenarioNumber == scenarioNumber);

    public void InitializeScenarioFromData(ScenarioData data)
    {
        entityQuantities = new(data.entityQuantities);
        posPlayer1List = new(data.Player1Positions);
        headingPlayer1List = new(data.Player1Headings);
        posPlayer2List = new(data.Player2Positions);
        headingPlayer2List = new(data.Player2Headings);
        difficultyLevel = data.difficultyLevel;
        
        DetermineDifficulty();
        SpawnWithExistingPositions();
    }

    #endregion

    #region Public Properties

    public float CurrentDifficultyLevel => difficultyLevel;
    public Difficulty CurrentDifficulty => currentDifficulty;
    public Dictionary<EntityType, int> EntityDictionary => entityDict;

    #endregion
}
