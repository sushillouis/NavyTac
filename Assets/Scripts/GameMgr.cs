using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Defines a type of entity and the quantity to be spawned.
/// Used for configuring scenario entity counts.
/// </summary>
[System.Serializable]
public class EntityQuantity
{
    public EntityType entityType;
    public int unitCount;
}

/// <summary>
/// Represents a starting position and heading for a group of entities.
/// </summary>
[System.Serializable]
public struct StartingPosition
{
    public Vector3 position;
    public float heading;
}

/// <summary>
/// Defines the difficulty levels for the game.
/// </summary>
public enum Difficulty { Easy, Medium, Hard }

/// <summary>
/// GameMgr (Game Manager) is a singleton class responsible for overall game flow,
/// scenario setup, difficulty management, entity spawning, and scene reloading.
/// </summary>
public class GameMgr : MonoBehaviour
{
    /// <summary>
    /// Static counter for the number of times the scene has been reloaded.
    /// </summary>
    public static int reloadCount = 0;
    /// <summary>
    /// Singleton instance of the GameMgr.
    /// </summary>
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

    /// <summary>
    /// Gets the random seed based on the current training state from OpenOceanMain.
    /// </summary>
    /// <returns>The selected integer seed.</returns>
    public int GetSelectedSeed()
    {
        int selectedSeed = seedPreTest; // Default to PreTest seed
        if (OpenOceanMain.inst != null)
        {
            switch (OpenOceanMain.inst.currentTrainingState)
            {
                case OpenOceanMain.TrainingState.PreTest:
                    selectedSeed = seedPreTest;
                    break;
                case OpenOceanMain.TrainingState.PostTest:
                    selectedSeed = seedPostTest;
                    break;
                case OpenOceanMain.TrainingState.Adaptive:
                    selectedSeed = seedAdaptive;
                    break;
                case OpenOceanMain.TrainingState.NonAdaptive:
                    selectedSeed = seedNonAdaptive;
                    break;
                default:
                    selectedSeed = seedPreTest; 
                    break;
            }
        }
        else
        {
            // selectedSeed remains seedPreTest or you can set a generic default
        }
        return selectedSeed;
    }

