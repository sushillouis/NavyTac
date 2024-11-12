using System.Collections;
using System.Collections.Generic;
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
        if(wd.ammoCount <= 0)
        {
            Debug.Log("Failure! Out of " + wd.weaponEntityType + " on " + launchingEntity.name);
        } 
        else
        {
            wd.ammoCount -= 1;
            Vector3 pos = launchingEntity.transform.TransformPoint(wd.launchLocation);
            Vector3 rot = launchingEntity.transform.TransformDirection(wd.launchDirection);
            Entity ent = EntityMgr.inst.CreateEntity(wd.weaponEntityType, pos, rot);
            weapons.Add(ent);
            wd.currentWeaponEntities.Add(ent);
            StartCoroutine(TargetEntity(ent, wd, target, targetPosition) );
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

    public void HandleSmartWeapon(Vector2 mousePos)
    {
        List<Entity> selectedEntities = SelectionMgr.inst.selectedEntities;
        if(selectedEntities == null) return;
        foreach(Entity selectedEnt in selectedEntities)
        {
            WeaponsAspect weaponsAspect = selectedEnt.GetComponentInChildren<WeaponsAspect>();
            if(weaponsAspect == null) continue;
            WeaponData wd = weaponsAspect.weapons.Find(x => x.behaviorType == WeaponBehaviors.Smart);
            if (wd != null) {
                if(UIMgr.inst.getTargetPosition(mousePos, out Vector3 targetPosition)){
                    Entity targetEntity = UIMgr.inst.GetTargetEntity(targetPosition);
                    if(targetEntity!= null && targetEntity.owner != selectedEnt.owner){
                        Debug.Log("Smart selected: " + selectedEnt.name + " with weapon: " + wd.weaponEntityType + " at " + targetEntity.name) ;
                        LaunchWeapon(selectedEnt, wd, targetEntity,targetPosition);
                    }
                }
            }
        }
    }

    public void HandleDumbWeapon(Vector2 mousePos){
        List<Entity> selectedEntities = SelectionMgr.inst.selectedEntities;
       if(selectedEntities == null) return;
       foreach(Entity selectedEnt in selectedEntities){
            WeaponsAspect weaponsAspect = selectedEnt.GetComponentInChildren<WeaponsAspect>();
            
            if(weaponsAspect == null) continue;
            WeaponData wd = weaponsAspect.weapons.Find(x => x.behaviorType == WeaponBehaviors.Dumb);
            if (wd != null) {
                Debug.Log("Smart selected: " + selectedEnt.name + " with weapon: " + wd.weaponEntityType);
                if(UIMgr.inst.getTargetPosition(mousePos, out Vector3 targetPosition)){
                    
                    LaunchWeapon(selectedEnt, wd, null,targetPosition);
                }
                
            }
       }
    }

    public void HandleAirInterceptorWeapon(Vector2 mousePos){
        List<Entity> selectedEntities = SelectionMgr.inst.selectedEntities;
       if(selectedEntities == null) return;
       foreach(Entity selectedEnt in selectedEntities){
            WeaponsAspect weaponsAspect = selectedEnt.GetComponentInChildren<WeaponsAspect>();
            if(weaponsAspect == null) continue;
            WeaponData wd = weaponsAspect.weapons.Find(x => x.behaviorType == WeaponBehaviors.AirInterceptor);
            if (wd != null) {
                if(UIMgr.inst.getTargetPosition(mousePos, out Vector3 targetPosition)){
                    Entity targetEntity = UIMgr.inst.GetTargetEntity(targetPosition);
                    if(targetEntity!= null && targetEntity.owner != selectedEnt.owner){
                        Debug.Log("Smart selected: " + selectedEnt.name + " with weapon: " + wd.weaponEntityType + " at " + targetEntity.name) ;
                        LaunchWeapon(selectedEnt, wd, targetEntity,targetPosition);
                        
                    }
                }
            }
       }
    }
    public void HandleSurfaceInterceptorWeapon(Vector2 mousePos){
       List<Entity> selectedEntities = SelectionMgr.inst.selectedEntities;
       if(selectedEntities == null) return;
       foreach(Entity selectedEnt in selectedEntities){
            WeaponsAspect weaponsAspect = selectedEnt.GetComponentInChildren<WeaponsAspect>();
            if(weaponsAspect == null) continue;
            WeaponData wd = weaponsAspect.weapons.Find(x => x.behaviorType == WeaponBehaviors.SurfaceInterceptor);
            if (wd != null) {
                if(UIMgr.inst.getTargetPosition(mousePos, out Vector3 targetPosition)){
                    Entity targetEntity = UIMgr.inst.GetTargetEntity(targetPosition);
                    if(targetEntity!= null && targetEntity.owner != selectedEnt.owner){
                        Debug.Log("Smart selected: " + selectedEnt.name + " with weapon: " + wd.weaponEntityType + " at " + targetEntity.name) ;
                        LaunchWeapon(selectedEnt, wd, targetEntity,targetPosition);
                    }
                }
            }
       }
        

        
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
