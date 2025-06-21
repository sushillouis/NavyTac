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

    public void handleWeapon(Vector2 mousePos)
    {
        List<Entity> selectedEntities = SelectionMgr.inst.selectedEntities;
        if (selectedEntities == null || selectedEntities.Count == 0) return;

        foreach (Entity selectedEnt in selectedEntities)
        {
            Ray ray = Camera.main.ScreenPointToRay(mousePos);
            if (!Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, AIMgr.inst.layerMask)) continue;

            // Find the closest entity using a physics overlap sphere
            Entity targetEntity = FindClosestEntityWithCollider(hit.point, 5f); // 5f is an example radius, adjust as needed

            if (targetEntity != null && targetEntity.entityClass!= EntityClass.Missile && !targetEntity.isGreyed) handleWeapon(selectedEnt, targetEntity);
        }
    }

    // New method to find the closest entity using colliders
    private Entity FindClosestEntityWithCollider(Vector3 position, float radius)
    {
        Collider[] hitColliders = Physics.OverlapSphere(position, radius);
        Entity closestEntity = null;
        float minDistanceSqr = float.MaxValue;

        foreach (var hitCollider in hitColliders)
        {
            Entity entity = hitCollider.GetComponentInParent<Entity>(); // Or GetComponent<Entity>() if Entity is on the same GameObject as the collider
            if (entity != null && !entity.isGreyed)
            {
                float distanceSqr = (entity.transform.position - position).sqrMagnitude;
                if (distanceSqr < minDistanceSqr)
                {
                    minDistanceSqr = distanceSqr;
                    closestEntity = entity;
                }
            }
        }
        return closestEntity;
    }

    public void handleWeapon(Entity entity, Entity targetEntity)
    {
        if (entity == null) return;
        
        WeaponsAspect weaponsAspect = entity.GetComponentInChildren<WeaponsAspect>();
        if (weaponsAspect == null || weaponsAspect.weapon == null) return;

        if (targetEntity != null && targetEntity.owner != entity.owner && 
            targetEntity.entityClass != EntityClass.Missile && 
            !targetEntity.isGreyed && 
            targetEntity.owner != PlayerMgr.inst.neutral && 
            entity.owner != PlayerMgr.inst.neutral)
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
            //Debug.Log("Reusing weapon from pool");
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

        Vector3 launchPos = wd.launchPoint.position; // Define launch position early
        float actualDistanceToTarget;


        Collider targetCollider = target.GetComponentInChildren<Collider>();

        if (targetCollider != null && targetCollider.enabled)
        {
            // Calculate the closest point on the target's collider to the launch position
            Vector3 closestPointOnTarget = targetCollider.ClosestPoint(launchPos);
            actualDistanceToTarget = Vector3.Distance(launchPos, closestPointOnTarget);
        }
        else
        {

            actualDistanceToTarget = Vector3.Distance(launchPos, target.transform.position);
            // Optionally, log a warning if a precise collider-based distance could not be determined:
            // if (targetCollider == null)
            //     //Debug.LogWarning($"Target {target.name} has no Collider. Using transform-based distance for range check.");
            // else if (!targetCollider.enabled)
            //     //Debug.LogWarning($"Target {target.name}'s Collider is disabled. Using transform-based distance for range check.");
        }

        if (wd.range < actualDistanceToTarget)
        {
            //Debug.Log($"Target out of range. Weapon Range: {wd.range}, Calculated Distance: {actualDistanceToTarget}");
            return;
        }

        if (wd.ammoCount > 0)
        {
            wd.ammoCount--;
        }
        // Note: If ammoCount was 0, the method would have returned from the initial check.

        // Calculate direction to aim the weapon (using targetPosition, which might be an intercept point)
        Vector3 directionToTargetAim = (targetPosition - launchPos).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(directionToTargetAim);
        Vector3 dir = targetRotation.eulerAngles; // Use rotation angles from target direction

        Entity ent = GetWeapon(wd.weaponEntityType, launchPos, dir, launchingEntity.owner, launchingEntity);
        if (ent == null)
        {
            //Debug.Log("No Weapon entity found or could be created/reused from pool.");
            return;
        }

        if (!ent.gameObject.activeSelf) // Check if the retrieved/created weapon is active
        {
            // This might indicate an issue with pooling or entity creation if it occurs unexpectedly
            //Debug.LogWarning("Weapon entity is not active immediately after GetWeapon call.");
        }

        weapons.Add(ent);
        wd.currentWeaponEntities.Add(ent);

        StartCoroutine(TargetEntity(ent, wd, target, targetPosition));
        wd.lastShotTime = Time.time;
    }

    public void DestroyEntity(Entity entity)
    {
        
        try
        {
            EntityMgr.inst.entitiesDict.Remove(entity.entityId);
            if (CameraMgr.inst != null)
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
            UnitAI unitAI = entity.GetComponentInChildren<UnitAI>();
            if (unitAI != null)
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
            FXMgr.inst.CreateExplosionAt(entity.position, 1, entity.transform.localScale.x);
            if (entity.entityRole == EntityRole.Base)
            {
                if (entity.owner != null)
                {
                    bool wasAIBase = entity.owner == PlayerMgr.inst.player2;

                    if (wasAIBase)
                    {
                        ScoreMgr.inst.playerWon = true;
                        ScoreMgr.inst.winReason = "Opponent Base Destroyed"; // Set win reason
                    }
                    else
                    {
                        ScoreMgr.inst.aiWon = true;
                        ScoreMgr.inst.winReason = "Your Base Was Destroyed";
                    }
                    ScoreMgr.inst.CheckVictory();
                }
            }
            else if (entity.entityClass != EntityClass.Missile)
            {
                // Check if owner has any combat entities left (non-missile, non-base)
                TactPlayer owner = entity.owner;
                if (owner != null)
                {
                    bool hasCombatEntities = EntityMgr.inst.entities.Any(e =>
                        e.owner == owner &&
                        e.entityClass != EntityClass.Missile &&
                        e.entityRole != EntityRole.Base);

                    if (!hasCombatEntities)
                    {
                        if (owner == PlayerMgr.inst.localPlayer)
                        {
                            ScoreMgr.inst.aiWon = true;
                            ScoreMgr.inst.winReason = "Lost All Friendly Combat Entities";
                        }
                        else if (owner == PlayerMgr.inst.player2)
                        {
                            ScoreMgr.inst.playerWon = true;
                            ScoreMgr.inst.winReason = "Opponent Lost All Combat Entities";
                        }
                        ScoreMgr.inst.CheckVictory();
                    }
                }
            }
        }
        
        catch (System.Exception e)
        {
            string entityName = entity != null ? entity.name : "null";
            //Debug.LogError($"Error in DestroyEntity for entity: {entityName}. Exception: {e.Message}");

        }
        
    }
    public class EntityTypes{
        public EntityType entityType;
        public float defaultDamage;
    }
    public void StopAllWeapons()
{
    foreach (Entity weapon in weapons)
    {
        if (weapon != null)
        {
            // Stop movement and AI
            weapon.StopAllCoroutines();
            if (weapon.TryGetComponent<UnitAI>(out var unitAI))
            {
                unitAI.StopAndRemoveAllCommands();
            }
            
            // Disable collision handling
            if (weapon.TryGetComponent<WeaponCollisionHandler>(out var handler))
            {
                handler.enabled = false;
            }
        }
    }
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
    // Add this method to WeaponsMgr class
public void DestroyAllWeaponsImmediately(bool includePooled = true)
{
    // Destroy active weapons
    List<Entity> weaponsToDestroy = weapons.ToList();
    foreach (Entity weapon in weaponsToDestroy)
    {
        if (weapon == null) continue;

        // Remove from management systems
        EntityMgr.inst.entities.Remove(weapon);
        // weapons.Remove(weapon); // This will be cleared at the end
        
        // Clean up components
        MinimapMgr.inst.RemoveMinimapIcon(weapon);
        
        // Stop AI and physics
        if (weapon.TryGetComponent<UnitAI>(out var unitAI))
        {
            unitAI.StopAndRemoveAllCommands();
        }
        
        // Immediate destruction
        GameObject.Destroy(weapon.gameObject);
    }
    weapons.Clear(); // Clear the set after iterating and destroying

    // Destroy pooled weapons if requested
    if (includePooled)
    {
        foreach (var playerEntry in weaponPools)
        {
            foreach (var typePool in playerEntry.Value)
            {
                while (typePool.Value.Count > 0)
                {
                    Entity pooledWeapon = typePool.Value.Dequeue();
                    if (pooledWeapon != null && pooledWeapon.gameObject != null)
                    {

                        MinimapMgr.inst.RemoveMinimapIcon(pooledWeapon); 
                        GameObject.Destroy(pooledWeapon.gameObject);
                    }
                }

            }
        }
        weaponPools.Clear(); 
    }
    if (DistanceMgr.inst != null)
    {
        DistanceMgr.inst.Initialize();
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