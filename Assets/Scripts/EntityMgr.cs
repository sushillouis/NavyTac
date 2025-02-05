using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityMgr : MonoBehaviour
{
    public static EntityMgr inst;
    private void Awake()
    {
        inst = this;
        entities = new List<Entity>();
        entitiesDict = new Dictionary<int, Entity> ();
        //foreach(Entity ent in movableEntitiesRoot.GetComponentsInChildren<Entity>()) {
        //    entities.Add(ent);
        //}
    }

    public GameObject movableEntitiesRoot;
    public List<GameObject> entityPrefabs;
    public GameObject entitiesRoot;
    public List<Entity> entities;
    public Dictionary<int, Entity> entitiesDict;

    public int entityId = 0;

    public Entity CreateEntity(EntityType et, Vector3 position, Vector3 eulerAngles) {
        return CreateEntity(et, position, eulerAngles, PlayerMgr.inst.player1);
    }

    public Entity CreateEntity(EntityType et, Vector3 position, Vector3 eulerAngles, TactPlayer player) {
        Entity entity = null;
        GameObject entityPrefab = entityPrefabs.Find(x => (x.GetComponent<Entity>().entityType == et));
        if(entityPrefab != null) {
            GameObject entityGo = Instantiate(entityPrefab, position, Quaternion.Euler(eulerAngles), entitiesRoot.transform);
            entityGo.SetActive(true);
            if(entityGo != null) {
                entity = entityGo.GetComponent<Entity>();
                entity.entityId = entityId;
                entityGo.name = et.ToString() + entityId++;
                entity.owner = player;
                entity.heading = entity.desiredHeading = eulerAngles.y;
                entities.Add(entity);
                entitiesDict.Add(entity.entityId, entity);
            }
        }
        DistanceMgr.inst.Initialize();

        return entity;
    }

    public void DestroyEntity(Entity entity)
    {
            entity.GetComponentInChildren<OrientedPhysics>().enabled = false;
            entity.GetComponentInChildren<UnitAI>().StopAndRemoveAllCommands();
            Vector3 deadRot = transform.localEulerAngles;
            deadRot.z = 90;
            entity.speed = 0;
            if (!CameraMgr.inst.isRTSMode && CameraMgr.inst.YawNode.transform.parent.parent.name == entity.name)
            {
                CameraMgr.inst.ToggleRTSView();
            }
            entities.Remove(entity);
            
    }



    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


}
