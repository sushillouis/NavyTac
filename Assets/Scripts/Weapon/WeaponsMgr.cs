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

    private Entity GetWeapon(EntityType weaponEntityType, Vector3 position, Vector3 direction, TactPlayer owner)
    {
        if (!weaponPools.ContainsKey(weaponEntityType))
            weaponPools[weaponEntityType] = new Queue<Entity>();

        Entity weaponEntity;
        if (weaponPools[weaponEntityType].Count > 0)
        {
            weaponEntity = weaponPools[weaponEntityType].Dequeue();
            weaponEntity.gameObject.SetActive(true);
            
            // Reset physics state
            Rigidbody rb = weaponEntity.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // Reset AI state
            UnitAI unitAI = weaponEntity.GetComponent<UnitAI>();
            if (unitAI != null) unitAI.StopAndRemoveAllCommands();
        }
        else
        {
            weaponEntity = EntityMgr.inst.CreateEntity(weaponEntityType, position, direction, owner);
        }

        // Set fresh transform values
        weaponEntity.transform.position = position;
        weaponEntity.transform.rotation = Quaternion.LookRotation(direction);
        weaponEntity.owner = owner;
        weaponEntity.entityType = weaponEntityType;

        // Ensure tracking in entity manager
        if (!EntityMgr.inst.entities.Contains(weaponEntity))
            EntityMgr.inst.entities.Add(weaponEntity);

        return weaponEntity;
    }

    private void ReturnWeapon(Entity weaponEntity)
    {
        // Stop all active behaviors
        weaponEntity.StopAllCoroutines();
        UnitAI unitAI = weaponEntity.GetComponent<UnitAI>();
        if (unitAI != null) unitAI.StopAndRemoveAllCommands();

        // Clean up component references
        weaponEntity.creatorsEntity = null;

        // Remove from active tracking
        // foreach (WeaponDamage wd in weaponDamages)
        //     wd.currentWeaponEntities.Remove(weaponEntity);

        weapons.Remove(weaponEntity);
        EntityMgr.inst.entities.Remove(weaponEntity);

        // Pooling
        weaponEntity.gameObject.SetActive(false);
        if (!weaponPools.ContainsKey(weaponEntity.entityType))
            weaponPools[weaponEntity.entityType] = new Queue<Entity>();
        
        weaponPools[weaponEntity.entityType].Enqueue(weaponEntity);
    }

    IEnumerator TargetEntity(Entity weapon, WeaponData wd, Entity targetEntity, Vector3 targetPosition)
    {
        yield return new WaitForFixedUpdate();
        
        // Validate weapon state
        if (weapon == null || !weapon.gameObject.activeSelf)
            yield break;

        List<Entity> entities = new List<Entity> { weapon };
        switch (wd.behaviorType)
        {
            case WeaponBehaviors.SurfaceInterceptor:
                AIMgr.inst.HandleIntercept(entities, targetEntity, false);
                break;
            case WeaponBehaviors.AirInterceptor:
                AIMgr.inst.Handle3dIntercept(entities, targetEntity, false);
                break;
            case WeaponBehaviors.Dumb:
                AIMgr.inst.HandleDumbMove(entities, targetPosition, false);
                break;
            case WeaponBehaviors.Smart:
                AIMgr.inst.HandleSmartIntercept(entities, targetEntity, false);
                break;
            default:
                AIMgr.inst.HandleFollow(entities, targetEntity, Vector3.zero, false);
                break;
        }
    }

    public void LaunchWeapon(Entity launchingEntity, WeaponData wd, Entity target, Vector3 targetPosition)
    {
        if (wd == null) return;

        // Cooldown check
        if (Time.time - wd.lastShotTime < wd.cooldown) return;

        // Ammo check (handle infinite ammo case)
        if (wd.ammoCount == 0) return;
        if (wd.ammoCount > 0) wd.ammoCount--;

        // Calculate launch parameters
        Vector3 toTarget = target != null ? 
            (target.transform.position - launchingEntity.transform.position).normalized :
            (targetPosition - launchingEntity.transform.position).normalized;

        bool isForwardFacing = Vector3.Dot(launchingEntity.transform.forward, toTarget) > 0;
        Vector3 localPos = isForwardFacing ? wd.launchLocation : new Vector3(
            wd.launchLocation.x,
            wd.launchLocation.y,
            -wd.launchLocation.z
        );
        Vector3 localDir = isForwardFacing ? wd.launchDirection : -wd.launchDirection;

        Vector3 pos = launchingEntity.transform.TransformPoint(localPos);
        Vector3 dir = launchingEntity.transform.TransformDirection(localDir).normalized;

        // Get weapon from pool
        Entity ent = GetWeapon(wd.weaponEntityType, pos, dir, launchingEntity.owner);
        weapons.Add(ent);
        wd.currentWeaponEntities.Add(ent);
        ent.creatorsEntity = launchingEntity;

        // Start fresh movement routine
        StartCoroutine(TargetEntity(ent, wd, target, targetPosition));
        wd.lastShotTime = Time.time;
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
            DistanceMgr.inst.Initialize();
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