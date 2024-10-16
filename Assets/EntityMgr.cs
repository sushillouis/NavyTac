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
        //foreach(Entity ent in movableEntitiesRoot.GetComponentsInChildren<Entity>()) {
        //    entities.Add(ent);
        //}
    }

    public GameObject movableEntitiesRoot;
    public List<GameObject> entityPrefabs;
    public GameObject blastPrefab;
    public GameObject entitiesRoot;
    public List<Entity> entities;

    public static int entityId = 0;

    public Entity CreateEntity(EntityType et, Vector3 position, Vector3 eulerAngles)
    {
        Entity entity = null;
        GameObject entityPrefab = entityPrefabs.Find(x => (x.GetComponent<Entity>().entityType == et));
        if (entityPrefab != null) {
            GameObject entityGo = Instantiate(entityPrefab, position, Quaternion.Euler(eulerAngles), entitiesRoot.transform);
            if (entityGo != null) {
                entity = entityGo.GetComponent<Entity>();
                entityGo.name = et.ToString() + entityId++;
                entity.health = 100f;
                entities.Add(entity);
            }
        }
        return entity;
    }


    public void RemoveEntity(Entity entity)
    {
        if (entities.Contains(entity))
        {
            GameObject blastInstance = Instantiate(blastPrefab, entity.transform.position, entity.transform.rotation);
            blastInstance.transform.localScale = new Vector3(entity.transform.localScale.x * 100f, entity.transform.localScale.y * 100f, entity.transform.localScale.z * 100f);
            bool rtsMode = false;
            CameraMgr.inst.SwitchRTSMode(rtsMode);
            UnitAI cmd = entity.GetComponent<UnitAI>();
            if (cmd != null)
            {
                cmd.StopAndRemoveAllCommands();
            }
            SelectionMgr.inst.DeselectEntity(entity);
            entities.Remove(entity);
            Destroy(entity.gameObject);
            StartCoroutine(DestroyBlastAfterDelay(blastInstance, .5f));
        }
    }
    private IEnumerator DestroyBlastAfterDelay(GameObject blast, float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(blast);
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
