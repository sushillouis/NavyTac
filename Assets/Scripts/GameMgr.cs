using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class EntityQuantity
{
    public EntityType entityType;
    public int unitCount;
}

[System.Serializable]
public struct StartingPosition
{
    public Vector3 position;
    public float heading;
}

[System.Serializable]
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

    // The implicit operator below might cause issues if ScenarioDataMgr.ScenarioData is not defined
    // or if it's not intended for direct serialization.
    // For showing data in the inspector, it's generally not needed.
    // public static implicit operator ScenarioData(ScenarioDataMgr.ScenarioData v)
    // {
    //     throw new System.NotImplementedException();
    // }
}

public class GameMgr : MonoBehaviour
{
    public static int reloadCount = 0;
    public static GameMgr inst;

    [Header("Seeds per Training State")]
    [Tooltip("Seed for the PreTest training state.")]
    [SerializeField] public int seedPreTest = 10;
    [Tooltip("Seed for the PostTest training state.")]
    [SerializeField] public int seedPostTest = 20;
    [Tooltip("Seed for the Adaptive training state.")]
    [SerializeField] public int seedAdaptive = 30;
    [Tooltip("Seed for the NonAdaptive training state.")]
    [SerializeField] public int seedNonAdaptive = 40;

    public float min = 0;
    public float max = 16;

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

    [Header("Time Control UI")]
    [SerializeField] private Button plusButton;
    [SerializeField] private Button minusButton;
    [SerializeField] private List<TextMeshProUGUI> simSpeedButtonText;

    public float timeScale = 1;

    [Header("Entity Spawning Parameters (Legacy/Test)")]
    [Tooltip("Base position for spawning entities in Create100.")]
    public Vector3 position;
    [Tooltip("Spread between entities in Create100.")]
    public float spread = 20;
    private float initZ;

    [Header("Scenario Entity Configuration")]
    [Tooltip("List of entity types and their quantities to spawn in scenarios.")]
    [SerializeField] public List<EntityQuantity> entityQuantities = new List<EntityQuantity>();
    public Dictionary<EntityType, int> entityDict;

