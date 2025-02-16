using System.Collections;
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

public class GameMgr : MonoBehaviour
{
    public static GameMgr inst;

    private void Awake()
    {
        inst = this;
        BuildEntityDictionary();
    }

    // UI elements for time scaling and simulation speed.
    [SerializeField] private Button plusButton;
    [SerializeField] private Button minusButton;
    [SerializeField] private TextMeshProUGUI simSpeedButtonText;
    public float timeScale = 1;

    // These fields are used for the Create100 method (spawning a grid of PilotVessels).
    public Vector3 position;
    public float spread = 20;
    public float colNum = 10;
    public float initZ;

    [SerializeField] private List<EntityQuantity> entityQuantities = new List<EntityQuantity>();
    private Dictionary<EntityType, int> entityDict;
    [Range(1, 4)]
    [SerializeField] public int players;
    [SerializeField] public bool sameEnityForAll;
    
   
    void Start()
    {
        // Disable movable entities container if applicable.
        EntityMgr.inst.movableEntitiesRoot.SetActive(false);

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
            // ent.isSelected = true;
        }
        AIMgr.inst.HandleMove(allEntities, movePos, shouldAdd);
        // AIMgr.inst.HandleMove(allEntities, new Vector3(3000, 0, 0), true);
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

    public void OpenOcean1x1()
    {
        Vector3 posPlayer1 = new Vector3(0100, 0, -7000);
        Vector3 posPlayer2 = new Vector3(0, 0, 1 * Utils.FromNauticalMiles);
        // MakeEntsForPlayer(posPlayer1, 0, PlayerMgr.inst.player1);
        // MakeEntsForPlayer(posPlayer2, 180, PlayerMgr.inst.player2);
        SpawnEntitiesFromDictionary(posPlayer1, 0, PlayerMgr.inst.player1);
        SpawnEntitiesFromDictionary(posPlayer2, 180, PlayerMgr.inst.player2);
    }


    public void MakeEntsForPlayer(Vector3 initPos, float initHeading, TactPlayer player)
    {
        Vector3 eulerAngles = new Vector3(0, initHeading, 0);
        Entity initEnt = EntityMgr.inst.CreateEntity(EntityType.DDG51, initPos, eulerAngles, player);
        Entity tmpEnt;

        // Example for additional spawns (escort, USVs, etc.). Uncomment and modify as needed.
        
        // Escort on right
        Vector3 offset = initEnt.transform.right * 1000;
        tmpEnt = EntityMgr.inst.CreateEntity(EntityType.DDG51, initPos + offset, eulerAngles, player);

        // USV on right
        offset = tmpEnt.transform.right * 500;
        tmpEnt = EntityMgr.inst.CreateEntity(EntityType.SeaHunter, initPos + offset, eulerAngles, player);

        // USV in front
        offset = initEnt.transform.forward * 1000;
        tmpEnt = EntityMgr.inst.CreateEntity(EntityType.SeaHunter, initPos + offset, eulerAngles, player);

        // USV behind
        offset = -initEnt.transform.forward * 1000;
        tmpEnt = EntityMgr.inst.CreateEntity(EntityType.SeaHunter, initPos + offset, eulerAngles, player);

        // Escort on left
        offset = -initEnt.transform.right * 1000;
        tmpEnt = EntityMgr.inst.CreateEntity(EntityType.DDG51, initPos + offset, eulerAngles, player);
        
    }

    private void BuildEntityDictionary()
    {
        entityDict = new Dictionary<EntityType, int>();
        foreach (EntityQuantity eq in entityQuantities)
        {
            if (entityDict.ContainsKey(eq.entityType))
                entityDict[eq.entityType] += eq.unitCount;
            else
                entityDict.Add(eq.entityType, eq.unitCount);
        }
    }

public List<EntityType> priorityList = new List<EntityType>()
    {
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
public void SpawnEntitiesFromDictionary(Vector3 initPos, float initHeading, TactPlayer player)
{
    BuildEntityDictionary();  
    

    List<EntityType> spawnQueue = new List<EntityType>();

    // Use the prioritized order to fill the spawn queue.
    foreach (EntityType type in priorityList)
    {
        if (entityDict.TryGetValue(type, out int count))
        {
            for (int j = 0; j < count; j++)
            {
                spawnQueue.Add(type);
            }
        }
    }

    int index = 0;
    int ring = 1;
    if (spawnQueue.Count > 0)
    {
        Vector3 spawnPos = initPos;  
        Vector3 eulerAngles = new Vector3(0, initHeading, 0);
        EntityMgr.inst.CreateEntity(spawnQueue[index], spawnPos, eulerAngles, player);
        // Debug.Log($"Spawning {spawnQueue[index]} at {spawnPos} (ring {ring})");
        index++;
    }

    ring = 2;  
    while (index < spawnQueue.Count)
    {
      
        int entitiesThisRing = 8 * (ring - 1);

        float angleStep = 360f / entitiesThisRing;

        for (int pos = 0; pos < entitiesThisRing && index < spawnQueue.Count; pos++)
        {
            float angle = pos * angleStep;
            float rad = angle * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(rad), 0, Mathf.Sin(rad));

            Vector3 offset = direction * 250f * (ring - 1);
            Vector3 spawnPos = initPos + offset;
            Vector3 eulerAngles = new Vector3(0, initHeading, 0);
            
            EntityMgr.inst.CreateEntity(spawnQueue[index], spawnPos, eulerAngles, player);
            // Debug.Log($"Spawning {spawnQueue[index]} at {spawnPos} (ring {ring}, angle {angle})");
            index++;
        }
        ring++;
    }
}
}