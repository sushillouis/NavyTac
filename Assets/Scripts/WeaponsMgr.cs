using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.XR;

public class WeaponsMgr : MonoBehaviour
{
    public static WeaponsMgr inst;
    private void Awake()
    {
        inst = this;
    }

    public List<GameObject> WeaponPrefabs = new List<GameObject>();
    public List<Entity> weapons = new List<Entity>();


    public void LaunchWeapon(Entity launchingEntity, WeaponData wd , Entity target, Vector3 targetPosition)
    {
        if(wd == null) Debug.Log("Could not find weapon: " + wd.weaponEntityType);
        Debug.Log("last sho time: "+ wd.lastShotTime + " cooldown: " + wd.cooldown + " ammo count: " + wd.ammoCount + "current time: " + Time.time);
        if (Time.time - wd.lastShotTime >= wd.cooldown)
        {
            if (wd.ammoCount <= 0)
            {
                Debug.Log("Failure! Out of " + wd.weaponEntityType + " on " + launchingEntity.name);
            }
            else
            {
                wd.ammoCount -= 1;
                Vector3 pos = launchingEntity.transform.TransformPoint(wd.launchLocation);
                Vector3 rot = launchingEntity.transform.TransformDirection(wd.launchDirection);
                Entity ent = EntityMgr.inst.CreateEntity(wd.weaponEntityType, pos, rot, launchingEntity.owner);
                weapons.Add(ent);
                wd.currentWeaponEntities.Add(ent);
                ent.creatorsEntity = launchingEntity;
                StartCoroutine(TargetEntity(ent, wd, target, targetPosition));
                wd.lastShotTime = Time.time;
            }
        }
    }

    IEnumerator TargetEntity(Entity weapon, WeaponData wd, Entity targetEntity, Vector3 targetPosition)
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
                AIMgr.inst.HandleDumbMove(entities, targetPosition , false);
                break;
            case WeaponBehaviors.Smart:
                AIMgr.inst.HandleSmartIntercept(entities, targetEntity, false);
                break;
            default:
                AIMgr.inst.HandleFollow(entities, targetEntity, Vector3.zero, false);
                break;
        }
    }

   
    public void WeaponDone(Entity weapon)
    {
        Entity ent = weapons.Find(x => x.name.Contains(weapon.name));
        if(ent != null)
        {
            weapons.Remove(ent);
        }
    }


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void handleWeapon(Vector2 mousePos, WeaponBehaviors behaviorType){
         List<Entity> selectedEntities = SelectionMgr.inst.selectedEntities;
        if (selectedEntities == null) return;
        foreach (Entity selectedEnt in selectedEntities)
        {
            WeaponsAspect weaponsAspect = selectedEnt.GetComponentInChildren<WeaponsAspect>();
            if (weaponsAspect == null) continue;
            WeaponData wd = weaponsAspect.weapons.Find(x => x.behaviorType == behaviorType);
            if (wd != null)
            {
                if (UIMgr.inst.getTargetPosition(mousePos, out Vector3 targetPosition))
                {
                    if (behaviorType == WeaponBehaviors.Dumb){
                        LaunchWeapon(selectedEnt, wd, null, targetPosition);
                        continue;
                    }
                    Entity targetEntity = UIMgr.inst.GetTargetEntity(targetPosition);
                    if (targetEntity != null && targetEntity.owner != selectedEnt.owner)
                    {
                        Debug.Log("Smart selected: " + selectedEnt.name + " with weapon: " + wd.weaponEntityType + " at " + targetEntity.name);
                        LaunchWeapon(selectedEnt, wd, targetEntity, targetPosition);
                    }
                    
                }
            }
        }

    }
    public void DestroyEntity(Entity entity)
{
    // Toggle RTS view if necessary
    if (!CameraMgr.inst.isRTSMode && CameraMgr.inst.YawNode.transform.parent.parent.name == entity.name)
    {
        CameraMgr.inst.ToggleRTSView();
    }

    // Stop and remove all commands from UnitAI, if present
    UnitAI unitAI = entity.GetComponentInChildren<UnitAI>();
    if (unitAI != null)
    {
        unitAI.StopAndRemoveAllCommands();
    }

    // Remove entity from the selection if it is currently selected
    if (SelectionMgr.inst.selectedEntities.Contains(entity))
    {
        SelectionMgr.inst.selectedEntities.Remove(entity);
        SelectionMgr.inst.selectedEntity = SelectionMgr.inst.selectedEntities.Count > 0
            ? SelectionMgr.inst.selectedEntities[0]
            : null;
    }

    // Remove the entity from EntityMgr and reinitialize DistanceMgr
    EntityMgr.inst.entities.Remove(entity);
    DistanceMgr.inst.Initialize();

    // Destroy the entity's GameObject
    Destroy(entity.gameObject);
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
