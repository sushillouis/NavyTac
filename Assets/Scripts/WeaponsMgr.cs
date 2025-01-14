using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponsMgr : MonoBehaviour
{
    public static WeaponsMgr inst;
    private void Awake()
    {
        inst = this;
    }

    public List<GameObject> WeaponPrefabs = new List<GameObject>();
    public List<Entity> weapons = new List<Entity>();
    [Header("DebugIsh")]
    [SerializeReference] GameObject judeMissilePrefab;
    [SerializeReference] GameObject judeAAMissilePrefab;
    public GameObject LaunchCruseMissile(Vector3 position, Entity targetEntity, Vector3 angle) {
        GameObject temp = Instantiate(judeMissilePrefab,position,Quaternion.identity,this.transform);
        temp.GetComponent<JudeMissile>().target = targetEntity;
        temp.transform.Rotate(angle);
        return temp;
    }

    public GameObject LaunchAAMissile(Vector3 position, PlaneEntity targetEntity) {
        GameObject temp = Instantiate(judeAAMissilePrefab,position,Quaternion.identity,this.transform);
        temp.GetComponent<JudeAAMissile>().target = targetEntity;
        return temp;
    }

    public void LaunchWeapon(Entity launchingEntity, EntityType weaponEntityType, Entity target)
    {
        WeaponsAspect weaponsAspect = launchingEntity.GetComponentInChildren<WeaponsAspect>();
        WeaponData wd = weaponsAspect.weapons.Find(x => x.weaponEntityType == weaponEntityType);
        if(wd == null) Debug.Log("Could not find weapon: " + weaponEntityType);
        if(wd.ammoCount <= 0)
        {
            Debug.Log("Failure! Out of " + weaponEntityType + " on " + launchingEntity.name);
        } else
        {
            wd.ammoCount -= 1;
            Vector3 pos = launchingEntity.transform.TransformPoint(wd.launchLocation);
            Vector3 rot = launchingEntity.transform.TransformDirection(wd.launchDirection);
            Entity ent = EntityMgr.inst.CreateEntity(weaponEntityType, pos, rot);
            weapons.Add(ent);
            Debug.Log("Weapon launching: " + ent.name);
            wd.currentWeaponEntities.Add(ent);

            StartCoroutine(TargetEntity(ent, wd, target));
        }
    }

    IEnumerator TargetEntity(Entity weapon, WeaponData wd, Entity targetEntity)
    {
        yield return new WaitForFixedUpdate();
        List<Entity> entities = new List<Entity>();
        entities.Add(weapon);
        switch(wd.behaviorType)
        {
            case WeaponBehaviors.SurfaceInterceptor:
                AIMgr.inst.HandleIntercept(entities, targetEntity, false);
                break;
            case WeaponBehaviors.AirInterceptor:
                AIMgr.inst.Handle3dIntercept(entities, targetEntity, false);
                break;
            case WeaponBehaviors.Dumb:
                AIMgr.inst.HandleFollow(entities, targetEntity, Vector3.zero, false);
                break;
            default:
                AIMgr.inst.HandleFollow(entities, targetEntity, Vector3.zero, false);
                break;
        }
    }

    public void WeaponDone(Entity weapon)
    {
        int index = weapons.IndexOf(weapon);
        // Entity ent = weapons.Find(x => x.name.Contains(weapon.name));
        if(index == -1)
        {
            weapons.RemoveAt(index);
        }
    }


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyUp(KeyCode.Alpha0))
            Test1();
    }



    void Test1()
    {
        Entity selectedEnt = SelectionMgr.inst.selectedEntity;
        Entity ent = 
            EntityMgr.inst.entities.Find(x => (x.entityType == EntityType.CVN75) && (x.owner.playerSide != selectedEnt.owner.playerSide));
        LaunchWeapon(selectedEnt, EntityType.AntiShipMissile, ent);
        Debug.Log("Launched: " + EntityType.AntiShipMissile + " @ " + ent.name);

        LaunchWeapon(selectedEnt, EntityType.SeaBaby, ent);
        Debug.Log("Launched: " + EntityType.SeaBaby + " @ " + ent.name);
    }

    public GameObject MovableEntitiesRoot;
    public GameObject WeaponsAspectPrefab;

    [ContextMenu("AddWeaponsAspectToAllEntities")]
    public void AddWeaponsAspectToAllEntities()
    {
        foreach(UIAspect uiAspect in MovableEntitiesRoot.transform.GetComponentsInChildren<UIAspect>(true))
        {
            GameObject aspectRoot = uiAspect.transform.parent.gameObject;
            if(aspectRoot != null)
            {
                if(!aspectRoot.transform.parent.name.Contains("DDG"))
                {
                    //GameObject go = Instantiate(WeaponsAspectPrefab, WeaponsAspectPrefab.transform);
                    Debug.Log("Added weapons aspect to " + aspectRoot.transform.parent.name);
                }
            }
        }

    }
}
