using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameMgr : MonoBehaviour
{
    public static GameMgr inst;
    public bool deployUnits = true;

    private void Awake()
    {
        inst = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        Vector3 position = Vector3.zero;
        if(deployUnits) {
            foreach(GameObject go in EntityMgr.inst.entityPrefabs) {
                Entity ent = EntityMgr.inst.CreateEntity(go.GetComponent<Entity>().entityType, position, Vector3.zero);
                ent.isSelected = false;
                position.x += 200;
            }
        }

        EntityMgr.inst.movableEntitiesRoot.SetActive(false);
    }

    public Vector3 position;
    public float spread = 20;
    public float colNum = 10;
    public float initZ;
    // Update is called once per frame
    void Update()
    {
        
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



    public void InitMapMenu() {
        List<Entity> allEntities = new List<Entity>();
        Vector3 position = Vector3.zero;
        Vector3 offset = new Vector3(100, 0, -50);

        foreach(GameObject go in EntityMgr.inst.entityPrefabs) {
            Entity ent = go.GetComponent<Entity>();
            ent = EntityMgr.inst.CreateEntity(ent.entityType, position + offset, new Vector3(0, 270, 0));
            allEntities.Add(ent);
            position.x += 400;
        }
        Vector3 pos = new Vector3(-3000, 0, 0);
        bool add = false;
        StartCoroutine(AddMoveCommandsToEnt(allEntities, pos, add, 270));
        pos.x = 3000;
        add = true;
        StartCoroutine(AddMoveCommandsToEnt(allEntities, pos, add));
    }


    IEnumerator AddMoveCommandsToEnt(List<Entity> allEntities, Vector3 movePos, bool shouldAdd, float heading = -1) {
        yield return new WaitForSeconds(0.1f);
        foreach(Entity ent in allEntities) {
            ent.heading = (heading == -1 ? ent.heading : heading);
            //ent.isSelected = true;
        }
        AIMgr.inst.HandleMove(allEntities, movePos, shouldAdd);
        //AIMgr.inst.HandleMove(allEntities, new Vector3(3000, 0, 0), true);

    }

    public void InitOpenOceanMap() {

        List<Entity> allEntities = CreateCarrierGroup(Vector3.zero, 0, 300, 300);
        Vector3 pos = new Vector3(0, 0, 4000);
        bool add = false;
        StartCoroutine(AddMoveCommandsToEnt(allEntities, pos, add, 0));

        
        List<Entity> allEntities2 = CreateCarrierGroup(new Vector3(0, 0, 3000), 180, 300, 300);
        pos.z = -1000;
        StartCoroutine(AddMoveCommandsToEnt(allEntities2, pos, add, 180));
    }


    List<Entity> CreateCarrierGroup(Vector3 position, float heading, float xDelta, float zDelta) {
        List<Entity> allEntities = new List<Entity>();

        Vector3 pos = new Vector3(position.x, position.y, position.z);
        Vector3 headingVector = new Vector3(0, heading, 0);

        Entity ent = EntityMgr.inst.CreateEntity(EntityType.CVN75, pos, headingVector);
        allEntities.Add(ent);

        pos.x = position.x - xDelta;
        pos.z = position.z + zDelta;
        ent = EntityMgr.inst.CreateEntity(EntityType.DDG51, pos, headingVector);
        allEntities.Add(ent);

        pos.x = position.x + xDelta;
        pos.z = position.z + zDelta;
        ent = EntityMgr.inst.CreateEntity(EntityType.DDG51, pos, headingVector);
        allEntities.Add(ent);

        pos.x = position.x - xDelta / 2;
        pos.z = position.z + zDelta;
        ent = EntityMgr.inst.CreateEntity(EntityType.SeaHunter, pos, headingVector);
        allEntities.Add(ent);

        pos.x = position.x + xDelta / 2;
        pos.z = position.z + zDelta;
        ent = EntityMgr.inst.CreateEntity(EntityType.SeaHunter, pos, headingVector);
        allEntities.Add(ent);

        pos.x = position.x - xDelta / 2;
        pos.z = position.z + zDelta / 2;
        ent = EntityMgr.inst.CreateEntity(EntityType.SeaHunter, pos, headingVector);
        allEntities.Add(ent);

        pos.x = position.x + xDelta / 2;
        pos.z = position.z + zDelta / 2;
        ent = EntityMgr.inst.CreateEntity(EntityType.SeaHunter, pos, headingVector);
        allEntities.Add(ent);

        return allEntities;
    }

}
