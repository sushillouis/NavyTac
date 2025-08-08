using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using System.Globalization;
using System;
using UnityEditor;
// using UnityEditor.SceneManagement;

public class EntityMgr : MonoBehaviour
{
    public static EntityMgr inst;
    
    [Header("Export Settings")]
    public string exportFileName = "entity_export.csv";
    
    void Awake()
    {
        inst = this;
        entities = new List<Entity>();
        entitiesDict = new Dictionary<int, Entity>();
    }
   
   void Update(){
        entities.RemoveAll(item => item == null);
   }

    public GameObject movableEntitiesRoot;
    public GameObject nonMoveableEntitiesRoot;
    public List<GameObject> entityPrefabs;
    public GameObject entitiesRoot;
    public List<Entity> entities;
    public Dictionary<int, Entity> entitiesDict;

    
    public int entityId = 0;

    public void Reset()
    {
        foreach (Entity entity in entities)
        {
            if (entity != null)
            {
                Destroy(entity.gameObject);
            }
        }
        entities.Clear();
        entitiesDict.Clear();
        entityId = 0;
    }

    public Entity CreateEntity(EntityType et, Vector3 position, Vector3 eulerAngles) {
        return CreateEntity(et, position, eulerAngles, PlayerMgr.inst.player1);
    }
    [ContextMenu("Export Entities to CSV")]
    public void ExportEntitiesToCSV()
    {
        if (entityPrefabs == null || entityPrefabs.Count == 0)
        {
            //Debug.LogWarning("No entities to export!");
            return;
        }

        string filePath = Path.Combine(Application.dataPath, exportFileName);
        StringBuilder csvContent = new StringBuilder();

        // CSV Header
        csvContent.AppendLine(
            "entityType,acceleration,turnRate,maxSpeed,minSpeed,cruiseSpeed," +
            "mass,length,width,height,maxFuel,maxRange,entityClass,maxHealth"
        );

        int exportedCount = 0;
        
        foreach (GameObject entityGO in entityPrefabs)
        {
            Entity entity = entityGO.GetComponent<Entity>(); 
            string ownerId = entity.owner != null ? entity.owner.playerId.ToString() : "null";
            string creatorId = entity.creatorsEntity != null ? 
                entity.creatorsEntity.entityId.ToString() : "null";

            csvContent.AppendLine(
                $"{entity.entityType}," +
                $"{entity.acceleration:F2},{entity.turnRate:F2}," +
                $"{entity.maxSpeed},{entity.minSpeed},{entity.cruiseSpeed}," +
                $"{entity.mass:F1},{entity.length:F1},{entity.width:F1},{entity.height:F1}," +
                $"{entity.maxFuel},{entity.maxRange}," +
                $"{entity.entityClass}," +
                $"{entity.maxHealth}," 
            );
            exportedCount++;
        }

        try
        {
            File.WriteAllText(filePath, csvContent.ToString(), Encoding.UTF8);
            //Debug.Log($"Successfully exported {exportedCount} entities to:\n{filePath}");
        }
        catch 
        {
            
        }
    }
   [ContextMenu("Import Entities from CSV")]
    public void ImportEntitiesFromCSV()
    {
        string filePath = Path.Combine(Application.dataPath, exportFileName);
        
        if (!File.Exists(filePath))
        {
            //Debug.LogWarning($"CSV file not found at: {filePath}");
            return;
        }

        try
        {
            string[] lines = File.ReadAllLines(filePath);
            if (lines.Length < 2)
            {
                //Debug.LogWarning("CSV file is empty or contains only headers");
                return;
            }

            int importedCount = 0;
            
            // Start from index 1 to skip header row
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                string[] fields = line.Split(',');

                // Handle potential trailing comma from export
                List<string> cleanedFields = new List<string>();
                foreach (string field in fields)
                {
                    string cleaned = field.Trim();
                    if (!string.IsNullOrEmpty(cleaned))
                    {
                        cleanedFields.Add(cleaned);
                    }
                }

                if (cleanedFields.Count < 14)
                {
                    //Debug.LogWarning($"Skipping line {i + 1}: Not enough fields ({cleanedFields.Count} instead of 13)");
                    continue;
                }

                string entityTypeString = cleanedFields[0];
                // Fix for EntityType comparison error
                GameObject entityPrefab = entityPrefabs.Find(go => 
                    go.GetComponent<Entity>().entityType.ToString() == entityTypeString);

                if (entityPrefab == null)
                {
                    //Debug.LogWarning($"Skipping line {i + 1}: Entity type '{entityTypeString}' not found in prefabs");
                    continue;
                }

                Entity entity = entityPrefab.GetComponent<Entity>();

                try
                {
                    // Parse and assign values with culture fix
                    entity.acceleration = float.Parse(cleanedFields[1], CultureInfo.InvariantCulture);
                    entity.originalAcceleration = entity.acceleration; // Ensure originalAcceleration is set
                    entity.turnRate = float.Parse(cleanedFields[2], CultureInfo.InvariantCulture);
                    entity.originalTurnRate = entity.turnRate; // Ensure originalTurnRate is set
                    entity.maxSpeed = float.Parse(cleanedFields[3], CultureInfo.InvariantCulture);
                    entity.originalMaxSpeed = entity.maxSpeed; // Ensure originalMaxSpeed is set
                    entity.minSpeed = float.Parse(cleanedFields[4], CultureInfo.InvariantCulture);
                    entity.cruiseSpeed = float.Parse(cleanedFields[5], CultureInfo.InvariantCulture);
                    entity.mass = float.Parse(cleanedFields[6], CultureInfo.InvariantCulture);
                    entity.length = float.Parse(cleanedFields[7], CultureInfo.InvariantCulture);
                    entity.width = float.Parse(cleanedFields[8], CultureInfo.InvariantCulture);
                    entity.height = float.Parse(cleanedFields[9], CultureInfo.InvariantCulture);
                    entity.maxFuel = float.Parse(cleanedFields[10], CultureInfo.InvariantCulture);
                    entity.maxRange = float.Parse(cleanedFields[11], CultureInfo.InvariantCulture);
                    entity.maxHealth = float.Parse(cleanedFields[13], CultureInfo.InvariantCulture);
                    // Fix for EntityClass conversion
                    entity.entityClass = (EntityClass)Enum.Parse(typeof(EntityClass), cleanedFields[12]);

                    importedCount++;
                    
                    #if UNITY_EDITOR
                    // Mark prefab as dirty to ensure changes are saved
                    EditorUtility.SetDirty(entityPrefab);
                    #endif
                }
                catch (FormatException e)
                {
                    //Debug.LogError($"Failed to parse values in line {i + 1}: {e.Message}");
                }
            }

            #if UNITY_EDITOR
            // Save all changes to assets
            AssetDatabase.SaveAssets();
            #endif

            //Debug.Log($"Successfully imported {importedCount} entities from {filePath}");
        }
        catch (Exception e)
        {
            //Debug.LogError($"Import failed: {e.Message}");
        }
    }



    // In EntityMgr.cs, within CreateEntity method
    public Entity CreateEntity(EntityType et, Vector3 position, Vector3 eulerAngles, TactPlayer player)
    {
        Entity entity = null;
        GameObject entityPrefab = entityPrefabs.Find(x => (x.GetComponent<Entity>().entityType == et));
        if (entityPrefab != null)
        {
            GameObject entityGo = Instantiate(entityPrefab, position, Quaternion.Euler(eulerAngles), entitiesRoot.transform);
            if (entityGo != null)
            {
                entity = entityGo.GetComponent<Entity>();
                entity.entityId = entityId;
                entityGo.name = et.ToString() + entityId++;
                entity.owner = player;
                entity.heading = entity.desiredHeading = eulerAngles.y;

                if (FogWarMgr.inst != null && FogWarMgr.inst.FOW && entity.entityClass != EntityClass.Missile)
                {
                    entity.isVisible = false;
                }
                else
                {
                    entity.isVisible = true;
                }

                entities.Add(entity);
                entitiesDict.Add(entity.entityId, entity);

                // Record creation event

            }
        }
        DistanceMgr.inst.Initialize();
        return entity;
    }
    public Entity GetEntityPrefab(EntityType et)
    {
        if (entityPrefabs == null || entityPrefabs.Count == 0)
        {
            //Debug.LogWarning("No entity prefabs available.");
            return null;
        }

        GameObject prefab = entityPrefabs.Find(x => x.GetComponent<Entity>().entityType == et);
        if (prefab != null)
        {
            return prefab.GetComponent<Entity>();
        }
        else
        {
            //Debug.LogWarning($"Entity prefab for {et} not found.");
            return null;
        }
    }
   
