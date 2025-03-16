using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class WeaponsMgr : MonoBehaviour
{
    public static WeaponsMgr inst;
    public DamageMatrix damageMatrix;
    private Dictionary<TactPlayer, Dictionary<EntityType, Queue<Entity>>> weaponPools = new Dictionary<TactPlayer, Dictionary<EntityType, Queue<Entity>>>();
    public List<WeaponDamage> weaponDamages;
    public HashSet<Entity> weapons = new HashSet<Entity>();
   

    private void Awake()
    {
        inst = this;
        damageMatrix = new DamageMatrix();
        damageMatrix.InitializeDamageMatrix(weaponDamages);
    }

    public void handleWeapon(Vector2 mousePos, WeaponBehaviors behaviorType)
    {
        List<Entity> selectedEntities = SelectionMgr.inst.selectedEntities;
        if (selectedEntities == null || selectedEntities.Count == 0) return;

        foreach (Entity selectedEnt in selectedEntities)
        {
            Ray ray = Camera.main.ScreenPointToRay(mousePos);
            if (!Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, AIMgr.inst.layerMask)) continue;

            Entity targetEntity = UIMgr.inst.FindClosestEntInRadius(hit.point);
            if (targetEntity != null) handleWeapon(selectedEnt, targetEntity);
        }
    }

    public void handleWeapon(Entity entity, Entity targetEntity)
    {
        if (entity == null) return;
        
        WeaponsAspect weaponsAspect = entity.GetComponentInChildren<WeaponsAspect>();
        if (weaponsAspect == null || weaponsAspect.weapon == null) return;

        if (targetEntity != null && targetEntity.owner != entity.owner)
        {
            LaunchWeapon(entity, weaponsAspect.weapon, targetEntity, targetEntity.transform.position);
        }
    }
    
    private Entity GetWeapon(EntityType weaponType, Vector3 position, Vector3 direction, TactPlayer owner, Entity creatorEntity)
    {
        if (!weaponPools.ContainsKey(owner))
        {
            weaponPools[owner] = new Dictionary<EntityType, Queue<Entity>>();
        }

        var ownerPool = weaponPools[owner];
        if (!ownerPool.ContainsKey(weaponType))
        {
            ownerPool[weaponType] = new Queue<Entity>();
        }

        if (ownerPool[weaponType].Count > 0)
        {
            Debug.Log("Reusing weapon from pool");
            return ReactivatePooledWeapon(ownerPool[weaponType], position, direction ,creatorEntity);
        }

        Entity weapon = EntityMgr.inst.CreateEntity(weaponType, position, direction, owner);
        weapon.creatorsEntity = creatorEntity;
        return weapon;
    }

    private Entity ReactivatePooledWeapon(Queue<Entity> pool, Vector3 position, Vector3 direction, Entity creatorEntity)
{
    var weapon = pool.Dequeue();
    ResetWeaponPhysics(weapon);
    weapon.GetComponentInChildren<Oriented3dPhysics>().ResetAltitude();
    UpdateWeaponTransform(weapon, position, direction);
    
    // Add these lines to set altitude correctly
    var phx3d = weapon.GetComponentInChildren<Oriented3dPhysics>();
    if (phx3d != null)
    {
        phx3d.altitude = position.y;         // Match current altitude to launch position
    }
    weapon.creatorsEntity = creatorEntity;
    weapon.gameObject.SetActive(true);
    UIAspect uiAspect = weapon.GetComponentInChildren<UIAspect>();
    if (uiAspect != null && uiAspect.minimapIcon != null)
    {
        MinimapMgr.inst.CreateMinimapIcon(weapon, uiAspect.minimapIcon);
    }
    DistanceMgr.inst.Initialize();
    return weapon;
    }

    private void ResetWeaponPhysics(Entity weapon)
    {
        
        
        weapon.health = 100;
        weapon.fuel = weapon.maxFuel;
        weapon.range = weapon.maxRange;
        weapon.desiredSpeed = 0;
        weapon.desiredHeading = 0;
        weapon.speed = 0;
        weapon.heading = 0;
        weapon.velocity = Vector3.zero;
        weapon.position = new Vector3(0,1,0);
        weapon.transform.position = new Vector3(0,1,0);
        weapon.transform.localEulerAngles = Vector3.zero;
        weapon.GetComponentInChildren<Oriented3dPhysics>().ResetAltitude();
        
    }

    private void ResetWeaponAI(Entity weapon)
    {
        if (weapon.TryGetComponent<UnitAI>(out var unitAI))
        {
            unitAI.StopAndRemoveAllCommands();
        }
    }

    private void UpdateWeaponTransform(Entity weapon, Vector3 position, Vector3 direction)
    {
        EntityMgr.inst.entities.Add(weapon);
        weapon.position = position;
        weapon.transform.localEulerAngles = direction;
        weapon.transform.position = position;
        weapon.heading = direction.y;
        
    }

    private void ReturnWeapon(Entity weaponEntity)
    {
        weaponEntity.StopAllCoroutines();
        ResetWeaponPhysics(weaponEntity);
        ResetWeaponAI(weaponEntity);

        weaponEntity.creatorsEntity = null;
        var owner = weaponEntity.owner;

        if (!weaponPools.ContainsKey(owner))
        {
            weaponPools[owner] = new Dictionary<EntityType, Queue<Entity>>();
        }

        var ownerPool = weaponPools[owner];
        if (!ownerPool.ContainsKey(weaponEntity.entityType))
        {
            ownerPool[weaponEntity.entityType] = new Queue<Entity>();
        }

        weaponEntity.gameObject.SetActive(false);
        ownerPool[weaponEntity.entityType].Enqueue(weaponEntity);
    }

    IEnumerator TargetEntity(Entity weapon, WeaponData wd, Entity targetEntity, Vector3 targetPosition)
    {
        yield return new WaitForFixedUpdate();
        
        if (weapon == null || !weapon.gameObject.activeSelf) yield break;

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

        float timeSinceLastShot = Time.time - wd.lastShotTime;
        if (timeSinceLastShot < wd.cooldown || wd.ammoCount == 0) return;

        if (wd.ammoCount > 0)
        {
            wd.ammoCount--;
        }
        if(wd.range< Vector3.Distance(launchingEntity.transform.position, target.transform.position))
        {
            Debug.Log("Target out of range");
            return;
        }

        Vector3 pos = wd.launchPoint.position;
        Debug.Log(wd.launchPoint.position);
        Vector3 dir = launchingEntity.transform.localEulerAngles;

        Entity ent = GetWeapon(wd.weaponEntityType, pos, dir, launchingEntity.owner, launchingEntity);
        if (ent == null)
        {
            Debug.Log("No Weapon entity found");
            return;
        }

        if (ent.gameObject.activeSelf == false)
        {
            Debug.Log("entity not active");
        }

        weapons.Add(ent);
        wd.currentWeaponEntities.Add(ent);
        

        StartCoroutine(TargetEntity(ent, wd, target, targetPosition));
        wd.lastShotTime = Time.time;
    }

    public void DestroyEntity(Entity entity)
    {
        
        try{
            
            if(CameraMgr.inst!= null)
            {
                if (!CameraMgr.inst.isRTSMode && CameraMgr.inst.YawNode.transform.parent.parent.name == entity.name)
                {
                CameraMgr.inst.ToggleRTSView();
                }
            }
            if (weapons.Contains(entity))
            {
                EntityMgr.inst.entities.Remove(entity);
                ReturnWeapon(entity);
                weapons.Remove(entity);
                DistanceMgr.inst.Initialize();
                return;
            }
            MinimapMgr.inst.RemoveMinimapIcon(entity);
            if (entity.TryGetComponent<UnitAI>(out var unitAI))
            {
                unitAI.StopAndRemoveAllCommands();
            }

            if (SelectionMgr.inst.selectedEntities.Contains(entity))
            {
                SelectionMgr.inst.selectedEntities.Remove(entity);
                SelectionMgr.inst.selectedEntity = SelectionMgr.inst.selectedEntities.Count > 0 
                    ? SelectionMgr.inst.selectedEntities[0] 
                    : null;
            }

            EntityMgr.inst.entities.Remove(entity);
            DistanceMgr.inst.Initialize();
            Destroy(entity.gameObject);
        }
        catch (System.Exception e){
            string entityName = entity != null ? entity.name : "null";
            Debug.LogError($"Error in DestroyEntity for entity: {entityName}. Exception: {e.Message}");
        
        }
        
    }
    public class EntityTypes{
        public EntityType entityType;
        public float defaultDamage;
    }

    [Header("Context Menu")]
    
    public List<EntityTypes> weaponTypes = new List<EntityTypes>();
    public GameObject MovableEntitiesRoot;
    public string fileNameCSV = "WeaponDamageMatrix.csv";
    public TextAsset csvFile;

    [ContextMenu("Add Weapons Aspect To All Entities")]
    public void AddWeaponsAspectToAllEntities()
    {
        foreach (UIAspect uiAspect in MovableEntitiesRoot.transform.GetComponentsInChildren<UIAspect>(true))
        {
            GameObject aspectRoot = uiAspect.transform.parent.gameObject;
            if (aspectRoot != null && !aspectRoot.transform.parent.name.Contains("DDG"))
            {
                // Add weapons aspect logic here
            }
        }
    }

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