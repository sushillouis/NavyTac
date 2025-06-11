using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class EntityQuantity
{
    public EntityType entityType;
    public int unitCount;
}

[Serializable]
public struct StartingPosition
{
    public Vector3 position;
    public float heading;
}

[Serializable]
public class ScenarioData
{
    public int scenarioNumber;
    public List<EntityQuantity> entityQuantities;
    public Vector3 posPlayer1;
    public float headingPlayer1;
    public Vector3 posPlayer2;
    public float headingPlayer2;
    public float difficultyLevel;
    public string timestamp;
    public bool winLoss;
    public string winReason;
    public float score;
    public float totalTime;
}

public class GameMgr : MonoBehaviour
{
    public static int reloadCount = 0;
    public static GameMgr inst;

    [Header("Seeds per Training State")]
    [SerializeField] public int seedPreTest = 10;
    [SerializeField] public int seedPostTest = 20;
    [SerializeField] public int seedAdaptive = 30;
    [SerializeField] public int seedNonAdaptive = 40;

    public float min = 2;
    public float max = 5;

    [Header("Time Control UI")]
    [SerializeField] private Button plusButton;
    [SerializeField] private Button minusButton;
    [SerializeField] private List<TextMeshProUGUI> simSpeedButtonText;

    public float timeScale = 1;

    [Header("Scenario Entity Configuration")]
    [SerializeField] public List<EntityQuantity> entityQuantities = new();
    public Dictionary<EntityType, int> entityDict;
    [Header("Player Start Positions")]
    public Vector3 posPlayer1;
    public float headingPlayer1;
    public Vector3 posPlayer2;
    public float headingPlayer2;

    [Range(0f, 1f)]
    public float difficultyLevel = 0.2f;
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

    float lastDisplayedSpeedValue = -10f;

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

