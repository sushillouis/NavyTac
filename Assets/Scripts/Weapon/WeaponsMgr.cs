using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponsMgr : MonoBehaviour
{
    public static WeaponsMgr inst;
    public DamageMatrix damageMatrix;
    private Dictionary<EntityType, Queue<Entity>> weaponPools = new Dictionary<EntityType, Queue<Entity>>();


    private void Awake()
    {
        inst = this;
        damageMatrix = new DamageMatrix();
        damageMatrix.InitializeDamageMatrix(weaponDamages);
        weaponPools = new Dictionary<EntityType, Queue<Entity>>();
    }
   
    public List<WeaponDamage> weaponDamages;
    public List<Entity> weapons = new List<Entity>();

   public void handleWeapon(Vector2 mousePos, WeaponBehaviors behaviorType)
    {
        List<Entity> selectedEntities = SelectionMgr.inst.selectedEntities;
        if (selectedEntities == null || selectedEntities.Count == 0) return;

        Camera mainCamera = Camera.main;
        AIMgr aiManager = AIMgr.inst;

        foreach (Entity selectedEnt in selectedEntities)
        {
            WeaponsAspect weaponsAspect = selectedEnt.GetComponentInChildren<WeaponsAspect>();
            if (weaponsAspect == null) continue;

            WeaponData wd = weaponsAspect.weapons.Find(x => x.behaviorType == behaviorType);
            if (wd == null) continue;

            RaycastHit hit;
            if (!Physics.Raycast(mainCamera.ScreenPointToRay(mousePos), out hit, float.MaxValue, aiManager.layerMask)) continue;

            Entity targetEntity = UIMgr.inst.FindClosestEntInRadius(hit.point);

            if (behaviorType == WeaponBehaviors.Dumb)
            {
                LaunchWeapon(selectedEnt, wd, null, hit.point);
                continue;
            }

            if (targetEntity != null && targetEntity.owner != selectedEnt.owner)
            {
                // Debug.Log($"Smart selected: {selectedEnt.name} with weapon: {wd.weaponEntityType} at {targetEntity.name}");
                LaunchWeapon(selectedEnt, wd, targetEntity, hit.point);
            }
        }
    }

    public void handleWeapon(Entity entity, Entity targetEntity, WeaponBehaviors behaviorType)
    {
        if (entity == null) return;

        Camera mainCamera = Camera.main;
        AIMgr aiManager = AIMgr.inst;

        
            WeaponsAspect weaponsAspect = entity.GetComponentInChildren<WeaponsAspect>();
            if (weaponsAspect == null) return;
            WeaponData wd = weaponsAspect.weapons.Find(x => x.behaviorType == behaviorType);
            if (wd == null) return;

            if (targetEntity != null && targetEntity.owner != entity.owner)
            {
                Debug.Log($"Smart selected: {entity.name} with weapon: {wd.weaponEntityType} at {targetEntity.name}");
                LaunchWeapon(entity, wd, targetEntity, targetEntity.transform.position);
            }
        
    }
    public void handleWeapon(List<Entity> entities, Entity targetEntity , WeaponBehaviors behaviorType)
    {
        if (entities == null || entities.Count == 0) return;

        Camera mainCamera = Camera.main;
        AIMgr aiManager = AIMgr.inst;

        foreach (Entity selectedEnt in entities)
        {
            WeaponsAspect weaponsAspect = selectedEnt.GetComponentInChildren<WeaponsAspect>();
            if (weaponsAspect == null) continue;
            WeaponData wd = weaponsAspect.weapons.Find(x => x.behaviorType == behaviorType);
            if (wd == null) continue;
            if (targetEntity != null && targetEntity.owner != selectedEnt.owner)
            {
                Debug.Log($"Smart selected: {selectedEnt.name} with weapon: {wd.weaponEntityType} at {targetEntity.name}");
                LaunchWeapon(selectedEnt, wd, targetEntity, targetEntity.transform.position);
            }
        }
    }
    
    IEnumerator TargetEntity(Entity weapon, WeaponData wd, Entity targetEntity, Vector3 targetPosition)
    {
        yield return new WaitForFixedUpdate();
        List<Entity> entities = new List<Entity> { weapon };
        switch (wd.behaviorType)
        {
            case WeaponBehaviors.SurfaceInterceptor:
                Debug.Log("Surface Interceptor");
                AIMgr.inst.HandleIntercept(entities, targetEntity, false);
                break;
            case WeaponBehaviors.AirInterceptor:
                Debug.Log("Air Interceptor");
                AIMgr.inst.Handle3dIntercept(entities, targetEntity, false);
                break;
            case WeaponBehaviors.Dumb:
                Debug.Log("Dumb");
                AIMgr.inst.HandleDumbMove(entities, targetPosition, false);
                break;
            case WeaponBehaviors.Smart:
                Debug.Log("Smart");
                AIMgr.inst.HandleSmartIntercept(entities, targetEntity, false);
                break;
            default:
                AIMgr.inst.HandleFollow(entities, targetEntity, Vector3.zero, false);
                break;
        }
    }

    public void LaunchWeapon(Entity launchingEntity, WeaponData wd, Entity target, Vector3 targetPosition)
    {
        if (wd == null) 
        {
            Debug.Log("Could not find weapon: " + (wd != null ? wd.weaponEntityType : "NULL"));
            return;
        }

        // Check cooldown
        if (Time.time - wd.lastShotTime >= wd.cooldown)
        {
            // Check ammo
            if (wd.ammoCount <= 0)
            {
                Debug.Log("Failure! Out of " + wd.weaponEntityType + " on " + launchingEntity.name);
            }
            else
            {
                wd.ammoCount -= 1;
                Vector3 toTarget = target != null ? 
                (target.transform.position - launchingEntity.transform.position).normalized :
                (targetPosition - launchingEntity.transform.position).normalized;
                float dotProduct = Vector3.Dot(launchingEntity.transform.forward, toTarget);
                bool isForwardFacing = dotProduct > 0;
                Vector3 localDir = wd.launchDirection;
                Vector3 localPos = wd.launchLocation;
                if (!isForwardFacing )
                {
                    localPos.z *= -1;  // Mirror position along local Z-axis
                    localDir *= -1;    // Reverse direction
                }
                
                Vector3 pos = launchingEntity.transform.TransformPoint(localPos);
                Vector3 dir = launchingEntity.transform.TransformDirection(localDir).normalized;
                Entity ent = GetWeapon(wd.weaponEntityType, pos, dir, launchingEntity.owner);
                weapons.Add(ent);
                wd.currentWeaponEntities.Add(ent);
                ent.creatorsEntity = launchingEntity;
                StartCoroutine(TargetEntity(ent, wd, target, targetPosition));
                wd.lastShotTime = Time.time;
            }
        }
    }
    private Entity GetWeapon(EntityType weaponEntityType, Vector3 position, Vector3 direction, TactPlayer owner)
    {

        if (!weaponPools.ContainsKey(weaponEntityType))
        {
            weaponPools[weaponEntityType] = new Queue<Entity>();
        }
        Entity weaponEntity = null;
        if (weaponPools[weaponEntityType].Count > 0)
        {
            weaponEntity = weaponPools[weaponEntityType].Dequeue();
            weaponEntity.gameObject.SetActive(true); 
            weaponEntity.owner = owner;
        }
        else
        {
            weaponEntity = EntityMgr.inst.CreateEntity(weaponEntityType, position, direction, owner);
        }
        weaponEntity.transform.position = position;
 
        weaponEntity.entityType = weaponEntityType; 

        return weaponEntity;
    }
    
    private void ReturnWeapon(Entity weaponEntity)
        {
            
            if (!weaponPools.ContainsKey(weaponEntity.entityType))
            {
                weaponPools[weaponEntity.entityType] = new Queue<Entity>();
            }
            weaponEntity.gameObject.SetActive(false);
            weaponPools[weaponEntity.entityType].Enqueue(weaponEntity);
        }
    public void DestroyEntity(Entity entity)
    {
        MinimapMgr.inst.RemoveMinimapIcon(entity);
        if (!CameraMgr.inst.isRTSMode && CameraMgr.inst.YawNode.transform.parent.parent.name == entity.name)
        {
            CameraMgr.inst.ToggleRTSView();
        }
        if (weapons.Contains(entity))
        {
            EntityMgr.inst.entities.Remove(entity);
            ReturnWeapon(entity);
            weapons.Remove(entity);
            return;
        }
        

        UnitAI unitAI = entity.GetComponentInChildren<UnitAI>();
        if (unitAI != null)
        {
            unitAI.StopAndRemoveAllCommands();
        }

        if (SelectionMgr.inst.selectedEntities.Contains(entity))
        {
            SelectionMgr.inst.selectedEntities.Remove(entity);
            SelectionMgr.inst.selectedEntity = 
                (SelectionMgr.inst.selectedEntities.Count > 0) 
                ? SelectionMgr.inst.selectedEntities[0] 
                : null;
        }

        EntityMgr.inst.entities.Remove(entity);
        DistanceMgr.inst.Initialize();
        Destroy(entity.gameObject);
    }

    [Header("Context Menu")]
    public GameObject MovableEntitiesRoot;

    [ContextMenu("Add Weapons Aspect To All Entities")]
    public void AddWeaponsAspectToAllEntities()
    {
        foreach (UIAspect uiAspect in MovableEntitiesRoot.transform.GetComponentsInChildren<UIAspect>(true))
        {
            GameObject aspectRoot = uiAspect.transform.parent.gameObject;
            if (aspectRoot != null)
            {
                if (!aspectRoot.transform.parent.name.Contains("DDG"))
                {
                    Debug.Log("Added weapons aspect to " + aspectRoot.transform.parent.name);
                }
            }
        }
    }
    public string fileNameCSV = "WeaponDamageMatrix.csv";
    public TextAsset csvFile;
    [ContextMenu("Damage Matrix to CSV")]
    public void DamageMatrixToCSV()
    {
        WeaponCSVHandler.DamageMatrixToCSV(weaponDamages, fileNameCSV);
    }
    [ContextMenu("CSV To Damage Matrix")]
    public void CSVToDamageMatrix()
    {
        string filePath = Application.dataPath + "/" + csvFile.name + ".csv";
        weaponDamages = WeaponCSVHandler.CSVToDamageMatrix(filePath);
    }
}