    private void Awake()
    {
        // Initialize singleton instance
        if (inst == null)
        {
            inst = this;
        }
        else if (inst != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // // Initialize random state with the selected seed
       // Build the initial entity dictionary based on inspector settings
        BuildEntityDictionary();
    }

    [Header("Time Control UI")]
    [SerializeField] private Button plusButton;
    [SerializeField] private Button minusButton;
    [SerializeField] private TextMeshProUGUI simSpeedButtonText;
    
    /// <summary>
    /// Current simulation time scale. Note: Unity's Time.timeScale is used directly.
    /// This field seems to be a conceptual placeholder or for initial setup.
    /// </summary>
    public float timeScale = 1; // Unity's Time.timeScale is the effective one.

    [Header("Entity Spawning Parameters (Legacy/Test)")]
    [Tooltip("Base position for spawning entities in Create100.")]
    public Vector3 position;
    [Tooltip("Spread between entities in Create100.")]
    public float spread = 20;
    // public float colNum = 10; // Appears unused, consider removing if confirmed.
    private float initZ; // Used internally by Create100

    [Header("Scenario Entity Configuration")]
    [Tooltip("List of entity types and their quantities to spawn in scenarios.")]
    [SerializeField] public List<EntityQuantity> entityQuantities = new List<EntityQuantity>();
    private Dictionary<EntityType, int> entityDict; // Internal dictionary built from entityQuantities

    [Header("Player Configuration")]
    [Range(1, 4)]
    [SerializeField] public int players = 2; // Default or max players for some scenarios
    [SerializeField] public bool sameEnityForAll = false; // If all players get the same entity composition

    void Start()
    {
        // Deactivate entity root objects initially
        if (EntityMgr.inst != null)
        {
            EntityMgr.inst.movableEntitiesRoot.SetActive(false);
            EntityMgr.inst.nonMoveableEntitiesRoot.SetActive(false);
        }

        // Setup UI button listeners for time scale control
        if (plusButton != null)
        {
            plusButton.onClick.RemoveAllListeners(); // Ensure no duplicate listeners
            plusButton.onClick.AddListener(() => DeltaScale(1));
        }
        if (minusButton != null)
        {
            minusButton.onClick.RemoveAllListeners(); // Ensure no duplicate listeners
            minusButton.onClick.AddListener(() => DeltaScale(-1));
        }
    }

    void Update()
    {
        // Keyboard shortcuts for time scale control
        if (Input.GetKeyUp(KeyCode.Equals) || Input.GetKeyUp(KeyCode.KeypadPlus)) // Added KeypadPlus
            DeltaScale(1);
        if (Input.GetKeyUp(KeyCode.Minus) || Input.GetKeyUp(KeyCode.KeypadMinus)) // Added KeypadMinus
            DeltaScale(-1);
    }

    /// <summary>
    /// Adjusts the game's time scale.
    /// </summary>
    /// <param name="delta">The amount to change the time scale by.</param>
    public void DeltaScale(float delta)
    {
        float newTimeScale = Time.timeScale + delta;
        // Clamp time scale between 0 and 16 (or a configurable max)
        Time.timeScale = Mathf.Clamp(newTimeScale, 0, 16); 
        if (simSpeedButtonText != null)
            simSpeedButtonText.text = Time.timeScale.ToString("0");
    }

    /// <summary>
    /// Determines and sets the game's difficulty level based on the current training state.
    /// </summary>
    void DetermineDifficulty()
    {
        if (OpenOceanMain.inst == null)
        {
            difficultyLevel = difficultyRanges["easy"]; // Default to easy
        }
        else
        {
            switch (OpenOceanMain.inst.currentTrainingState)
            {
                case OpenOceanMain.TrainingState.PreTest:
                    difficultyLevel = .2f;
                    break;
                case OpenOceanMain.TrainingState.PostTest:
                    // Example: Difficulty increases in the latter part of PostTest
                    if (OpenOceanMain.inst.gamePlayCountMAX > 0 && 
                        OpenOceanMain.inst.gamesPlayedCount < OpenOceanMain.inst.gamePlayCountMAX * 0.6f)
                    {
                        difficultyLevel =.2f;
                    }
                    else
                    {
                        difficultyLevel = 0.5f;
                    }
                    break;
                case OpenOceanMain.TrainingState.Adaptive:
                    difficultyLevel = ComputeAdaptiveDifficulty(); // Custom logic for adaptive difficulty
                    break;
                case OpenOceanMain.TrainingState.NonAdaptive:
                    difficultyLevel = .2f; // Or specific logic for NonAdaptive
                    break;
                default:
                    difficultyLevel = .2f;
                    break;
            }
        }

        // Convert the float difficultyLevel to the Difficulty enum
        if (difficultyLevel <= difficultyRanges["easy"]) currentDifficulty = Difficulty.Easy;
        else if (difficultyLevel <= difficultyRanges["medium"]) currentDifficulty = Difficulty.Medium;
        else currentDifficulty = Difficulty.Hard;
    }

    /// <summary>
    /// Computes difficulty adaptively, for example, based on player score.
    /// </summary>
    /// <returns>The computed adaptive difficulty level (0-1).</returns>
    float ComputeAdaptiveDifficulty()
    {
        // Example: Increase difficulty slightly based on score
        // Ensure ScoreMgr.inst and score are valid before use
        if (ScoreMgr.inst != null)
        {
            difficultyLevel = difficultyLevel + 0.05f * ScoreMgr.inst.score / 100;
        }
        float clampedDifficulty = Mathf.Clamp(difficultyLevel, 0f, 1f); // Clamp between 0 and 1
        return clampedDifficulty; 
    }

    /// <summary>
    /// Test function to create 100 entities in a grid formation.
    /// </summary>
    public void Create100()
    {
        initZ = position.z; // Store initial Z position
        for (int i = 0; i < 10; i++) // Rows
        {
            for (int j = 0; j < 10; j++) // Columns
            {
                EntityMgr.inst.CreateEntity(EntityType.PilotVessel, position, Vector3.zero);
                position.z += spread; // Move along Z for next entity in column
            }
            position.x += spread; // Move along X for next row
            position.z = initZ;   // Reset Z for next row
        }
        if (DistanceMgr.inst != null) DistanceMgr.inst.Initialize(); // Re-initialize distance manager
    }

    /// <summary>
    /// Initializes entities for a map menu display.
    /// </summary>
    public void InitMapMenu()
    {
        List<Entity> allEntities = new List<Entity>();
        Vector3 pos = Vector3.zero;
        Vector3 offset = new Vector3(100, 0, -50); // Initial offset for the first entity

        // Create one of each entity prefab for display
        foreach (GameObject go in EntityMgr.inst.entityPrefabs)
        {
            Entity prefabComponent = go.GetComponent<Entity>();
            if (prefabComponent != null)
            {
                Entity ent = EntityMgr.inst.CreateEntity(prefabComponent.entityType, pos + offset, new Vector3(0, 270, 0)); // Spawn facing a direction
                allEntities.Add(ent);
                pos.x += 400; // Space out entities
            }
        }

        // Example movement commands for the menu entities
        Vector3 movePos = new Vector3(-3000, 0, 0);
        StartCoroutine(AddMoveCommandsToEnt(allEntities, movePos, false, 270)); // Move left
        movePos.x = 3000;
        StartCoroutine(AddMoveCommandsToEnt(allEntities, movePos, true)); // Move right (additive)
    }

    /// <summary>
    /// Coroutine to add movement commands to a list of entities after a short delay.
    /// </summary>
    IEnumerator AddMoveCommandsToEnt(List<Entity> entities, Vector3 targetPos, bool shouldAdd, float heading = -1)
    {
        yield return new WaitForSeconds(0.1f); // Short delay
        foreach (Entity ent in entities)
        {
            if (ent != null && heading != -1) // Optionally set heading
            {
                ent.heading = heading;
            }
        }
        if (AIMgr.inst != null) AIMgr.inst.HandleMove(entities, targetPos, shouldAdd);
    }

    /// <summary>
    /// Spawns a predefined set of entities for each player. (Likely for testing or specific scenarios)
    /// </summary>
    public void MakeMapEntities()
    {
        Vector3 pos = Vector3.zero;
        Entity ent;
        foreach (TactPlayer player in PlayerMgr.inst.players)
        {
            for (int i = 0; i < 5; i++) // Spawn 5 SeaHunters per player
            {
                ent = EntityMgr.inst.CreateEntity(EntityType.SeaHunter, pos, Vector3.zero, player);
                pos.x += 50; // Offset each entity
            }
            pos.z += 100; // Offset for next player's entities
            pos.x = 0;    // Reset X offset
        }
    }

    [Header("Player Start Positions")]
    public Vector3 posPlayer1 = new Vector3(0, 0, -7000);
    public float headingPlayer1 = 0;
    public Vector3 posPlayer2 = new Vector3(0, 0, 10000); // Note: Original was 7000, then 10000. Using 10000 from latest.
    public float headingPlayer2 = 180;

    [Header("Difficulty Settings")]
    [Range(0f, 1f)]
    [Tooltip("Current difficulty level (0=Easy, 1=Hard). Can be set by DetermineDifficulty.")]
    public float difficultyLevel = 0.2f; 
    [Tooltip("Defines the thresholds for Easy, Medium, and Hard difficulty levels.")]
    public Dictionary<string, float> difficultyRanges = new Dictionary<string, float>()
    {
        {"easy", 0.33f},
        {"medium", 0.667f},
        {"hard", 1f}
    };
    private Difficulty currentDifficulty; // The current difficulty enum, derived from difficultyLevel

    /// <summary>
    /// Defines relationships between starting positions for Player 2 based on Player 1's position and difficulty.
    /// Rows: Player 1's starting position index.
    /// Columns: Difficulty-based choices for Player 2's position index (e.g., [EasyChoice, MediumChoice1, MediumChoice2/HardChoice1...]).
    /// Values are indices into the `allPositions` array in `SpawnEntities`.
    /// Example: If P1 is at index 0, Easy P2 is at index 1, Medium P2 can be 1 or 3, Hard P2 can be 1, 3, or 2.
    /// </summary>
    private int[,] positionRelations = new int[4, 3] {
        // P1 Index | P2 Easy | P2 Med | P2 Hard (or another Med)
        {1, 3, 2},  // P1 at pos 0: P2 can be at pos 1 (Easy), pos 3 (Med), pos 2 (Hard/Med)
        {0, 2, 3},  // P1 at pos 1: P2 can be at pos 0 (Easy), pos 2 (Med), pos 3 (Hard/Med)
        {3, 0, 1},  // P1 at pos 2: P2 can be at pos 3 (Easy), pos 0 (Med), pos 1 (Hard/Med)
        {2, 1, 0}   // P1 at pos 3: P2 can be at pos 2 (Easy), pos 1 (Med), pos 0 (Hard/Med)
    };

    /// <summary>
    /// Main function to set up and start a 1v1 Open Ocean scenario.
    /// </summary>
    public void OpenOcean1x1()
    {
        InitializeScenario(); // Set up difficulty, unit counts, AI levels
        SpawnEntities();      // Spawn player entities
        if (CameraMgr.inst != null) CameraMgr.inst.SetCameraPosition(); // Adjust camera
    }

    /// <summary>
    /// Initializes core scenario parameters before entity spawning.
    /// This includes re-initializing the random seed, determining difficulty,
    /// setting AI levels, and adjusting unit counts.
    /// </summary>
    void InitializeScenario()
    {
                
        DetermineDifficulty(); // Sets currentDifficulty and difficultyLevel

        // Set enemy AI level based on the determined difficulty
        if (EnemyAIMgr.inst != null)
        {
            if(currentDifficulty == Difficulty.Easy)
                EnemyAIMgr.inst.currentLevel = 1;
            else if (currentDifficulty == Difficulty.Medium)
                EnemyAIMgr.inst.currentLevel = 2;
            else if (currentDifficulty == Difficulty.Hard)
                EnemyAIMgr.inst.currentLevel = 3;
        }
        
        AdjustUnitCounts(); // Adjust entity counts based on difficulty
    }

    /// <summary>
    /// Adjusts the number of units for each entity type based on the current difficulty.
    /// </summary>
    void AdjustUnitCounts()
    {
        foreach (EntityQuantity eq in entityQuantities)
        {
            // Special case: Rig_Balder count is fixed (e.g., objective unit)
            if (eq.entityType == EntityType.Rig_Balder)
            {
                eq.unitCount = 1; 
                continue; // Skip dynamic adjustment for this type
            }

            // Adjust unit count based on difficulty using random ranges
            eq.unitCount = currentDifficulty switch
            {
                Difficulty.Easy   => Random.Range(3, 6),    // 3-5 units
                Difficulty.Medium => Random.Range(6, 11),   // 6-10 units
                Difficulty.Hard   => Random.Range(11, 21),  // 11-20 units
                _                 => eq.unitCount           // Default: no change
            };
        }
        BuildEntityDictionary(); // Rebuild dictionary with new counts
    }

    /// <summary>
    /// Spawns entities for both players at randomly selected starting positions,
    /// considering the current difficulty for Player 2's placement.
    /// </summary>
    void SpawnEntities()
    {
        // Define all possible starting positions and headings
        StartingPosition[] allPositions = new StartingPosition[]
        {
            new() { position = new(0, 0, -7000), heading = 0 },    // South, heading North
            new() { position = new(0, 0, 7000),  heading = 180 },  // North, heading South
            new() { position = new(-7000, 0, 0), heading = 90 },   // West,  heading East
            new() { position = new(7000, 0, 0),  heading = 270 }   // East,  heading West
        };

        // Randomly select Player 1's starting position
        int player1Index = Random.Range(0, allPositions.Length);
        StartingPosition p1StartPos = allPositions[player1Index];
        posPlayer1 = p1StartPos.position; // Update GameMgr's record
        headingPlayer1 = p1StartPos.heading;

        // Determine valid positions for Player 2 based on Player 1's position and difficulty
        List<int> player2ValidIndices = GetValidPlayer2Positions(player1Index);
        int player2AssignedIndex = player1Index; // Default to P1's index if list is empty (should not happen with current logic)
        if (player2ValidIndices.Count > 0)
        {
            player2AssignedIndex = player2ValidIndices[Random.Range(0, player2ValidIndices.Count)];
        }
        else
        {
            // Fallback: pick any position not P1's, or opposite if possible
            for(int i=0; i < allPositions.Length; ++i) { if (i != player1Index) { player2AssignedIndex = i; break; } }
        }
        
        StartingPosition p2StartPos = allPositions[player2AssignedIndex];
        posPlayer2 = p2StartPos.position; // Update GameMgr's record
        headingPlayer2 = p2StartPos.heading;

        // Spawn entities for each player
        if (PlayerMgr.inst != null)
        {
            SpawnEntitiesFromDictionary(p1StartPos.position, p1StartPos.heading, PlayerMgr.inst.player1);
            SpawnEntitiesFromDictionary(p2StartPos.position, p2StartPos.heading, PlayerMgr.inst.player2);
        }
    }

    /// <summary>
    /// Gets a list of valid starting position indices for Player 2,
    /// based on Player 1's starting position index and the current game difficulty.
    /// </summary>
    /// <param name="player1Index">The index of Player 1's starting position in `allPositions`.</param>
    /// <returns>A list of valid indices for Player 2's starting position.</returns>
    List<int> GetValidPlayer2Positions(int player1Index)
    {
        List<int> validPositions = new();
        // positionRelations: [P1_idx, P2_Easy_Choice, P2_Medium_Choice, P2_Hard_Choice_or_other_Medium]
        switch (currentDifficulty) 
        {
            case Difficulty.Easy:
                // Easy: Player 2 takes the "opposite" or predefined "easy" position.
                validPositions.Add(positionRelations[player1Index, 0]); 
                break;
            case Difficulty.Medium:
                // Medium: Player 2 can be in one of two positions (e.g., opposite or one adjacent).
                validPositions.Add(positionRelations[player1Index, 0]); 
                validPositions.Add(positionRelations[player1Index, 1]); 
                break;
            case Difficulty.Hard:
                // Hard: Player 2 can be in any position except Player 1's.
                // The positionRelations table's third column can be used, or simply all other positions.
                // Using all other positions for maximum variability in Hard.
                for (int i = 0; i < 4; i++) // Assuming 4 total starting positions
                    if (i != player1Index) validPositions.Add(i);
                break;
        }
        return validPositions;
    }

    /// <summary>
    /// Builds (or rebuilds) the internal dictionary of entity types and their counts
    /// from the public `entityQuantities` list.
    /// </summary>
    void BuildEntityDictionary()
    {
        entityDict = new Dictionary<EntityType, int>();
        foreach (EntityQuantity eq in entityQuantities)
        {
            // Special handling for Rig_Balder: ensure count is at most 1.
            if (eq.entityType == EntityType.Rig_Balder)
            {
                entityDict[eq.entityType] = Mathf.Min(eq.unitCount, 1); 
                continue; // Move to next item
            }

            // Add or update count for other entity types
            if (entityDict.ContainsKey(eq.entityType))
                entityDict[eq.entityType] += eq.unitCount;
            else
                entityDict.Add(eq.entityType, eq.unitCount);
        }
    }

    /// <summary>
    /// Spawns entities for a given player based on the `entityDict` counts,
    /// using the specified initial position and heading. Entities are spawned in formation.
    /// </summary>
    /// <param name="initPos">The center position for the formation.</param>
    /// <param name="initHeading">The initial heading for all spawned entities.</param>
    /// <param name="player">The player to whom these entities belong.</param>
    public void SpawnEntitiesFromDictionary(Vector3 initPos, float initHeading, TactPlayer player)
    {
        List<EntityType> spawnQueue = new();
        // Populate spawn queue based on priority and counts from entityDict
        foreach (EntityType type in priorityList) // Assumes priorityList is defined
        {
            if (entityDict.TryGetValue(type, out int count))
            {
                for (int j = 0; j < count; j++)
                {
                    spawnQueue.Add(type);
                }
            }
        }
        
        if (player == null)
        {
            return;
        }
        SpawnInFormation(spawnQueue, initPos, initHeading, player);
    }

    /// <summary>
    /// Spawns a list of entities in a circular/ring formation around a center point.
    /// </summary>
    /// <param name="queue">List of entity types to spawn.</param>
    /// <param name="center">Center point of the formation.</param>
    /// <param name="heading">Initial heading for all entities.</param>
    /// <param name="player">The owning player.</param>
    void SpawnInFormation(List<EntityType> queue, Vector3 center, float heading, TactPlayer player)
    {
        int index = 0; // Current entity index in the queue
        int ring = 1;  // Current ring number (1 is center, 2 is first outer ring, etc.)

        // Spawn the first entity at the center if the queue is not empty
        if (queue.Count > 0)
        {
            EntityMgr.inst.CreateEntity(queue[index], center, new Vector3(0, heading, 0), player);
            index++;
        }
        
        // Spawn remaining entities in concentric rings
        while (index < queue.Count)
        {
            // Skip ring 1 calculations as it's the center point (already handled or not used for ring logic)
            // Effectively, the first *outer* ring is ring #2.
            if (ring == 1) 
            {
                ring++; 
                continue; 
            }

            // Calculate properties for the current ring
            float radius = 500f * (ring - 1); // Radius increases with ring number
            int numPositionsThisRing = 8 * (ring - 1); // Number of positions in this ring (e.g., 8, 16, 24...)
            
            // This check was likely for an older way of calculating numPositionsThisRing.
            // With `8 * (ring - 1)`, and `ring` starting effectively at 2 for this block,
            // `numPositionsThisRing` will be 8, 16, etc., never 0.
            // if (numPositionsThisRing == 0 && ring > 1) { 
            //      numPositionsThisRing = 8; // Fallback for the first effective ring if calculation was different
            // }

            float angleStep = 360f / numPositionsThisRing; // Angle between positions in the ring

            // Place entities in the current ring
            for (int pos = 0; pos < numPositionsThisRing && index < queue.Count; pos++)
            {
                float currentAngleRad = pos * angleStep * Mathf.Deg2Rad; // Angle in radians
                // Calculate offset from center
                Vector3 offset = new Vector3(
                    Mathf.Cos(currentAngleRad),
                    0, // Assuming Y is up, entities are on XZ plane
                    Mathf.Sin(currentAngleRad)
                ) * radius;

                // Rotate offset to align with the formation's overall heading
                offset = Quaternion.Euler(0, heading, 0) * offset;

                // Create the entity
                EntityMgr.inst.CreateEntity(queue[index], center + offset,
                    new Vector3(0, heading, 0), player);
                index++;
            }
            ring++; // Move to the next ring
        }
    }

    /// <summary>
    /// Defines the priority order for spawning entity types.
    /// Entities higher in the list are generally considered more important or spawned first/centrally.
    /// </summary>
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

    /// <summary>
    /// Context menu item in the Unity Editor to reload the current scenario.
    /// </summary>
    [ContextMenu("Reload Scene")] 
    public void ReloadScene()
    {
        reloadCount++;
        
        ClearAllEntities(); // Remove existing entities and weapons
        ResetGameState();   // Reset various game systems and states
        
        OpenOcean1x1();     // Re-initialize and start the 1v1 scenario
    }

    /// <summary>
    /// Clears all spawned entities and weapons from the scene.
    /// </summary>
    private void ClearAllEntities()
    {
        // Clear weapons
        if (WeaponsMgr.inst != null)
        {
            WeaponsMgr.inst.StopAllWeapons(); // Stop active weapon behaviors
            var weaponsCopy = new List<Entity>(WeaponsMgr.inst.weapons); // Iterate over a copy
            foreach (Entity weapon in weaponsCopy)
            {
                if (weapon != null && weapon.gameObject != null)
                {
                    // Stop AI commands if applicable
                    if (weapon.TryGetComponent<UnitAI>(out var unitAI))
                    {
                        unitAI.StopAndRemoveAllCommands();
                    }
                    // Destroy the weapon game object
                    if (weapon.gameObject != null) // Double check before destroy
                    {
                        DestroyImmediate(weapon.gameObject); // Use DestroyImmediate for editor/synchronous cleanup
                    }
                }
            }
            WeaponsMgr.inst.weapons.Clear(); // Clear the list
        }

        // Clear entities
        if (EntityMgr.inst != null)
        {
            var entitiesCopy = new List<Entity>(EntityMgr.inst.entities); // Iterate over a copy
            foreach (Entity entity in entitiesCopy)
            {
                if (entity != null && entity.gameObject != null)
                {
                    // Stop AI commands if applicable
                    if (entity.TryGetComponent<UnitAI>(out var unitAI))
                    {
                        unitAI.StopAndRemoveAllCommands();
                    }
                    // Destroy the entity game object
                     if (entity.gameObject != null) // Double check before destroy
                    {
                        DestroyImmediate(entity.gameObject); // Use DestroyImmediate
                    }
                }
            }
            EntityMgr.inst.entities.Clear(); // Clear the list
        }

        // Clear selections
        if (SelectionMgr.inst != null)
        {
            SelectionMgr.inst.selectedEntities.Clear();
            SelectionMgr.inst.selectedEntity = null;
        }

        // Force garbage collection and unload unused assets (can cause a hitch)
        System.GC.Collect();
        Resources.UnloadUnusedAssets();
        Physics.SyncTransforms(); // Ensure physics system is updated after transform changes
    }

    /// <summary>
    /// Resets various game state aspects and managers to their initial conditions.
    /// </summary>
    private void ResetGameState()
    {
        // Reset time scale
        Time.timeScale = 1f;
        if (simSpeedButtonText != null)
            simSpeedButtonText.text = "1";

        // Reset legacy spawning parameters (if they were modified)
        position = Vector3.zero; 
        initZ = 0;

        // Random.InitState is handled at the beginning of InitializeScenario(),
        // which is called by OpenOcean1x1() during reload. This ensures the
        // correct seed is used for the new scenario setup.

        BuildEntityDictionary(); // Rebuild based on initial `entityQuantities` (or adjusted ones if logic changes)

        // Reset various managers
        if (AIMgr.inst != null) AIMgr.inst.StopAllCoroutines(); // Stop AI-related coroutines
        if (DistanceMgr.inst != null) DistanceMgr.inst.Initialize(); // Re-init distance calculations
        if (FogWarMgr.inst != null) FogWarMgr.inst.ResetFog(); // Reset fog of war
        if (CameraMgr.inst != null) CameraMgr.inst.ResetCamera(); // Reset camera to default
        if (ScoreMgr.inst != null) ScoreMgr.inst.ResetScores(); // Reset player scores
        if (OpenOceanMain.inst != null) OpenOceanMain.inst.ResetGameState(); // Reset main training state logic
        if (MinimapMgr.inst != null) MinimapMgr.inst.ResetMinimap(); // Reset minimap
        if (LineMgr.inst != null) LineMgr.inst.DestroyAllLines(); // Clear drawn lines
        if (WeaponsMgr.inst != null) WeaponsMgr.inst.DestroyAllWeaponsImmediately(); // Ensure all weapons are gone
        if (FXMgr.inst != null) FXMgr.inst.ResetEffects(); // Reset visual effects
    }
}

