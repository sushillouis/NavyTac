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
        
        Debug.Log($"[WeaponsMgr] Initialized with {weaponDamages?.Count} weapon damage entries");
        if (weaponDamages == null || weaponDamages.Count == 0)
        {
            Debug.LogError("[WeaponsMgr] ERROR: Weapon damages list not configured!");
        }
    }

    public void handleWeapon(Vector2 mousePos, WeaponBehaviors behaviorType)
    {
        Debug.Log($"[WeaponsMgr] Handling weapon action for behavior: {behaviorType}");
        
        List<Entity> selectedEntities = SelectionMgr.inst.selectedEntities;
        if (selectedEntities == null || selectedEntities.Count == 0)
        {
            Debug.LogWarning("[WeaponsMgr] No entities selected");
            return;
        }

        Debug.Log($"[WeaponsMgr] Processing {selectedEntities.Count} selected entities");
        foreach (Entity selectedEnt in selectedEntities)
        {
            Debug.Log($"[WeaponsMgr] Processing entity: {selectedEnt.name}");
            
            Ray ray = Camera.main.ScreenPointToRay(mousePos);
            RaycastHit hit;
            if (!Physics.Raycast(ray, out hit, float.MaxValue, AIMgr.inst.layerMask))
            {
                Debug.LogWarning($"[WeaponsMgr] Raycast missed for {selectedEnt.name}");
                continue;
            }

            Debug.DrawRay(ray.origin, ray.direction * 100, Color.red, 2f);
            Debug.Log($"[WeaponsMgr] Raycast hit: {hit.collider.name} at {hit.point}");

            Entity targetEntity = UIMgr.inst.FindClosestEntInRadius(hit.point);
            if (targetEntity == null)
            {
                Debug.LogWarning("[WeaponsMgr] No target entity found near hit point");
                continue;
            }

            Debug.Log($"[WeaponsMgr] Found target: {targetEntity.name} ({targetEntity.entityType})");
            handleWeapon(selectedEnt, targetEntity);
        }
    }

    public void handleWeapon(Entity entity, Entity targetEntity)
    {
        if (entity == null)
        {
            Debug.LogWarning("[WeaponsMgr] HandleWeapon called with null entity");
            return;
        }

        Debug.Log($"[WeaponsMgr] Handling weapon for {entity.name} targeting {targetEntity?.name}");
        
        WeaponsAspect weaponsAspect = entity.GetComponentInChildren<WeaponsAspect>();
        if (weaponsAspect == null)
        {
            Debug.LogWarning($"[WeaponsMgr] No WeaponsAspect found on {entity.name}");
            return;
        }

        WeaponData wd = weaponsAspect.weapon;
        if (wd == null)
        {
            Debug.LogError($"[WeaponsMgr] No WeaponData configured on {entity.name}");
            return;
        }

        if (targetEntity != null && targetEntity.owner != entity.owner)
        {
            Debug.Log($"[WeaponsMgr] Launching weapon from {entity.name} to {targetEntity.name}");
            LaunchWeapon(entity, wd, targetEntity, targetEntity.transform.position);
        }
    }
    
    private Entity GetWeapon(EntityType weaponType, Vector3 position, Vector3 direction, TactPlayer owner, Entity creatorEntity)
    {
        Debug.Log($"[WeaponsMgr] Getting weapon: {weaponType} for {owner.name}");

        if (!weaponPools.ContainsKey(owner))
        {
            Debug.Log($"[WeaponsMgr] Creating new weapon pool for player {owner.name}");
            weaponPools[owner] = new Dictionary<EntityType, Queue<Entity>>();
        }

        var ownerPool = weaponPools[owner];
        if (!ownerPool.ContainsKey(weaponType))
        {
            Debug.Log($"[WeaponsMgr] Creating new queue for {weaponType}");
            ownerPool[weaponType] = new Queue<Entity>();
        }

        if (ownerPool[weaponType].Count > 0)
        {
            var weapon = ReactivatePooledWeapon(ownerPool[weaponType], position, direction);
            Debug.Log($"[WeaponsMgr] Reusing pooled weapon: {weapon.name} (Remaining in pool: {ownerPool[weaponType].Count})");
            return weapon;
        }
        else
        {
            Debug.Log($"[WeaponsMgr] Creating new weapon instance of type {weaponType}");
            Entity weapon = EntityMgr.inst.CreateEntity(weaponType, position, direction, owner);
            weapon.creatorsEntity = creatorEntity;
            weapon.gameObject.SetActive(true);
            Debug.Log($"[WeaponsMgr] New weapon created: {weapon.name}");
            return weapon;
        }
    }

    private Entity ReactivatePooledWeapon(Queue<Entity> pool, Vector3 position, Vector3 direction)
    {
        var weapon = pool.Dequeue();
        
        
        Debug.Log($"[WeaponsMgr] Reactivating weapon: {weapon.name}");
        Debug.Log($"Before reset - Position: {weapon.transform.position}, Rotation: {weapon.transform.rotation.eulerAngles}");

        ResetWeaponPhysics(weapon);
        ResetWeaponAI(weapon);
        UpdateWeaponTransform(weapon, position, direction);
        weapon.gameObject.SetActive(true);
        Debug.Log($"After reset - Position: {weapon.transform.position}, Rotation: {weapon.transform.rotation.eulerAngles}");
        return weapon;
    }

    private void ResetWeaponPhysics(Entity weapon)
    {
        weapon.health = weapon.maxHealth;
        weapon.fuel = weapon.maxFuel;
        weapon.range = weapon.maxRange;
        weapon.desiredSpeed = 0;
        weapon.desiredHeading = 0;
        weapon.speed = 0;
        weapon.heading = 0;
        weapon.velocity = Vector3.zero;
        weapon.position = new Vector3(0,-100,0);
        weapon.transform.localEulerAngles = Vector3.zero;
        Debug.Log($"[WeaponsMgr] Reset physics for {weapon.name}");
    }

    private void ResetWeaponAI(Entity weapon)
    {
        UnitAI unitAI = weapon.GetComponent<UnitAI>();
        if (unitAI != null)
        {
            Debug.Log($"[WeaponsMgr] Resetting AI for {weapon.name}");
            unitAI.StopAndRemoveAllCommands();
        }
    }

    private void UpdateWeaponTransform(Entity weapon, Vector3 position, Vector3 direction)
    {
        EntityMgr.inst.entities.Add(weapon);
        weapon.transform.position = position;
        weapon.transform.rotation = Quaternion.LookRotation(direction);
        Debug.Log($"[WeaponsMgr] Set {weapon.name} position to {position} and rotation to {direction}");
    }

    private void ReturnWeapon(Entity weaponEntity)
    {
        Debug.Log($"[WeaponsMgr] Returning weapon to pool: {weaponEntity.name}");
        
        weaponEntity.StopAllCoroutines();
        UnitAI unitAI = weaponEntity.GetComponent<UnitAI>();
        if (unitAI != null)
        {
            Debug.Log($"[WeaponsMgr] Cleaning AI for {weaponEntity.name}");
            unitAI.StopAndRemoveAllCommands();
        }

        weaponEntity.creatorsEntity = null;

        var owner = weaponEntity.owner;
        if (!weaponPools.ContainsKey(owner))
        {
            Debug.LogWarning($"[WeaponsMgr] No pool found for owner {owner.name}, creating new");
            weaponPools[owner] = new Dictionary<EntityType, Queue<Entity>>();
        }

        var ownerPool = weaponPools[owner];
        if (!ownerPool.ContainsKey(weaponEntity.entityType))
        {
            Debug.Log($"[WeaponsMgr] Creating new queue for {weaponEntity.entityType}");
            ownerPool[weaponEntity.entityType] = new Queue<Entity>();
        }

        weaponEntity.gameObject.SetActive(false);
        ownerPool[weaponEntity.entityType].Enqueue(weaponEntity);
        Debug.Log($"[WeaponsMgr] Weapon {weaponEntity.name} returned to pool. New pool size: {ownerPool[weaponEntity.entityType].Count}");
    }

    IEnumerator TargetEntity(Entity weapon, WeaponData wd, Entity targetEntity, Vector3 targetPosition)
    {
        Debug.Log($"[WeaponsMgr] Starting TargetEntity routine for {weapon.name}");
        
        yield return new WaitForFixedUpdate();
        
        if (weapon == null || !weapon.gameObject.activeSelf)
        {
            Debug.LogWarning($"[WeaponsMgr] Weapon invalid or inactive in TargetEntity");
            yield break;
        }

        List<Entity> entities = new List<Entity> { weapon };
        Debug.Log($"[WeaponsMgr] Handling {wd.behaviorType} behavior for {weapon.name}");

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

        Debug.Log($"[WeaponsMgr] {wd.behaviorType} behavior initiated for {weapon.name}");
    }

    public void LaunchWeapon(Entity launchingEntity, WeaponData wd, Entity target, Vector3 targetPosition)
    {
        if (wd == null)
        {
            Debug.LogError("[WeaponsMgr] LaunchWeapon: No WeaponData provided!");
            return;
        }

        Debug.Log($"[WeaponsMgr] Attempting to launch  from {launchingEntity.name}");

        // Cooldown check
        float timeSinceLastShot = Time.time - wd.lastShotTime;
        if (timeSinceLastShot < wd.cooldown)
        {
            Debug.Log($"[WeaponsMgr] Cooldown active ({timeSinceLastShot:0.00}s < {wd.cooldown:0.00}s)");
            return;
        }

        // Ammo check
        if (wd.ammoCount == 0)
        {
            Debug.LogWarning("[WeaponsMgr] No ammo remaining");
            return;
        }
        if (wd.ammoCount > 0)
        {
            wd.ammoCount--;
            Debug.Log($"[WeaponsMgr] Ammo reduced to {wd.ammoCount}");
        }

        // Calculate launch parameters
        Vector3 pos = wd.launchPoint.position;
        Vector3 dir = wd.launchPoint.forward;

        // Get weapon from pool
        Entity ent = GetWeapon(wd.weaponEntityType, pos, dir, launchingEntity.owner, launchingEntity);
        if (ent == null)
        {
            Debug.LogError("[WeaponsMgr] Failed to create weapon entity!");
            return;
        }

        weapons.Add(ent);
        wd.currentWeaponEntities.Add(ent);
        ent.creatorsEntity = launchingEntity;

        Debug.Log($"[WeaponsMgr] Starting weapon behavior coroutine");
        StartCoroutine(TargetEntity(ent, wd, target, targetPosition));
        wd.lastShotTime = Time.time;
        // Debug.Log($"[WeaponsMgr] {wd.weaponName} launched successfully at {Time.time}");
    }

    public void DestroyEntity(Entity entity)
    {
        Debug.Log($"[WeaponsMgr] DestroyEntity called for {entity.name}");

        MinimapMgr.inst.RemoveMinimapIcon(entity);
        if (!CameraMgr.inst.isRTSMode && CameraMgr.inst.YawNode.transform.parent.parent.name == entity.name)
        {
            Debug.Log($"[WeaponsMgr] Switching to RTS view for destroyed entity");
            CameraMgr.inst.ToggleRTSView();
        }

        if (weapons.Contains(entity))
        {
            Debug.Log($"[WeaponsMgr] Processing weapon entity destruction");
            EntityMgr.inst.entities.Remove(entity);
            ReturnWeapon(entity);
            weapons.Remove(entity);
            Debug.Log($"[WeaponsMgr] Weapon {entity.name} cleaned up");
            return;
        }

        UnitAI unitAI = entity.GetComponentInChildren<UnitAI>();
        if (unitAI != null)
        {
            Debug.Log($"[WeaponsMgr] Cleaning AI for {entity.name}");
            unitAI.StopAndRemoveAllCommands();
        }

        if (SelectionMgr.inst.selectedEntities.Contains(entity))
        {
            Debug.Log($"[WeaponsMgr] Removing from selection");
            SelectionMgr.inst.selectedEntities.Remove(entity);
            SelectionMgr.inst.selectedEntity = 
                (SelectionMgr.inst.selectedEntities.Count > 0) 
                ? SelectionMgr.inst.selectedEntities[0] 
                : null;
        }

        EntityMgr.inst.entities.Remove(entity);
        DistanceMgr.inst.Initialize();
        Debug.Log($"[WeaponsMgr] Destroying game object: {entity.name}");
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