    [Header("Player Configuration")]
    [Range(1, 4)]
    [SerializeField] public int players = 2;
    [SerializeField] public bool sameEnityForAll = false;

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
    public void PlusButtonClicked()
    {
        DeltaScale(1);
    }
    public void MinusButtonClicked()
    {
        DeltaScale(-1);
    }
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Equals) || Input.GetKeyUp(KeyCode.KeypadPlus))
            DeltaScale(1);
        if (Input.GetKeyUp(KeyCode.Minus) || Input.GetKeyUp(KeyCode.KeypadMinus))
            DeltaScale(-1);
    }

    public void DeltaScale(float delta = 0)
    {
        float newTimeScale = Time.timeScale + delta;
        Time.timeScale = Mathf.Clamp(newTimeScale, min, max);
        if (simSpeedButtonText != null)
        {
            float displayedSpeedValue = Time.timeScale - min + 1;
            foreach (TextMeshProUGUI text in simSpeedButtonText)
            {
                text.text = displayedSpeedValue.ToString("0");
            }
        }
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
                difficultyLevel = 0.6f; // Default value if no scores are available
                return difficultyLevel;
            }
            difficultyLevel = difficultyLevel + 0.05f * ScoreMgr.inst.playerScores[ScoreMgr.inst.playerScores.Count - 1] / 100f;
        }
        float clampedDifficulty = Mathf.Clamp(difficultyLevel, 0f, 1f);
        return clampedDifficulty;
    }


    [Header("Player Start Positions")]
    public Vector3 posPlayer1;
    public float headingPlayer1;
    public Vector3 posPlayer2;
    public float headingPlayer2;
    [Range(0f, 1f)]

    public float difficultyLevel = 0.2f;
    public Dictionary<string, float> difficultyRanges = new Dictionary<string, float>()
    {
        {"easy", 0.333f},
        {"medium", 0.667f},
        {"hard", 1.00f}
    };
    private Difficulty currentDifficulty;

    private int[,] positionRelations = new int[4, 3] {
        {1, 3, 2},
        {0, 2, 3},
        {3, 0, 1},
        {2, 1, 0}
    };

    public void OpenOcean1x1()
    {
        InitializeScenario();


        SpawnEntities();
        if (CameraMgr.inst != null) CameraMgr.inst.SetCameraPosition();

        if (ReplayMgr.inst != null)
        {
            ReplayMgr.inst.StartNewScenario();
        }
    }

    void InitializeScenario()
    {
        DetermineDifficulty();
        if (EnemyAIMgr.inst != null)
        {
            if (currentDifficulty == Difficulty.Easy)
                EnemyAIMgr.inst.currentLevel = 1;
            else if (currentDifficulty == Difficulty.Medium)
                EnemyAIMgr.inst.currentLevel = 2;
            else if (currentDifficulty == Difficulty.Hard)
                EnemyAIMgr.inst.currentLevel = 3;
        }
        if (OpenOceanMain.inst.currentTrainingState == TrainingState.Adaptive)
        {
            AdjustAdaptiveTimeScale();
            AdjustAdaptiveUnitCounts();
        }
        else
        {
            AdjustNonAdaptiveTimeScale();
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
    void AdjustAdaptiveTimeScale()
    {

        min = 1f + (difficultyLevel * 3);
        max = 6;
        DeltaScale(min - 1);

    }
    void AdjustNonAdaptiveTimeScale()
    {
        switch (currentDifficulty)
        {
            case Difficulty.Easy:
                min = 2;
                max = 6;
                DeltaScale(min - 1);
                Debug.Log("Easy difficulty: Time scale set to " + Time.timeScale);
                break;
            case Difficulty.Medium:
                min = 3;
                max = 6;
                DeltaScale(min - 1);
                Debug.Log("Medium difficulty: Time scale set to " + Time.timeScale);
                break;
            case Difficulty.Hard:
                min = 4;
                max = 6;
                DeltaScale(min - 1);
                Debug.Log("Hard difficulty: Time scale set to " + Time.timeScale);
                break;
            default:
                Time.timeScale = 1f;
                break;
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
                Difficulty.Easy => Random.Range(3, 6),
                Difficulty.Medium => Random.Range(6, 11),
                Difficulty.Hard => Random.Range(11, 21),
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

        int player1Index = Random.Range(0, allPositions.Length);
        StartingPosition p1StartPos = allPositions[player1Index];
        posPlayer1 = p1StartPos.position;
        headingPlayer1 = p1StartPos.heading;

        List<int> player2ValidIndices = GetValidPlayer2Positions(player1Index);
        int player2AssignedIndex = player1Index;
        if (player2ValidIndices.Count > 0)
        {
            player2AssignedIndex = player2ValidIndices[Random.Range(0, player2ValidIndices.Count)];
        }
        else
        {
            for (int i = 0; i < allPositions.Length; ++i) { if (i != player1Index) { player2AssignedIndex = i; break; } }
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
                validPositions.Add(positionRelations[player1Index, 0]);
                validPositions.Add(positionRelations[player1Index, 1]);
                break;
            case Difficulty.Hard:
                for (int i = 0; i < 4; i++)
                    if (i != player1Index) validPositions.Add(i);
                break;
        }
        return validPositions;
    }

    public void BuildEntityDictionary()
    {
        entityDict = new Dictionary<EntityType, int>();
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

        ScenarioData data = new ScenarioData
        {
            scenarioNumber = allScenarios.Count + 1,
            entityQuantities = new List<EntityQuantity>(entityQuantities),
            posPlayer1 = posPlayer1,
            headingPlayer1 = headingPlayer1,
            posPlayer2 = posPlayer2,
            headingPlayer2 = headingPlayer2,
            difficultyLevel = difficultyLevel,
            winLoss = ScoreMgr.inst != null && ScoreMgr.inst.playerWon,
            winReason = ScoreMgr.inst != null ? ScoreMgr.inst.winReason : "Unknown",
            score = ScoreMgr.inst != null ? ScoreMgr.inst.score : 0f,
            totalTime = OpenOceanMain.inst.playSessionDuration,
            timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        SaveScenario(data);
    }

    public void InitializeScenarioFromData(ScenarioData data)
    {
        entityQuantities = new List<EntityQuantity>(data.entityQuantities);
        // Correctly assign positions without swapping
        posPlayer1 = data.posPlayer1;
        headingPlayer1 = data.headingPlayer1;
        posPlayer2 = data.posPlayer2;
        headingPlayer2 = data.headingPlayer2;
        difficultyLevel = data.difficultyLevel;
        // Initialize with stored data
        InitializeScenario();
        SpawnWithExistingPositions();
    }
    public void SpawnWithExistingPositions()
    {
        if (PlayerMgr.inst != null)
        {
            SpawnEntityMgr.inst.SpawnEntitiesFromDictionary(
                posPlayer1,
                headingPlayer1,
                PlayerMgr.inst.player1
            );
            SpawnEntityMgr.inst.SpawnEntitiesFromDictionary(
                posPlayer2,
                headingPlayer2,
                PlayerMgr.inst.player2
            );
        }
    }

    public List<ScenarioData> allScenarios = new List<ScenarioData>();

    public void SaveScenario(ScenarioData data)
    {
        allScenarios.Add(data);
    }

    public ScenarioData GetScenario(int scenarioNumber)
    {
        return allScenarios.Find(s => s.scenarioNumber == scenarioNumber);
    }
    
    public void Create100()
    { }
}

    // public void Create100()
    // {
    //     initZ = position.z; 
    //     for (int i = 0; i < 10; i++) 
    //     {
    //         for (int j = 0; j < 10; j++) 
    //         {
    //        position.z += spr//     foreach (GameObject go in EntityMgr.inst.entityPrefabs)
    //     {
    //         Entity prefabComponent = go.GetComponent<Entity>();
    //         if (prefabComponent != null)
    //         {
    //             Entity ent = EntityMgr.inst.CreateEntity(prefabComponent.entityType, pos + offset, new Vector3(0, 270, 0)); 
    //             allEntities.Add(ent);
    //             pos.x += 400; 
    //         }
    //     }

    //     Vector3 movePos = new Vector3(-3000, 0, 0);
    //     StartCoroutine(AddMoveCommandsToEnt(allEntities, movePos, false, 270)); 
    //     movePos.x = 3000;
    //     StartCoroutine(AddMoveCommandsToEnt(allEntities, movePos, true)); 
    // }

    // IEnumerator AddMoveCommandsToEnt(List<Entity> entities, Vector3 targetPos, bool shouldAdd, float heading = -1)
    // {
    //     yield return new WaitForSeconds(0.1f); 
    //     foreach (Entity ent in entities)
    //     {
    //         if (ent != null && heading != -1) 
    //         {
    //             ent.heading = heading;
    //         }
    //     }
    //     if (AIMgr.inst != null) AIMgr.inst.HandleMove(entities, targetPos, shouldAdd);
    // }

    // public void MakeMapEntities()
    // {
    //     Vector3 pos = Vector3.zero;
    //     Entity ent;
    //     foreach (TactPlayer player in PlayerMgr.inst.players)
    //     {
    //         for (int i = 0; i < 5; i++) 
    //         {
    //             ent = EntityMgr.inst.CreateEntity(EntityType.SeaHunter, pos, Vector3.zero, player);
    //             pos.x += 50; 
    //         }
    //         pos.z += 100; 
    //         pos.x = 0;    
    //     }
    // }