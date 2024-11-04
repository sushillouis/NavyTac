using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameMgr : MonoBehaviour
{
    public static GameMgr inst;
    private void Awake()
    {
        inst = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (SceneManager.GetActiveScene().name.Equals("OpenOceanMap"))
            InitOpenOceanMap();
        //else
        //    InitOpenOcean();
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

    void InitOpenOcean()
    {
        Vector3 position = Vector3.zero;
        foreach (GameObject go in EntityMgr.inst.entityPrefabs)
        {
            Entity ent = EntityMgr.inst.CreateEntity(go.GetComponent<Entity>().entityType, position, Vector3.zero);
            ent.isSelected = false;
            position.x += 200;
        }

        DistanceMgr.inst.Initialize();
    }

    List<Entity> allEntities = new List<Entity>();
    public void InitMapMenu() {
        Vector3 position = Vector3.zero;
        Vector3 offset = new Vector3(100, 0, -50);

        foreach(GameObject go in EntityMgr.inst.entityPrefabs) {
            Entity ent = go.GetComponent<Entity>();
            ent = EntityMgr.inst.CreateEntity(ent.entityType, position + offset, new Vector3(0, 270, 0));
            allEntities.Add(ent);
            position.x += 400;
        }
        StartCoroutine(AddMoveCommandsToEnt());

    }

    IEnumerator AddMoveCommandsToEnt() {
        yield return new WaitForSeconds(0.5f);
        foreach(Entity ent in allEntities) {
            ent.heading = 270;
            ent.isSelected = true;
        }
        AIMgr.inst.HandleMove(allEntities, new Vector3(-3000, 0, 0), false);
        AIMgr.inst.HandleMove(allEntities, new Vector3(3000, 0, 0), true);

    }

    public List<Vector3> OpenOceanMapSpawns;
    public List<float> OpenOceanMapRotations;
    void InitOpenOceanMap()
    {
        float outerRadius = 500;
        float vertPosAngle = 0;
        float innerRadius = 100;
        float rotation = 0;

        for (int i = 0; i < OpenOceanMapRotations.Count; i++)
        {
            rotation = OpenOceanMapRotations[i];
            Vector3 eulers = new Vector3(0, rotation, 0);
            Vector3 pos1 = CalculateVertex(OpenOceanMapSpawns[i], outerRadius, 30 + vertPosAngle);
            Vector3 pos2 = CalculateVertex(OpenOceanMapSpawns[i], outerRadius, 150 + vertPosAngle);
            Vector3 pos3 = CalculateVertex(OpenOceanMapSpawns[i], outerRadius, 270 + vertPosAngle);
            Vector3 pos4 = CalculateVertex(OpenOceanMapSpawns[i], innerRadius, 0);
            Vector3 pos5 = CalculateVertex(OpenOceanMapSpawns[i], innerRadius, 90);
            Vector3 pos6 = CalculateVertex(OpenOceanMapSpawns[i], innerRadius, 180);
            Vector3 pos7 = CalculateVertex(OpenOceanMapSpawns[i], innerRadius, 270);

            List<Entity> newEnts = new List<Entity>();
            newEnts.Add(EntityMgr.inst.CreateEntity(EntityType.DDG51, pos1, eulers));
            newEnts.Add(EntityMgr.inst.CreateEntity(EntityType.DDG51, pos2, eulers));
            newEnts.Add(EntityMgr.inst.CreateEntity(EntityType.CVN75, pos3, eulers));
            newEnts.Add(EntityMgr.inst.CreateEntity(EntityType.JARIUSV, pos4, eulers));
            newEnts.Add(EntityMgr.inst.CreateEntity(EntityType.JARIUSV, pos5, eulers));
            newEnts.Add(EntityMgr.inst.CreateEntity(EntityType.JARIUSV, pos6, eulers));
            newEnts.Add(EntityMgr.inst.CreateEntity(EntityType.JARIUSV, pos7, eulers));

            foreach(Entity ent in newEnts)
            {
                ent.desiredHeading = rotation;
                ent.heading = rotation;
            }

            rotation -= 90;
            vertPosAngle += 90; 
        }

        DistanceMgr.inst.Initialize();
    }

    Vector3 CalculateVertex(Vector3 center, float radius, float angle)
    {
        float angleInRadians = Mathf.Deg2Rad*angle;
        float x = center.x + radius * Mathf.Cos(angleInRadians);
        float z = center.z + radius * Mathf.Sin(angleInRadians);
        return new Vector3(x, center.y, z);

    }



}