[System.Serializable]
    public struct PrefabMaterialPair
    {
        public GameObject prefab;     // exact prefab asset (drag-and-drop)
        public Material replacement;  // e.g. “DDG51_Hull”
    }

    [Header("Bright-Green ➜ Replacement list")]
    [SerializeField] private List<PrefabMaterialPair> brightGreenOverrides = new();

    /* ─────────────────────────────────────────────────────────────
     * 2.  Context-menu command
     * ──────────────────────────────────────────────────────────── */
    [ContextMenu("🟢 Replace Bright-Green Materials")]
    private void ReplaceBrightGreenMaterials()
    {
        if (brightGreenOverrides.Count == 0)
        {
            Debug.LogWarning("No overrides set.  Populate ‘Bright-Green → Replacement list’ first.");
            return;
        }

        Color target = new Color(0.196f, 1f, 0.196f);     // #32FF32
        const float tol = 0.01f;
        int changeCount = 0;

        foreach (var pair in brightGreenOverrides)
        {
            if (pair.prefab == null || pair.replacement == null) continue;

            // Fetch all renderers inside the prefab asset
            var renderers = pair.prefab.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer rend in renderers)
            {
                var mats = rend.sharedMaterials;
                for (int i = 0; i < mats.Length; ++i)
                {
                    var mat = mats[i];
                    if (mat == null) continue;

                    if (IsApproximately(mat.color, target, tol))
                    {
                        // Record for undo + prefab dirtying
                        // Undo.RecordObject(rend, "Replace Bright-Green Material");
                        mats[i] = pair.replacement;
                        rend.sharedMaterials = mats;
                        changeCount++;

                        // If the renderer's GameObject is untagged, set its color to bright green and tag it
                        if (rend.gameObject.tag == "Untagged")
                        {
                            
                            rend.gameObject.tag = "BrightGreenColor";
                        }
                    }
                }
            }

            // Mark the prefab asset dirty so the change is saved
            // EditorUtility.SetDirty(pair.prefab);
        }

        // AssetDatabase.SaveAssets();
        // EditorSceneManager.MarkAllScenesDirty();

        Debug.Log($"✅ Replaced {changeCount} bright-green material(s).");
    }

    /* ─────────────────────────────────────────────────────────────
     * 3.  Helper
     * ──────────────────────────────────────────────────────────── */
    private static bool IsApproximately(Color a, Color b, float t) =>
        Mathf.Abs(a.r - b.r) < t &&
        Mathf.Abs(a.g - b.g) < t &&
        Mathf.Abs(a.b - b.b) < t;
}
