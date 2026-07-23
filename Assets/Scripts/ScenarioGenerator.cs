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
    public List<Vector3> neutralEntityPositions = new();
    public List<float> neutralEntityHeadings = new();
    public float nonAdaptiveDifficulty = 0.25f;
    public float adaptiveDifficulty = 0.33f;

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
        inst = this;
        BuildEntityDictionary();
    }

    public void DetermineDifficulty()
    {
        if (OpenOceanMain.inst == null)
        {
            difficultyLevel = 0.333f;
            currentDifficulty = Difficulty.Easy;
            return;
        }
        switch (OpenOceanMain.inst.currentTrainingState)
        {
            case TrainingState.Tutorial:
                difficultyLevel = 0.1f;
                break;
            case TrainingState.PreTest:
            case TrainingState.PostTest:
                if (OpenOceanMain.inst.gamePlayCountMAX > 0)
                {
                    float progress = (float)OpenOceanMain.inst.gamesPlayedCount / OpenOceanMain.inst.gamePlayCountMAX;
                    difficultyLevel = progress < 0.6f ? 0.2f : 0.5f;
                }
                else
                {
                    difficultyLevel = 0.333f;
                }
                break;
            case TrainingState.Adaptive:
                difficultyLevel = ComputeAdaptiveDifficulty();
                break;
            case TrainingState.NonAdaptive:
                difficultyLevel = nonAdaptiveDifficulty;
                break;
            default:
                difficultyLevel = 0.333f;
                break;
        }
        difficultyLevel = Mathf.Clamp(difficultyLevel, 0.05f, 1f);
        if (difficultyLevel <= difficultyRanges["easy"]) currentDifficulty = Difficulty.Easy;
        else if (difficultyLevel <= difficultyRanges["medium"]) currentDifficulty = Difficulty.Medium;
        else currentDifficulty = Difficulty.Hard;
    }

    private float ComputeAdaptiveDifficulty()
    {
        if (difficultyLevel <= 0f) difficultyLevel = adaptiveDifficulty;
        if (ScoreMgr.inst != null)
        {
            if (ScoreMgr.inst.playerScores.Count == 0) return difficultyLevel;
            float lastScore = ScoreMgr.inst.playerScores[^1];
            float adjustment = 0.07f * lastScore / 100f;
            difficultyLevel += adjustment;
        }
        return Mathf.Clamp(difficultyLevel, 0.05f, 1f);
    }

    public ScenarioData GenerateScenario(TrainingState trainingState)
    {
        DetermineDifficulty();
        ScenarioData scenario = new ScenarioData
        {
            scenarioNumber = allScenarios.Count + 1,
            difficultyLevel = difficultyLevel,
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            trainingState = trainingState
        };

        if (trainingState == TrainingState.Tutorial)
        {
            AdjustNonAdaptiveEntitySpeed();
            GenerateTutorialScenario(scenario);
        }
        else
        {
            if (trainingState == TrainingState.Adaptive) AdjustAdaptiveEntitySpeed();
            else AdjustNonAdaptiveEntitySpeed();
            GenerateStandardScenario(scenario, trainingState);
        }
        return scenario;
    }

    private void GenerateTutorialScenario(ScenarioData scenario)
    {
        scenario.entityQuantities = new List<EntityQuantity>();
        foreach (EntityQuantity eq in entityQuantities)
            scenario.entityQuantities.Add(new EntityQuantity { entityType = eq.entityType, unitCount = 1 });

        scenario.Player1Positions.Add(new Vector3(0, 0, 0));
        scenario.Player1Headings.Add(0f);
        scenario.Player2Positions.Add(new Vector3(0, 0, 7000));
        scenario.Player2Headings.Add(180f);
    }

    private void GenerateStandardScenario(ScenarioData scenario, TrainingState trainingState)
    {
        scenario.entityQuantities = new List<EntityQuantity>();
        foreach (EntityQuantity baseEq in entityQuantities)
            scenario.entityQuantities.Add(new EntityQuantity { entityType = baseEq.entityType, unitCount = baseEq.unitCount });

        if (trainingState == TrainingState.Adaptive)
            AdjustAdaptiveUnitCounts(scenario.entityQuantities);
        else
            AdjustNonAdaptiveUnitCounts(scenario.entityQuantities);

        GeneratePlayerPositions(scenario);
    }

    private void GeneratePlayerPositions(ScenarioData scenario)
    {
        StartingPosition[] allPositions = new StartingPosition[]
        {
            new() { position = new(0, 0, -7000), heading = 0 },
            new() { position = new(0, 0, 7000), heading = 180 },
            new() { position = new(-7000, 0, 0), heading = 90 },
            new() { position = new(7000, 0, 0), heading = 270 }
        };
        int player1Index = UnityEngine.Random.Range(0, allPositions.Length);
        StartingPosition p1StartPos = allPositions[player1Index];
        scenario.Player1Positions.Add(p1StartPos.position);
        scenario.Player1Headings.Add(p1StartPos.heading);
        List<int> player2ValidIndices = GetValidPlayer2Positions(player1Index);
        int player2AssignedIndex = player2ValidIndices.Count > 0
            ? player2ValidIndices[UnityEngine.Random.Range(0, player2ValidIndices.Count)]
            : (player1Index + 1) % allPositions.Length;
        StartingPosition p2StartPos = allPositions[player2AssignedIndex];
        scenario.Player2Positions.Add(p2StartPos.position);
        scenario.Player2Headings.Add(p2StartPos.heading);
    }

    private void AdjustAdaptiveUnitCounts(List<EntityQuantity> scenarioQuantities)
    {
        foreach (EntityQuantity eq in scenarioQuantities)
        {
            if (eq.entityType == EntityType.Rig_Balder)
            {
                eq.unitCount = 1;
                continue;
            }
            int calculatedCount = Mathf.RoundToInt(21.25f * difficultyLevel - 1.25f);
            eq.unitCount = Mathf.Max(1, calculatedCount);
        }
    }

    private void AdjustNonAdaptiveUnitCounts(List<EntityQuantity> scenarioQuantities)
    {
        foreach (EntityQuantity eq in scenarioQuantities)
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
    }

    public void AdjustAdaptiveEntitySpeed()
    {
        float range = 2f;
        float speedFactor = 1f + difficultyLevel * range;
        foreach (GameObject prefabGo in EntityMgr.inst.entityPrefabs)
        {
            var prefab = prefabGo.GetComponent<Entity>();
            if (prefab == null || prefab.entityType == EntityType.Rig_Balder) continue;
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
        }
    }

    public void SpawnScenario(ScenarioData scenario)
    {
        entityQuantities = new List<EntityQuantity>(scenario.entityQuantities);
        posPlayer1List = new List<Vector3>(scenario.Player1Positions);
        headingPlayer1List = new List<float>(scenario.Player1Headings);
        posPlayer2List = new List<Vector3>(scenario.Player2Positions);
        headingPlayer2List = new List<float>(scenario.Player2Headings);
        difficultyLevel = scenario.difficultyLevel;

        if (difficultyLevel <= difficultyRanges["easy"]) currentDifficulty = Difficulty.Easy;
        else if (difficultyLevel <= difficultyRanges["medium"]) currentDifficulty = Difficulty.Medium;
        else currentDifficulty = Difficulty.Hard;

        BuildEntityDictionary();

        if (scenario.Player1Positions.Count == 1 && scenario.Player1Positions[0] == Vector3.zero &&
            scenario.Player2Positions.Count == 1 && scenario.Player2Positions[0] == new Vector3(0, 0, 7000))
            SpawnTutorialEntities();
        else
            SpawnStandardEntities(scenario);

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

    public void SpawnStandardEntities(ScenarioData scenario)
    {
        if (PlayerMgr.inst == null || SpawnEntityMgr.inst == null) return;
        SpawnWithExistingPositions();
        SpawnNeutralEntities(scenario);
    }

    public void SpawnWithExistingPositions()
    {
        if (PlayerMgr.inst != null && SpawnEntityMgr.inst != null)
        {
            for (int i = 0; i < posPlayer1List.Count; i++)
                SpawnEntityMgr.inst.SpawnEntitiesFromDictionary(posPlayer1List[i], headingPlayer1List[i], PlayerMgr.inst.player1);
            for (int i = 0; i < posPlayer2List.Count; i++)
                SpawnEntityMgr.inst.SpawnEntitiesFromDictionary(posPlayer2List[i], headingPlayer2List[i], PlayerMgr.inst.player2);
        }
    }

    private void SpawnNeutralEntities(ScenarioData scenario)
    {
        StartingPosition[] allPositions = new StartingPosition[]
        {
            new() { position = new(0, 0, -7000), heading = 0 },
            new() { position = new(0, 0, 7000), heading = 180 },
            new() { position = new(-7000, 0, 0), heading = 90 },
            new() { position = new(7000, 0, 0), heading = 270 }
        };

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
            if (usedIndices.Contains(i)) continue;
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
                neutralIndices.Add(i);
        }

        var neutralPlayer = PlayerMgr.inst?.neutral;
        if (neutralPlayer != null && SpawnEntityMgr.inst != null)
        {
            if (currentDifficulty != Difficulty.Easy && neutralIndices.Count > 0)
            {
                int idx = neutralIndices[0];
                scenario.isNeutralBaseAvailable = true;
                scenario.NeutralBasePositions.Add(allPositions[idx].position);
                scenario.NeutralBaseHeadings.Add(allPositions[idx].heading);
                SpawnEntityMgr.inst.SpawnEntitiesFromDictionary(allPositions[idx].position, allPositions[idx].heading, neutralPlayer);
                neutralEntityHeadings.Add(allPositions[idx].heading);
                neutralEntityPositions.Add(allPositions[idx].position);
            }
        }
    }

    private void SpawnNeutralEntitiesFromData(ScenarioData data)
    {
        if (!data.isNeutralBaseAvailable) return;
        var neutralPlayer = PlayerMgr.inst?.neutral;
        if (neutralPlayer == null || SpawnEntityMgr.inst == null) return;
        for (int i = 0; i < data.NeutralBasePositions.Count; i++)
        {
            var pos = data.NeutralBasePositions[i];
            var hdg = i < data.NeutralBaseHeadings.Count ? data.NeutralBaseHeadings[i] : 0f;
            SpawnEntityMgr.inst.SpawnEntitiesFromDictionary(pos, hdg, neutralPlayer);
        }
    }

    private void ApplyGreyOverlays()
    {
        List<Entity> allEntities = EntityMgr.inst.entities;
        foreach (Entity e in allEntities)
        {
            if (e.TryGetComponent<GreyOverlayGenerator>(out var greyOverlay))
                greyOverlay.ApplyGreyOverlay();
        }
    }

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
            int safeUnitCount = Mathf.Max(0, eq.unitCount);
            if (entityDict.ContainsKey(eq.entityType))
                entityDict[eq.entityType] += safeUnitCount;
            else
                entityDict.Add(eq.entityType, safeUnitCount);
        }
    }

    public void GenerateTwoVsTwoScenario()
    {
        StartingPosition northStart = new() { position = new(0, 0, -7000), heading = 0 };
        StartingPosition southStart = new() { position = new(0, 0, 7000), heading = 180 };
        StartingPosition westStart = new() { position = new(-7000, 0, 0), heading = 90 };
        StartingPosition eastStart = new() { position = new(7000, 0, 0), heading = 270 };
        posPlayer1List.Clear();
        headingPlayer1List.Clear();
        posPlayer2List.Clear();
        headingPlayer2List.Clear();
        posPlayer1List.Add(northStart.position);
        headingPlayer1List.Add(northStart.heading);
        posPlayer1List.Add(westStart.position);
        headingPlayer1List.Add(westStart.heading);
        posPlayer2List.Add(southStart.position);
        headingPlayer2List.Add(southStart.heading);
        posPlayer2List.Add(eastStart.position);
        headingPlayer2List.Add(eastStart.heading);
        BuildEntityDictionary();
        SpawnWithExistingPositions();
    }

    public void StoreCurrentScenario()
    {
        if (ReplayMgr.inst != null && ReplayMgr.inst.isReplaying) return;
        TrainingState currentState = OpenOceanMain.inst != null ? OpenOceanMain.inst.currentTrainingState : TrainingState.NonAdaptive;
        ScenarioData data = new()
        {
            scenarioNumber = allScenarios.Count + 1,
            entityQuantities = new(entityQuantities),
            Player1Positions = new(posPlayer1List),
            Player1Headings = new(headingPlayer1List),
            Player2Positions = new(posPlayer2List),
            Player2Headings = new(headingPlayer2List),
            difficultyLevel = difficultyLevel,
            trainingState = currentState,
            isNeutralBaseAvailable = true,
            NeutralBasePositions = new(neutralEntityPositions),
            NeutralBaseHeadings = new(neutralEntityHeadings),
            winLoss = ScoreMgr.inst != null && ScoreMgr.inst.playerWon,
            winReason = ScoreMgr.inst != null ? ScoreMgr.inst.winReason : "Unknown",
            score = ScoreMgr.inst != null ? ScoreMgr.inst.score : 0f,
            totalTime = OpenOceanMain.inst != null ? OpenOceanMain.inst.playSessionDuration : 0f,
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
        BuildEntityDictionary();
        if (difficultyLevel <= difficultyRanges["easy"]) currentDifficulty = Difficulty.Easy;
        else if (difficultyLevel <= difficultyRanges["medium"]) currentDifficulty = Difficulty.Medium;
        else currentDifficulty = Difficulty.Hard;
        switch (data.trainingState)
        {
            case TrainingState.Adaptive: AdjustAdaptiveEntitySpeed(); break;
            case TrainingState.Tutorial:
            case TrainingState.PreTest:
            case TrainingState.PostTest:
            case TrainingState.NonAdaptive:
            default: AdjustNonAdaptiveEntitySpeed(); break;
        }
        SpawnWithExistingPositions();
        if (data.isNeutralBaseAvailable) SpawnNeutralEntitiesFromData(data);
        ApplyGreyOverlays();
    }

    public float CurrentDifficultyLevel => difficultyLevel;
    public Difficulty CurrentDifficulty => currentDifficulty;
    public Dictionary<EntityType, int> EntityDictionary => entityDict;
}
