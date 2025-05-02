using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
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
public enum Difficulty { Easy, Medium, Hard }

public class GameMgr : MonoBehaviour
{
    public static int reloadCount = 0;
    public static GameMgr inst;

    private void Awake()
    {
        Random.InitState(seed);
        inst = this;
        BuildEntityDictionary();
    }

    [SerializeField] private Button plusButton;
    [SerializeField] private Button minusButton;
    [SerializeField] private TextMeshProUGUI simSpeedButtonText;
    public float timeScale = 1;

    public Vector3 position;
    public float spread = 20;
    public float colNum = 10;
    public float initZ;

    [SerializeField] private List<EntityQuantity> entityQuantities = new List<EntityQuantity>();
    private Dictionary<EntityType, int> entityDict;
    [Range(1, 4)]
    [SerializeField] public int players;
    [SerializeField] public bool sameEnityForAll;

    // Seed for deterministic random position selection
    [SerializeField] public int seed = 10;

    void Start()
    {
        EntityMgr.inst.movableEntitiesRoot.SetActive(false);
        EntityMgr.inst.nonMoveableEntitiesRoot.SetActive(false);

        if (plusButton != null && minusButton != null)
        {
            plusButton.onClick.RemoveAllListeners();
            plusButton.onClick.AddListener(() => DeltaScale(1));
            minusButton.onClick.RemoveAllListeners();
            minusButton.onClick.AddListener(() => DeltaScale(-1));
        }
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Equals))
            DeltaScale(1);
        if (Input.GetKeyUp(KeyCode.Minus))
            DeltaScale(-1);
    }

    public void DeltaScale(float delta)
    {
        if (Time.timeScale + delta >= 0)
        {
            Time.timeScale += delta;
            Time.timeScale = Mathf.Clamp(Time.timeScale, 0, 16);
            if (simSpeedButtonText != null)
                simSpeedButtonText.text = Time.timeScale.ToString("0");
        }
    }

    public void Create100()
    {
        initZ = position.z;
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                Entity ent = EntityMgr.inst.CreateEntity(EntityType.PilotVessel, position, Vector3.zero);
                position.z += spread;
            }
            position.x += spread;
            position.z = initZ;
        }
        DistanceMgr.inst.Initialize();
    }

    public void InitMapMenu()
    {
        List<Entity> allEntities = new List<Entity>();
        Vector3 pos = Vector3.zero;
        Vector3 offset = new Vector3(100, 0, -50);

        foreach (GameObject go in EntityMgr.inst.entityPrefabs)
        {
            Entity ent = go.GetComponent<Entity>();
            ent = EntityMgr.inst.CreateEntity(ent.entityType, pos + offset, new Vector3(0, 270, 0));
            allEntities.Add(ent);
            pos.x += 400;
        }
        Vector3 movePos = new Vector3(-3000, 0, 0);
        bool shouldAdd = false;
        StartCoroutine(AddMoveCommandsToEnt(allEntities, movePos, shouldAdd, 270));
        movePos.x = 3000;
        shouldAdd = true;
        StartCoroutine(AddMoveCommandsToEnt(allEntities, movePos, shouldAdd));
    }

    IEnumerator AddMoveCommandsToEnt(List<Entity> allEntities, Vector3 movePos, bool shouldAdd, float heading = -1)
    {
        yield return new WaitForSeconds(0.1f);
        foreach (Entity ent in allEntities)
        {
            ent.heading = (heading == -1 ? ent.heading : heading);
        }
        AIMgr.inst.HandleMove(allEntities, movePos, shouldAdd);
    }

    public void MakeMapEntities()
    {
        Vector3 pos = new Vector3(0, 0, 0);
        Entity ent;
        foreach (TactPlayer player in PlayerMgr.inst.players)
        {
            for (int i = 0; i < 5; i++)
            {
                ent = EntityMgr.inst.CreateEntity(EntityType.SeaHunter, pos, Vector3.zero, player);
                pos.x += 50;
            }
            pos.z += 100;
            pos.x = 0;
        }
    }

    public Vector3 posPlayer1 = new Vector3(0, 0, -7000);
    public float headingPlayer1 = 0;
    public Vector3 posPlayer2 = new Vector3(0, 0, 10000);
    public float headingPlayer2 = 180;

    [Range(0f, 1f)]
    public float difficultyLevel = 0.2f;
    public Dictionary<string, float> difficultyRanges = new Dictionary<string, float>()
    {

    {"easy", 0.33f},

    {"medium", 0.667f},

    {"hard", 1f}

    };
    private Difficulty currentDifficulty;
    private int[,] positionRelations = new int[4, 3] {
        {1, 3, 2}, // North: opposite=1, right=3, left=2
        {0, 2, 3}, // South: opposite=0, right=2, left=3
        {3, 0, 1}, // West: opposite=3, right=0, left=1
        {2, 1, 0}  // East: opposite=2, right=1, left=0
    };
    void DetermineDifficulty()
    {
        if (difficultyLevel <= difficultyRanges["easy"]) currentDifficulty = Difficulty.Easy;
        else if (difficultyLevel <= difficultyRanges["medium"]) currentDifficulty = Difficulty.Medium;
        else currentDifficulty = Difficulty.Hard;
    }

    public void OpenOcean1x1()
    {
        InitializeScenario();
        SpawnEntities();
        CameraMgr.inst.SetCameraPosition();
    }

  void InitializeScenario()
    {
        DetermineDifficulty();
        if(currentDifficulty == Difficulty.Easy)
            EnemyAIMgr.inst.currentLevel = 1;
        else
            EnemyAIMgr.inst.currentLevel = 2;
        AdjustUnitCounts(); // Uses difficulty but varies with combinedSeed
    }



    void AdjustUnitCounts()
    {

        foreach (EntityQuantity eq in entityQuantities)
        {
            if (eq.entityType == EntityType.Rig_Balder)
            {
                eq.unitCount = 1; // Always set to 1 regardless of difficulty
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
            new() { position = new(0, 0, -7000), heading = 0 },  // North
            new() { position = new(0, 0, 7000), heading = 180 },   // South
            new() { position = new(-7000, 0, 0), heading = 90 },    // West
            new() { position = new(7000, 0, 0), heading = 270 }     // East
        };

        int player1Index = Random.Range(0, allPositions.Length);
        var player2Indices = GetValidPlayer2Positions(player1Index);

        StartingPosition p1 = allPositions[player1Index];
        posPlayer1 = p1.position;
        headingPlayer1 = p1.heading;
        StartingPosition p2 = allPositions[player2Indices[Random.Range(0, player2Indices.Count)]];
        posPlayer2 = p2.position;
        headingPlayer2 = p2.heading;
        SpawnEntitiesFromDictionary(p1.position, p1.heading, PlayerMgr.inst.player1);
        SpawnEntitiesFromDictionary(p2.position, p2.heading, PlayerMgr.inst.player2);
    }

    List<int> GetValidPlayer2Positions(int player1Index)
    {
        List<int> validPositions = new();
        switch (currentDifficulty)
        {
            case Difficulty.Easy:
                validPositions.Add(positionRelations[player1Index, 0]); // Opposite only
                break;
            case Difficulty.Medium:
                validPositions.Add(positionRelations[player1Index, 0]); // Opposite
                validPositions.Add(positionRelations[player1Index, 1]); // Right
                break;
            case Difficulty.Hard:
                for (int i = 0; i < 4; i++)
                    if (i != player1Index) validPositions.Add(i);
                break;
        }
        return validPositions;
    }

    void BuildEntityDictionary()
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

    public void SpawnEntitiesFromDictionary(Vector3 initPos, float initHeading, TactPlayer player)
    {
        List<EntityType> spawnQueue = new();
        foreach (EntityType type in priorityList)
            if (entityDict.TryGetValue(type, out int count))
                for (int j = 0; j < count; j++)
                    spawnQueue.Add(type);

        SpawnInFormation(spawnQueue, initPos, initHeading, player);
    }

    void SpawnInFormation(List<EntityType> queue, Vector3 center, float heading, TactPlayer player)
    {
        int index = 0;
        int ring = 1;

        if (queue.Count > 0)
        {
            EntityMgr.inst.CreateEntity(queue[index], center, new(0, heading, 0), player);
            index++;
        }

        while (index < queue.Count)
        {
            int positionsInRing = 8 * (ring - 1);
            float angleStep = 360f / positionsInRing;

            for (int pos = 0; pos < positionsInRing && index < queue.Count; pos++)
            {
                Vector3 offset = new Vector3(
                    Mathf.Cos(pos * angleStep * Mathf.Deg2Rad),
                    0,
                    Mathf.Sin(pos * angleStep * Mathf.Deg2Rad)
                ) * (500f * (ring - 1));

                EntityMgr.inst.CreateEntity(queue[index], center + offset,
                    new(0, heading, 0), player);
                index++;
            }
            ring++;
        }
    }


    public List<EntityType> priorityList = new List<EntityType>()
    {
        EntityType.Rig_Balder,
        EntityType.CVN75,
        EntityType.Submarine,
        EntityType.DDG51,
        EntityType.SeaHunter,
        EntityType.JARIUSV,
        EntityType.OrientExplorer,
        EntityType.MineSweeper,
        EntityType.PilotVessel,
        EntityType.Mykola,
        EntityType.Container,
        EntityType.OilServiceVessel,
        EntityType.Tanker,
        EntityType.TugBoat,
        EntityType.SeaBaby
    };
    [ContextMenu("Reload Scene")] // Creates an inspector context menu entry
    [ContextMenu("Reload Scene")]
    public void ReloadScene()
    {
        // Save current settings
        reloadCount++;

        // Clear all existing entities
        ClearAllEntities();

        // Reset game state
        ResetGameState();

        // Respawn entities
        OpenOcean1x1();
        

}

private void ClearAllEntities()
{
    // Clear weapons first with direct destruction
    if (WeaponsMgr.inst != null)
    {
        // Stop all weapon behaviors first
        WeaponsMgr.inst.StopAllWeapons();
        
        // Destroy weapons using a copy of the list
        var weaponsCopy = new List<Entity>(WeaponsMgr.inst.weapons);
        foreach (Entity weapon in weaponsCopy)
        {
            if (weapon != null && weapon.gameObject != null)
            {
                // Cancel any pending commands
                if (weapon.TryGetComponent<UnitAI>(out var unitAI))
                {
                    unitAI.StopAndRemoveAllCommands();
                }
                
                // Immediate destruction with null-check
                if (weapon.gameObject != null)
                {
                    DestroyImmediate(weapon.gameObject);
                }
            }
        }
        WeaponsMgr.inst.weapons.Clear();
    }

    // Clear all other entities
    if (EntityMgr.inst != null)
    {
        // Use a copy to avoid modification during iteration
        var entitiesCopy = new List<Entity>(EntityMgr.inst.entities);
        foreach (Entity entity in entitiesCopy)
        {
            if (entity != null && entity.gameObject != null)
            {
                // Stop AI first
                if (entity.TryGetComponent<UnitAI>(out var unitAI))
                {
                    unitAI.StopAndRemoveAllCommands();
                }
                
                // Immediate destruction with null-check
                if (entity.gameObject != null)
                {
                    DestroyImmediate(entity.gameObject);
                }
            }
        }
        EntityMgr.inst.entities.Clear();
    }

    // Clear selection
    if (SelectionMgr.inst != null)
    {
        SelectionMgr.inst.selectedEntities.Clear();
        SelectionMgr.inst.selectedEntity = null;
    }

    // Force cleanup
    System.GC.Collect();
    Resources.UnloadUnusedAssets();
    Physics.SyncTransforms();
}

    private void ResetGameState()
    {
        // Reset time scale
        Time.timeScale = 1f;
        if (simSpeedButtonText != null)
            simSpeedButtonText.text = "1";

        // Reset position
        position = Vector3.zero;
        initZ = 0;

        // Clear and rebuild entity dictionary
        // entityQuantities.Clear();
        BuildEntityDictionary();

        // Reset any other necessary game state variables
        if (AIMgr.inst != null)
        {
            AIMgr.inst.StopAllCoroutines();

        }

        // Reset distance manager
        if (DistanceMgr.inst != null)
        {
            DistanceMgr.inst.Initialize();
        }
        if (FogWarMgr.inst != null)
        {
            FogWarMgr.inst.ResetFog();
        }
        if (CameraMgr.inst != null)
        {
            CameraMgr.inst.ResetCamera();
        }
        if (ScoreMgr.inst != null)
        {
            ScoreMgr.inst.ResetScores();
        }
        if (OpenOceanMain.inst != null)
        {
            OpenOceanMain.inst.ResetGameState();
        }
        if (MinimapMgr.inst != null)
        {
            MinimapMgr.inst.ResetMinimap();
        }
}

   
    


}
