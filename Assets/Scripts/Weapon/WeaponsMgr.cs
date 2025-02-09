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

    public List<WeaponDamage> weaponDamages;
    public List<GameObject> WeaponPrefabs = new List<GameObject>();
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
                Debug.Log($"Smart selected: {selectedEnt.name} with weapon: {wd.weaponEntityType} at {targetEntity.name}");
                LaunchWeapon(selectedEnt, wd, targetEntity, hit.point);
            }
        }
    }

    IEnumerator TargetEntity(Entity weapon, WeaponData wd, Entity targetEntity, Vector3 targetPosition)
    {
        yield return new WaitForFixedUpdate();
        List<Entity> entities = new List<Entity>();
        entities.Add(weapon);
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
        if (wd == null) Debug.Log("Could not find weapon: " + wd.weaponEntityType);
        Debug.Log("last sho time: " + wd.lastShotTime + " cooldown: " + wd.cooldown + " ammo count: " + wd.ammoCount + "current time: " + Time.time);
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

    public float GetDamage(EntityType weaponType, EntityType targetType)
    {
        WeaponDamage weaponDamage = weaponDamages.Find(wd => wd.weaponType == weaponType);
        if (weaponDamage == null)
        {
            Debug.LogWarning($"No damage entry found for weapon type: {weaponType}");
            return 0f;
        }

        TargetDamage targetDamage = weaponDamage.targetDamages.Find(td => td.targetType == targetType);
        if (targetDamage == null)
        {
            Debug.LogWarning($"No damage entry found for weapon type: {weaponType} against target type: {targetType}");
            return 0f;
        }

        return targetDamage.damageValue;
    }

    public void DestroyEntity(Entity entity)
    {
        if (!CameraMgr.inst.isRTSMode && CameraMgr.inst.YawNode.transform.parent.parent.name == entity.name)
        {
            CameraMgr.inst.ToggleRTSView();
        }

        UnitAI unitAI = entity.GetComponentInChildren<UnitAI>();
        if (unitAI != null)
        {
            unitAI.StopAndRemoveAllCommands();
        }

        if (SelectionMgr.inst.selectedEntities.Contains(entity))
        {
            SelectionMgr.inst.selectedEntities.Remove(entity);
            SelectionMgr.inst.selectedEntity = SelectionMgr.inst.selectedEntities.Count > 0 ? SelectionMgr.inst.selectedEntities[0] : null;
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