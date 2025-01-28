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
    public List<GenericMissile> activeMissiles = new List<GenericMissile>();
    [Header("DebugIsh")]
    [SerializeReference] GameObject judeMissilePrefab;
    [SerializeReference] GameObject judeAAMissilePrefab;
    [SerializeReference] GameObject judeAntiMissilePrefab;
    public GameObject LaunchCruseMissile(Vector3 position, Entity targetEntity, Vector3 angle, TactPlayer team) {
        GameObject temp = Instantiate(judeMissilePrefab,position,Quaternion.identity,this.transform);
        JudeMissile missile = temp.GetComponent<JudeMissile>();
        missile.target = targetEntity;
        activeMissiles.Add(missile);
        missile.Init(team);
        temp.transform.Rotate(angle);
        return temp;
    }

    public GameObject LaunchAAMissile(Vector3 position, Entity targetEntity, Vector3 angle, TactPlayer team) {
        GameObject temp = Instantiate(judeAAMissilePrefab,position,Quaternion.identity,this.transform);
        JudeAAMissile missile = temp.GetComponent<JudeAAMissile>();
        missile.target = targetEntity;
        activeMissiles.Add(missile);
        missile.Init(team);
        temp.transform.Rotate(angle);
        return temp;
    }

    public GameObject LaunchAntiMissile(Vector3 position, GenericMissile targetMissile, Vector3 angle, TactPlayer team) {
        GameObject temp = Instantiate(judeAntiMissilePrefab,position,Quaternion.identity,this.transform);
        AntiMissileMissile missile = temp.GetComponent<AntiMissileMissile>();
        missile.target = targetMissile;
        // activeMissiles.Add(missile);
        missile.Init(team);
        temp.transform.Rotate(angle);
        return temp;
    }

    public void CalculateAndDealDamage(Entity target, WeaponType weapon,float baseDamage) {
        float scalar = Utils.weaponDamageScalar.TryGetValue((weapon,target.entityClass), out scalar) ? scalar: 1f;
        target.health-=baseDamage*scalar;
        if(target.health<=0f) {
            target.ai.Die();
        }
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