    void Start()
    {
        if (EntityMgr.inst != null)
        {
            EntityMgr.inst.movableEntitiesRoot.SetActive(false);
            EntityMgr.inst.nonMoveableEntitiesRoot.SetActive(false);
        }

        if (plusButton != null)
        {
            plusButton.onClick.RemoveAllListeners();
            plusButton.onClick.AddListener(() => DeltaScale(1));
        }
        if (minusButton != null)
        {
            minusButton.onClick.RemoveAllListeners();
            minusButton.onClick.AddListener(() => DeltaScale(-1));
        }
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Equals) || Input.GetKeyUp(KeyCode.KeypadPlus))
            DeltaScale(1);
        if (Input.GetKeyUp(KeyCode.Minus) || Input.GetKeyUp(KeyCode.KeypadMinus))
            DeltaScale(-1);

        float displayedSpeedValue = Time.timeScale;
        if (displayedSpeedValue != lastDisplayedSpeedValue)
        {
            lastDisplayedSpeedValue = displayedSpeedValue;
            float relativeDisplay = displayedSpeedValue - min + 1f;
            foreach (TextMeshProUGUI text in simSpeedButtonText)
            {
                text.text = relativeDisplay.ToString("0");
            }
        }
    }

    public int GetSelectedSeed()
    {
        int selectedSeed = seedPreTest;
        if (OpenOceanMain.inst != null)
        {
            switch (OpenOceanMain.inst.currentTrainingState)
            {
                case TrainingState.PreTest:
                    selectedSeed = seedPreTest;
                    break;
                case TrainingState.PostTest:
                    selectedSeed = seedPostTest;
                    break;
                case TrainingState.Adaptive:
                    selectedSeed = seedAdaptive;
                    break;
                case TrainingState.NonAdaptive:
                    selectedSeed = seedNonAdaptive;
                    break;
                default:
                    selectedSeed = seedPreTest;
                    break;
            }
        }
        return selectedSeed;
    }

    public void PlusButtonClicked() => DeltaScale(1);
    public void MinusButtonClicked() => DeltaScale(-1);

    public void DeltaScale(float delta = 0)
    {
        float newTimeScale = Time.timeScale + delta;
        Time.timeScale = Mathf.Clamp(newTimeScale, min, max);
        ReplayCommand cmd = new()
        {
            timestamp = Time.time,
            timeScale = Time.timeScale,
            commandType = "TimeScaleChange",
        };
        ReplayMgr.inst.RecordCommand(cmd);
    }

    void DetermineDifficulty()
    {
        if (OpenOceanMain.inst == null)
        {
            difficultyLevel = difficultyRanges["easy"];
            currentDifficulty = Difficulty.Easy;
            return;
        }

        if (OpenOceanMain.inst.lobbyState == LobbyState.Replay)
        {
            if (difficultyLevel <= difficultyRanges["easy"]) currentDifficulty = Difficulty.Easy;
            else if (difficultyLevel <= difficultyRanges["medium"]) currentDifficulty = Difficulty.Medium;
            else currentDifficulty = Difficulty.Hard;
            return;
        }

        switch (OpenOceanMain.inst.currentTrainingState)
        {
            case TrainingState.PreTest:
                difficultyLevel = 0.2f;
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
            default:
                difficultyLevel = 0.2f;
                break;
        }

        if (difficultyLevel <= difficultyRanges["easy"]) currentDifficulty = Difficulty.Easy;
        else if (difficultyLevel <= difficultyRanges["medium"]) currentDifficulty = Difficulty.Medium;
        else currentDifficulty = Difficulty.Hard;
    }

    float ComputeAdaptiveDifficulty()
    {
        if (ScoreMgr.inst != null)
        {
            if (ScoreMgr.inst.playerScores.Count == 0)
            {
                difficultyLevel = 0.2f;
                return difficultyLevel;
            }
            difficultyLevel += 0.07f * ScoreMgr.inst.playerScores[^1] / 100f;
        }
        return Mathf.Clamp(difficultyLevel, 0f, 1f);
    }

    public void OpenOcean1x1()
    {
        if (OpenOceanMain.inst != null && OpenOceanMain.inst.currentTrainingState == TrainingState.Tutorial)
        {
            InitializeTrainingScenario();
            SpawnTrainingEntities();
        }
        else
        {
            InitializeScenario();
            SpawnEntities();
        }
        if (CameraMgr.inst != null) CameraMgr.inst.SetCameraPosition();
        if (ReplayMgr.inst != null) ReplayMgr.inst.StartNewScenario();
    }

    // Called when in Tutorial training state
    public void InitializeTrainingScenario()
    {

        DetermineDifficulty();
    }

    // Called when in Tutorial training state
    public void SpawnTrainingEntities()
    {
        // Spawn just 1 of each entity type for the tutorial
        if (PlayerMgr.inst != null)
        {
            foreach (EntityQuantity eq in entityQuantities)
            {
                eq.unitCount = 1;
            }
            BuildEntityDictionary();
            posPlayer1 = new Vector3(0, 0, 0);
            headingPlayer1 = 0f; // Facing North
            posPlayer2 = new Vector3(0, 0, 7000);
            headingPlayer2 = 180f; // Facing South
            SpawnEntityMgr.inst.SpawnEntitiesFromDictionary(posPlayer1, headingPlayer1, PlayerMgr.inst.player1);
            SpawnEntityMgr.inst.SpawnEntitiesFromDictionary(posPlayer2, headingPlayer2, PlayerMgr.inst.player2);
            // Optionally spawn player2 or other tutorial-specific entities as needed
        }
    }

    void InitializeScenario()
    {
        DetermineDifficulty();
        if (EnemyAIMgr.inst != null)
        {
            EnemyAIMgr.inst.currentLevel = currentDifficulty switch
            {
                Difficulty.Easy => 1,
                Difficulty.Medium => 2,
                Difficulty.Hard => 3,
                _ => 1
            };
        }
        if (OpenOceanMain.inst.currentTrainingState == TrainingState.Adaptive)
        {
            Time.timeScale = 2f;
            AdjustAdaptiveUnitCounts();
        }
        else
        {
            Time.timeScale = 2f;
            AdjustNonAdaptiveUnitCounts();
        }
    }

    void AdjustAdaptiveUnitCounts()
    {
        foreach (EntityQuantity eq in entityQuantities)
        {
            if (eq.entityType == EntityType.Rig_Balder)
            {
                eq.unitCount = 1;
                continue;
            }
            eq.unitCount = Mathf.RoundToInt(21.25f * difficultyLevel - 1.25f);
        }
        BuildEntityDictionary();
    }

    public void AdjustAdaptiveEntitySpeed()
    {
        float range = 2f;
        float speedFactor = 1f + difficultyLevel * range;

        foreach (GameObject prefabGo in EntityMgr.inst.entityPrefabs)
        {
            var prefab = prefabGo.GetComponent<Entity>();
            if (prefab == null || prefab.entityType == EntityType.Rig_Balder) continue;

            SetAccelerationAndTurnRate(prefab, speedFactor);

            Debug.Log($"Adaptive {prefab.entityType}: speed×{speedFactor:0.00}, accel={prefab.acceleration:0.00}, turn={prefab.turnRate:0.00}");
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

            SetAccelerationAndTurnRate(prefab, baseFactor);

            Debug.Log($"NonAdaptive {prefab.entityType}: speed×{baseFactor:0.00}, accel={prefab.acceleration:0.00}, turn={prefab.turnRate:0.00}");
        }
    }

    private void SetAccelerationAndTurnRate(Entity entity, float speedFactor)
    {
        entity.maxSpeed = entity.originalMaxSpeed * speedFactor;
        float stopDistance = 100f;
        if (entity.maxSpeed > 0.01f)
        {
            entity.acceleration = (entity.maxSpeed * entity.maxSpeed) / (2f * stopDistance);
            if (entity.entityType == EntityType.AntiShipMissile)
            {
                entity.turnRate = entity.originalTurnRate;
            }
            else
            {
                entity.turnRate = Mathf.Rad2Deg * (entity.acceleration / entity.maxSpeed);
            }
        }
        else
        {
            entity.acceleration = 0f;
            entity.turnRate = 0f;
        }
    }

    void AdjustNonAdaptiveUnitCounts()
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

    void SpawnEntities()
    {
        StartingPosition[] allPositions = new StartingPosition[]
        {
            new() { position = new(0, 0, -7000), heading = 0 },
            new() { position = new(0, 0, 7000),  heading = 180 },
            new() { position = new(-7000, 0, 0), heading = 90 },
            new() { position = new(7000, 0, 0),  heading = 270 }
        };

        int player1Index = UnityEngine.Random.Range(0, allPositions.Length);
        StartingPosition p1StartPos = allPositions[player1Index];
        posPlayer1 = p1StartPos.position;
        headingPlayer1 = p1StartPos.heading;

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
        posPlayer2 = p2StartPos.position;
        headingPlayer2 = p2StartPos.heading;

        if (PlayerMgr.inst != null)
        {
            SpawnEntityMgr.inst.SpawnEntitiesFromDictionary(p1StartPos.position, p1StartPos.heading, PlayerMgr.inst.player1);
            SpawnEntityMgr.inst.SpawnEntitiesFromDictionary(p2StartPos.position, p2StartPos.heading, PlayerMgr.inst.player2);
        }
    }

    List<int> GetValidPlayer2Positions(int player1Index)
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
            if (entityDict.ContainsKey(eq.entityType))
                entityDict[eq.entityType] += eq.unitCount;
            else
                entityDict.Add(eq.entityType, eq.unitCount);
        }
    }

    public void StoreCurrentScenario()
    {
        if (ReplayMgr.inst != null && ReplayMgr.inst.isReplaying) return;

        ScenarioData data = new()
        {
            scenarioNumber = allScenarios.Count + 1,
            entityQuantities = new(entityQuantities),
            posPlayer1 = posPlayer1,
            headingPlayer1 = headingPlayer1,
            posPlayer2 = posPlayer2,
            headingPlayer2 = headingPlayer2,
            difficultyLevel = difficultyLevel,
            winLoss = ScoreMgr.inst != null && ScoreMgr.inst.playerWon,
            winReason = ScoreMgr.inst != null ? ScoreMgr.inst.winReason : "Unknown",
            score = ScoreMgr.inst != null ? ScoreMgr.inst.score : 0f,
            totalTime = OpenOceanMain.inst.playSessionDuration,
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        SaveScenario(data);
    }

    public void InitializeScenarioFromData(ScenarioData data)
    {
        entityQuantities = new(data.entityQuantities);
        posPlayer1 = data.posPlayer1;
        headingPlayer1 = data.headingPlayer1;
        posPlayer2 = data.posPlayer2;
        headingPlayer2 = data.headingPlayer2;
        difficultyLevel = data.difficultyLevel;
        InitializeScenario();
        SpawnWithExistingPositions();
    }

    public void SpawnWithExistingPositions()
    {
        if (PlayerMgr.inst != null)
        {
            SpawnEntityMgr.inst.SpawnEntitiesFromDictionary(posPlayer1, headingPlayer1, PlayerMgr.inst.player1);
            SpawnEntityMgr.inst.SpawnEntitiesFromDictionary(posPlayer2, headingPlayer2, PlayerMgr.inst.player2);
        }
    }

    public void SaveScenario(ScenarioData data) => allScenarios.Add(data);

    public ScenarioData GetScenario(int scenarioNumber) =>
        allScenarios.Find(s => s.scenarioNumber == scenarioNumber);

    public void Create100() { }
}
