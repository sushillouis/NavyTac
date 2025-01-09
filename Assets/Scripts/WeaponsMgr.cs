using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.XR;
using System;
using System.IO;
using System.Text;
using System.Transactions;

public class WeaponsMgr : MonoBehaviour
{
    public static WeaponsMgr inst;
    private void Awake()
    {
        inst = this;
       
    }
    [System.Serializable]
    public class WeaponDamage
    {
        public EntityType weaponType;
        public List<TargetDamage> targetDamages;
    }
    
    [System.Serializable]
    public class TargetDamage
    {
        public EntityType targetType;
        public float damageValue;
    }
    public string fileNameCSV = "WeaponDamageMatrix.csv";
    public TextAsset csvFile;
    public List<WeaponDamage> weaponDamages;
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
                RaycastHit hit;
                Physics.Raycast(Camera.main.ScreenPointToRay(mousePos), out hit, float.MaxValue, AIMgr.inst.layerMask);
                Entity targetEntity = UIMgr.inst.FindClosestEntInRadius(hit.point);
                if (hit.point != null)
                {
                    if (behaviorType == WeaponBehaviors.Dumb){
                        LaunchWeapon(selectedEnt, wd, null, hit.point);
                        continue;
                    }
    
                    if (targetEntity != null && targetEntity.owner != selectedEnt.owner)
                    {
                        Debug.Log("Smart selected: " + selectedEnt.name + " with weapon: " + wd.weaponEntityType + " at " + targetEntity.name);
                        LaunchWeapon(selectedEnt, wd, targetEntity, hit.point);
                    }
                    
                }
            }
        }

    }
    public void DestroyEntity(Entity entity) {
    
        // Toggle RTS view if necessary
        if (!CameraMgr.inst.isRTSMode && CameraMgr.inst.YawNode.transform.parent.parent.name == entity.name) {
            CameraMgr.inst.ToggleRTSView();
        }
        
        // Stop and remove all commands from UnitAI, if present
        UnitAI unitAI = entity.GetComponentInChildren<UnitAI>();
        if (unitAI != null){
            unitAI.StopAndRemoveAllCommands();
        }
        
        // Remove entity from the selection if it is currently selected
        if (SelectionMgr.inst.selectedEntities.Contains(entity))  {
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

    [ContextMenu("Damage Matrix to CSV")]
    public void DamageMatrixToCSV()
    {
        // Get all EntityTypes
        EntityType[] entityTypes = (EntityType[])Enum.GetValues(typeof(EntityType));

        // Build a dictionary for quick access to weaponDamages by weaponType
        Dictionary<EntityType, WeaponDamage> weaponDamageDict = new Dictionary<EntityType, WeaponDamage>();
        if (weaponDamages != null)
        {
            foreach (WeaponDamage wd in weaponDamages)
            {
                weaponDamageDict[wd.weaponType] = wd;
            }
        }

        // Create the CSV content
        StringBuilder csvContent = new StringBuilder();

        // Write the header
        csvContent.Append("WeaponType");
        foreach (EntityType targetType in entityTypes)
        {
            csvContent.Append(",");
            csvContent.Append(targetType.ToString());
        }

        csvContent.AppendLine();

        // Write each row for every weaponType in entityTypes
        foreach (EntityType weaponType in entityTypes)
        {
            csvContent.Append(weaponType.ToString());

            // Get the WeaponDamage for this weaponType, or create one with zero damages
            WeaponDamage weaponDamage;
            if (weaponDamageDict.ContainsKey(weaponType))
            {
                weaponDamage = weaponDamageDict[weaponType];
            }
            else
            {
                weaponDamage = new WeaponDamage
                {
                    weaponType = weaponType,
                    targetDamages = new List<TargetDamage>()
                };
            }

            // Create a dictionary for quick lookup of damage values
            Dictionary<EntityType, float> targetDamageDict = new Dictionary<EntityType, float>();
            if (weaponDamage.targetDamages != null)
            {
                foreach (TargetDamage td in weaponDamage.targetDamages)
                {
                    targetDamageDict[td.targetType] = td.damageValue;
                }
            }

            // For each targetType, get the damageValue
            foreach (EntityType targetType in entityTypes)
            {
                csvContent.Append(",");
                if (targetDamageDict.ContainsKey(targetType))
                {
                    csvContent.Append(targetDamageDict[targetType].ToString());
                }
                else
                {
                    csvContent.Append("0");
                }
            }

            csvContent.AppendLine();
        }

        // Save the CSV file
        string filePath = Application.dataPath +"/"+fileNameCSV;
        File.WriteAllText(filePath, csvContent.ToString());
        Debug.Log("Damage matrix saved to " + filePath);
    }
    [ContextMenu("CSV To Damage Matrix")]
    public void CSVToDamageMatrix()
    {
        string filePath = Application.dataPath +"/"+csvFile.name+".csv";
    

        // Check if the file exists
        if (!File.Exists(filePath))
        {
            Debug.LogError("Damage matrix CSV file not found at " + filePath);
            return;
        }

        // Read all lines from the CSV file
        string[] lines = File.ReadAllLines(filePath);

        if (lines.Length < 2)
        {
            Debug.LogError("Damage matrix CSV file is empty or improperly formatted.");
            return;
        }

        // Clear the existing weaponDamages list
        weaponDamages = new List<WeaponDamage>();

        // Parse the header to get the target types
        string headerLine = lines[0];
        string[] headers = headerLine.Split(',');

        // The first column is "WeaponType", so target types start from index 1
        List<EntityType> targetTypes = new List<EntityType>();
        for (int i = 1; i < headers.Length; i++)
        {
            try
            {
                EntityType targetType = (EntityType)Enum.Parse(typeof(EntityType), headers[i]);
                targetTypes.Add(targetType);
            }
            catch (Exception e)
            {
                Debug.LogError("Invalid EntityType in header: " + headers[i] + ". Error: " + e.Message);
                return;
            }
        }

        // Parse each line to build weaponDamages
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            string[] values = line.Split(',');

            if (values.Length != headers.Length)
            {
                Debug.LogError("Line " + (i + 1) + " is improperly formatted.");
                continue; // Skip this line
            }

            // Parse weaponType
            EntityType weaponType;
            try
            {
                weaponType = (EntityType)Enum.Parse(typeof(EntityType), values[0]);
            }
            catch (Exception e)
            {
                Debug.LogError("Invalid EntityType in weaponType at line " + (i + 1) + ": " + values[0] + ". Error: " + e.Message);
                continue; // Skip this line
            }

            // Create WeaponDamage object
            WeaponDamage weaponDamage = new WeaponDamage
            {
                weaponType = weaponType,
                targetDamages = new List<TargetDamage>()
            };

            // Parse damage values for each target type
            for (int j = 1; j < values.Length; j++)
            {
                EntityType targetType = targetTypes[j - 1]; // Index adjusted because headers start from index 1

                float damageValue;
                if (float.TryParse(values[j], out damageValue))
                {
                    TargetDamage targetDamage = new TargetDamage
                    {
                        targetType = targetType,
                        damageValue = damageValue
                    };
                    weaponDamage.targetDamages.Add(targetDamage);
                }
                else
                {
                    Debug.LogError("Invalid damage value at line " + (i + 1) + ", column " + (j + 1) + ": " + values[j]);
                    // Optionally, you can assign a default value or skip adding this targetDamage
                }
            }

            // Add to weaponDamages list
            weaponDamages.Add(weaponDamage);
        }

        Debug.Log("Damage matrix loaded from " + filePath);
    }
    public float GetDamage(EntityType weaponType, EntityType targetType)
    {
        // Find the WeaponDamage entry for this weapon type
        WeaponDamage weaponDamage = weaponDamages.Find(wd => wd.weaponType == weaponType);

        // If no weapon damage entry found, return 0
        if (weaponDamage == null)
        {
            Debug.LogWarning($"No damage entry found for weapon type: {weaponType}");
            return 0f;
        }

        // Find the TargetDamage entry for this target type
        TargetDamage targetDamage = weaponDamage.targetDamages.Find(td => td.targetType == targetType);

        // If no target damage entry found, return 0
        if (targetDamage == null)
        {
            Debug.LogWarning($"No damage entry found for weapon type: {weaponType} against target type: {targetType}");
            return 0f;
        }

        return targetDamage.damageValue;
    }
}
