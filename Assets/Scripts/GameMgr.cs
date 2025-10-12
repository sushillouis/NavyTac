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
    public List<Vector3> Player1Positions = new();
    public List<float> Player1Headings = new();
    public List<Vector3> Player2Positions = new();
    public List<float> Player2Headings = new();
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

    public float min = 1;
    public float max = 5;

    [Header("Time Control UI")]
    [SerializeField] private Button plusButton;
    [SerializeField] private Button minusButton;
    [SerializeField] private List<TextMeshProUGUI> simSpeedButtonText;

    public float timeScale = 1;
    public bool isIntroPlaying = true;

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
            float relativeDisplay = displayedSpeedValue;
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

    public void OpenOcean1x1()
    {
        isIntroPlaying = true;

        if (ScenarioGenerator.inst == null)
        {
            Debug.LogError("ScenarioGenerator instance not found!");
            return;
        }

        if (OpenOceanMain.inst != null && OpenOceanMain.inst.currentTrainingState == TrainingState.Tutorial)
        {
            var tutorialScenario = ScenarioGenerator.inst.GenerateScenario(TrainingState.Tutorial);
            ScenarioGenerator.inst.SpawnScenario(tutorialScenario);
        }
        else
        {
            var scenario = ScenarioGenerator.inst.GenerateScenario(OpenOceanMain.inst?.currentTrainingState ?? TrainingState.PreTest);
            
            // Apply speed adjustments based on training state
            if (OpenOceanMain.inst?.currentTrainingState == TrainingState.Adaptive)
            {
                Time.timeScale = 1f;
                ScenarioGenerator.inst.AdjustAdaptiveEntitySpeed();
            }
            else
            {
                Time.timeScale = 1f;
                ScenarioGenerator.inst.AdjustNonAdaptiveEntitySpeed();
            }
            
            ScenarioGenerator.inst.SpawnScenario(scenario);
        }

        if (CameraMgr.inst != null) CameraMgr.inst.SetCameraPosition();
        if (ReplayMgr.inst != null) ReplayMgr.inst.StartNewScenario();
    }

    public void CreateTwoVsTwoScenario()
    {
        isIntroPlaying = true;
        
        if (ScenarioGenerator.inst != null)
        {
            ScenarioGenerator.inst.GenerateTwoVsTwoScenario();
        }
    }

    // Property accessors for compatibility with existing code
    public float difficultyLevel => ScenarioGenerator.inst?.CurrentDifficultyLevel ?? 0f;
    public Dictionary<EntityType, int> entityDict => ScenarioGenerator.inst?.EntityDictionary ?? new Dictionary<EntityType, int>();
    public List<EntityQuantity> entityQuantities => ScenarioGenerator.inst?.entityQuantities ?? new List<EntityQuantity>();

    // Scenario data management methods
    public void StoreCurrentScenario()
    {
        ScenarioGenerator.inst?.StoreCurrentScenario();
    }

    public void InitializeScenarioFromData(ScenarioData data)
    {
        ScenarioGenerator.inst?.InitializeScenarioFromData(data);
    }

    public ScenarioData GetScenario(int scenarioNumber)
    {
        return ScenarioGenerator.inst?.GetScenario(scenarioNumber);
    }
}
