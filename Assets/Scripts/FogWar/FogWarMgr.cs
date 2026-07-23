using System.Collections.Generic;
using UnityEngine;

public class FogWarMgr : MonoBehaviour
{
    public static FogWarMgr inst;

    [Header("Fog of War Settings")]
    public bool FOW;
    public List<PlayerSide> playerSide; // List of player sides that reveal fog

    [Header("Fog Plane Settings")]
    public Material fogMaterial; // Material with Unlit/FogOfWar shader
    public Vector2 fogPlaneSize = new Vector2(18250f, 18250f);
    public float heightAboveMap = 50f;
    public Color previouslyRevealedColor = new Color(0f, 0f, 0f, 0.7f); // Transparent dull state
    public Color fogColor = Color.black; // Hidden state (opaque black)

    [Header("Grid Settings")]
    [Range(1, 1000)] public int gridWidthOverride = 128; // Override for grid width
    [Min(0.1f)] public float gridCellSize; // 18250 / 128 for 128x128 grid
    public List<Entity> revelers = new List<Entity>();
    private List<Entity> _nonRevelers = new List<Entity>();
    public List<Entity> nonRevelers
    {
        get
        {
            _nonRevelers.RemoveAll(e => e == null || e.Equals(null));
            return _nonRevelers;
        }
        private set => _nonRevelers = value;
    }
    [Range(0, 10000f)] public float fogRevealRadius = 150f;
    [SerializeField] private float updateInterval = 0.2f; // Reduced frequency for WebGL
    [SerializeField] private float visibilityCheckInterval = 0.2f;

    private GameObject fogPlane;
    private Vector2 gridOrigin;
    private int gridWidth;
    private int gridHeight;
    private float[,] fogGrid; // Current visibility (0 = not visible, 1 = visible)
    private bool[,] exploredGrid; // Tracks if cell was ever visible
    private Mesh mesh;
    private Vector3[] vertices;
    private Color[] colors;
    private int[] triangles;
    private bool[] dirtyCells;
    private float updateTimer;
    private float visibilityCheckTimer;
    private bool isInitialized = false;
    private bool lastFOWState;
    private HashSet<Entity> revealedRigBalders = new HashSet<Entity>();

    private void Awake()
    {
        inst = this;
        lastFOWState = FOW;
        if (FOW)
        {
            PerformInitialization();
        }
    }

    void PerformInitialization()
    {
        if (isInitialized) return;

        InitializeFogPlane();
        if (fogPlane == null)
        {
            Debug.LogError("FogWarMgr: FogPlane is null after InitializeFogPlane.");
            return;
        }

        InitializeGrid();
        InitializeMesh();
        isInitialized = true;
        if (fogPlane != null)
        {
            fogPlane.SetActive(true);
        }
    }

    void InitializeFogPlane()
    {
        if (fogPlane == null)
        {
            fogPlane = new GameObject("FogOfWarPlane");
            fogPlane.AddComponent<MeshFilter>();
            fogPlane.AddComponent<MeshRenderer>();
            fogPlane.layer = LayerMask.NameToLayer("FOW");
        }

        fogPlane.transform.position = new Vector3(0, heightAboveMap, 0);
        fogPlane.transform.rotation = Quaternion.identity;

        if (fogMaterial != null)
        {
           MeshRenderer mr = fogPlane.GetComponent<MeshRenderer>();
            mr.material = fogMaterial;
            mr.material.renderQueue = 3001;
            mr.material.SetInt("_ZWrite", 0);
        }
        else
        {
            Debug.LogError("FogWarMgr: fogMaterial is null.");
        }
    }

    void InitializeGrid()
    {
        gridCellSize = fogPlaneSize.x / gridWidthOverride;
        gridWidth = Mathf.Max(1, Mathf.CeilToInt(fogPlaneSize.x / gridCellSize));
        gridHeight = Mathf.Max(1, Mathf.CeilToInt(fogPlaneSize.y / gridCellSize));
        gridOrigin = new Vector2(-fogPlaneSize.x / 2, -fogPlaneSize.y / 2);
        fogGrid = new float[gridWidth, gridHeight];
        exploredGrid = new bool[gridWidth, gridHeight];
        dirtyCells = new bool[gridWidth * gridHeight];
    }

    void InitializeMesh()
    {
        mesh = new Mesh();
        mesh.MarkDynamic();
        fogPlane.GetComponent<MeshFilter>().mesh = mesh;

        int vertexCount = (gridWidth + 1) * (gridHeight + 1);
        vertices = new Vector3[vertexCount];
        colors = new Color[vertexCount];
        triangles = new int[gridWidth * gridHeight * 6];

        float offsetX = fogPlaneSize.x / 2f;
        float offsetZ = fogPlaneSize.y / 2f;

        // Generate vertices on XZ plane
        for (int x = 0; x <= gridWidth; x++)
        {
            for (int z = 0; z <= gridHeight; z++)
            {
                int index = x + z * (gridWidth + 1);
                vertices[index] = new Vector3(x * gridCellSize - offsetX, heightAboveMap, z * gridCellSize - offsetZ);
                colors[index] = fogColor; // Hidden
            }
        }

        // Generate triangles
        int triIndex = 0;
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                int v0 = x + z * (gridWidth + 1);
                int v1 = v0 + 1;
                int v2 = v0 + (gridWidth + 1);
                int v3 = v2 + 1;

                triangles[triIndex++] = v0;
                triangles[triIndex++] = v2;
                triangles[triIndex++] = v1;
                triangles[triIndex++] = v1;
                triangles[triIndex++] = v2;
                triangles[triIndex++] = v3;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colors;
        mesh.RecalculateNormals();
    }

    void Update()
    {
        if (ReplayMgr.inst != null && ReplayMgr.inst.isReplaying)
        {
            FOW = false;
        }

        if (FOW != lastFOWState)
        {
            if (FOW)
            {
                if (!isInitialized)
                {
                    PerformInitialization();
                }
                else if (fogPlane != null)
                {
                    fogPlane.SetActive(true);
                }
            }
            else
            {
                if (fogPlane != null)
                {
                    fogPlane.SetActive(false);
                }
                if (EntityMgr.inst != null && EntityMgr.inst.entities != null)
                {
                    foreach (var entity in EntityMgr.inst.entities)
                    {
                        if (entity != null)
                        {
                            entity.isVisible = true;
                        }
                    }
                }
            }
            lastFOWState = FOW;
        }

        if (!FOW)
        {
            if (isInitialized && fogPlane != null && fogPlane.activeSelf)
            {
                fogPlane.SetActive(false);
            }
            if (EntityMgr.inst != null && EntityMgr.inst.entities != null)
            {
                foreach (var entity in EntityMgr.inst.entities)
                {
                    if (entity != null)
                    {
                        entity.isVisible = true;
                    }
                }
            }
            return;
        }

        if (!isInitialized)
        {
            PerformInitialization();
            if (!isInitialized) return;
        }

        if (fogPlane != null && !fogPlane.activeSelf)
        {
            fogPlane.SetActive(true);
        }

        updateTimer -= Time.deltaTime;
        if (updateTimer <= 0)
        {
            UpdateFog();
            updateTimer = updateInterval;
        }

        visibilityCheckTimer -= Time.deltaTime;
        if (visibilityCheckTimer <= 0)
        {
            UpdateRevealerVisibility();
            UpdateNonRevealerVisibility();
            visibilityCheckTimer = visibilityCheckInterval;
        }
    }

    void UpdateFog()
    {
        revelers = EntityMgr.inst.entities.FindAll(entity =>
            entity != null &&
            entity.owner != null &&
            entity.gameObject.activeInHierarchy &&
            playerSide.Contains(entity.owner.playerSide) &&
            entity.entityClass != EntityClass.Missile);

        _nonRevelers = EntityMgr.inst.entities.FindAll(entity =>
            entity != null &&
            entity.owner != null &&
            entity.gameObject.activeInHierarchy &&
            !playerSide.Contains(entity.owner.playerSide) &&
            entity.entityClass != EntityClass.Missile);

        // FIX 2: Removed the "if (revelers.Count == 0)" instant-clear block.
        // Let the natural decay below handle it to prevent one-frame black flashes.

        // FIX 3: Consistent decay independent of frame rate spikes.
        // Instead of Time.deltaTime, we use a fixed decay multiplier appropriate for the 0.2s interval.
        // 0.7f means it retains 70% visibility every 0.2s until revealed again. Adjust to taste (higher = slower fade).
        float decayMultiplier = 0.75f; 

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                if (fogGrid[x, z] > 0)
                {
                    // Apply fixed decay
                    fogGrid[x, z] *= decayMultiplier;

                    // Clean cutoff to zero to stop unnecessary updates
                    if (fogGrid[x, z] < 0.05f) fogGrid[x, z] = 0f;
                    
                    dirtyCells[x + z * gridWidth] = true;
                }
            }
        }

        // Reveal areas
        foreach (var reveler in revelers)
        {
            if (reveler == null || reveler.transform == null) continue;
            RevealArea(reveler.transform.position, Mathf.Max(reveler.length, fogRevealRadius));
        }

        UpdateMeshColors();
    }

    void RevealArea(Vector3 worldPos, float radius)
    {
        // Convert world position to grid coordinates
        int centerX = Mathf.FloorToInt((worldPos.x - gridOrigin.x) / gridCellSize);
        int centerZ = Mathf.FloorToInt((worldPos.z - gridOrigin.y) / gridCellSize);
        int radiusCells = Mathf.CeilToInt(radius / gridCellSize);

        // Define falloff range for smooth edges (e.g., last 10% of radius)
        float falloffDistance = radius * 0.1f; // Adjust this for sharper or smoother falloff
        float innerRadius = radius - falloffDistance;

        for (int x = centerX - radiusCells; x <= centerX + radiusCells; x++)
        {
            for (int z = centerZ - radiusCells; z <= centerZ + radiusCells; z++)
            {
                if (x >= 0 && x < gridWidth && z >= 0 && z < gridHeight)
                {
                    // Calculate the world position of the current grid cell's center
                    Vector2 cellWorldPos = new Vector2(
                        gridOrigin.x + (x + 0.5f) * gridCellSize,
                        gridOrigin.y + (z + 0.5f) * gridCellSize
                    );
                    // Calculate the actual distance from the reveal center to the cell center
                    float dist = Vector2.Distance(
                        new Vector2(worldPos.x, worldPos.z),
                        cellWorldPos
                    );
                    // Apply smooth falloff
                    if (dist <= radius)
                    {
                        float visibility;
                        if (dist <= innerRadius)
                        {
                            visibility = 1f; // Fully visible inside inner radius
                        }
                        else
                        {
                            // Linear falloff between innerRadius and radius
                            float t = (dist - innerRadius) / falloffDistance;
                            visibility = Mathf.Lerp(1f, 0f, t);
                        }
                        // Update fogGrid with the maximum visibility value
                        fogGrid[x, z] = Mathf.Max(fogGrid[x, z], visibility);
                        exploredGrid[x, z] = true;
                        dirtyCells[x + z * gridWidth] = true;
                    }
                }
            }
        }
    }

    void UpdateMeshColors()
    {
        bool needsUpdate = false;
        for (int i = 0; i < dirtyCells.Length; i++)
        {
            if (dirtyCells[i]) { needsUpdate = true; break; }
        }
        if (!needsUpdate) return;

        for (int x = 0; x <= gridWidth; x++)
        {
            for (int z = 0; z <= gridHeight; z++)
            {
                int index = x + z * (gridWidth + 1);
                float visibility = 0f;
                bool isExplored = false;
                int count = 0;

                if (x < gridWidth && z < gridHeight)
                {
                    visibility = Mathf.Max(visibility, fogGrid[x, z]);
                    isExplored |= exploredGrid[x, z];
                    count++;
                }
                if (x > 0 && z < gridHeight)
                {
                    visibility = Mathf.Max(visibility, fogGrid[x - 1, z]);
                    isExplored |= exploredGrid[x - 1, z];
                    count++;
                }
                if (z > 0 && x < gridWidth)
                {
                    visibility = Mathf.Max(visibility, fogGrid[x, z - 1]);
                    isExplored |= exploredGrid[x, z - 1];
                    count++;
                }
                if (x > 0 && z > 0)
                {
                    visibility = Mathf.Max(visibility, fogGrid[x - 1, z - 1]);
                    isExplored |= exploredGrid[x - 1, z - 1];
                    count++;
                }

                if (visibility > 0.5f)
                    colors[index] = Color.clear; // Visible
                else if (isExplored)
                    colors[index] = previouslyRevealedColor; // Revealed but not visible
                else
                    colors[index] = fogColor; // Hidden
            }
        }
        mesh.colors = colors;
        for (int i = 0; i < dirtyCells.Length; i++) dirtyCells[i] = false;
    }

    private void UpdateRevealerVisibility()
    {
        foreach (Entity reveler in revelers)
        {
            if (reveler == null || reveler.transform == null) continue;
            reveler.isVisible = true;
        }
    }

    private void UpdateNonRevealerVisibility()
    {
        List<Entity> currentNonRevelers = nonRevelers;
        foreach (var entity in currentNonRevelers)
        {
            if (entity == null || entity.transform == null) continue;

            bool isVisible = false;
            Vector3 pos = entity.transform.position;
            int gridX = Mathf.FloorToInt((pos.x - gridOrigin.x) / gridCellSize);
            int gridZ = Mathf.FloorToInt((pos.z - gridOrigin.y) / gridCellSize);

            if (gridX >= 0 && gridX < gridWidth && gridZ >= 0 && gridZ < gridHeight)
            {
                isVisible = fogGrid[gridX, gridZ] > 0.5f;
            }

            if (entity.entityType == EntityType.Rig_Balder)
            {
                if (revealedRigBalders.Contains(entity))
                {
                    entity.isVisible = true;
                }
                else if (isVisible)
                {
                    revealedRigBalders.Add(entity);
                    entity.isVisible = true;
                }
                else
                {
                    entity.isVisible = false;
                }
            }
            else
            {
                entity.isVisible = isVisible;
            }
        }
    }

    public void CleanupEntities()
    {
        revelers.RemoveAll(e => e == null || e.Equals(null));
        _nonRevelers.RemoveAll(e => e == null || e.Equals(null));
    }

    public void ResetFog()
    {
        if (!FOW || !isInitialized) return;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                fogGrid[x, z] = 0f;
                exploredGrid[x, z] = false;
                dirtyCells[x + z * gridWidth] = true;
            }
        }
        UpdateMeshColors();
        revealedRigBalders.Clear();
        UpdateNonRevealerVisibility();
    }

    void OnDisable()
    {
        if (fogPlane != null)
        {
            fogPlane.SetActive(false);
        }
    }

    void OnDestroy()
    {
        revelers.Clear();
        _nonRevelers.Clear();
        if (mesh != null)
        {
            Destroy(mesh);
        }
        if (fogPlane != null)
        {
            Destroy(fogPlane);
        }
    }

    // Serialization for persistence
    [System.Serializable]
    private class FogData
    {
        public float[] fog;
    }

    [System.Serializable]
    private class ExploredData
    {
        public bool[] explored;
    }

    public void SaveFogState()
    {
        if (!isInitialized) return;

        FogData fogData = new FogData { fog = new float[gridWidth * gridHeight] };
        ExploredData exploredData = new ExploredData { explored = new bool[gridWidth * gridHeight] };

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                int index = x + z * gridWidth;
                fogData.fog[index] = fogGrid[x, z];
                exploredData.explored[index] = exploredGrid[x, z];
            }
        }

        PlayerPrefs.SetString("FogOfWarState", JsonUtility.ToJson(fogData));
        PlayerPrefs.SetString("ExploredState", JsonUtility.ToJson(exploredData));
        PlayerPrefs.Save();
    }

    void LoadFogState()
    {
        if (PlayerPrefs.HasKey("FogOfWarState") && PlayerPrefs.HasKey("ExploredState"))
        {
            string jsonFog = PlayerPrefs.GetString("FogOfWarState");
            string jsonExplored = PlayerPrefs.GetString("ExploredState");

            FogData fogData = JsonUtility.FromJson<FogData>(jsonFog);
            ExploredData exploredData = JsonUtility.FromJson<ExploredData>(jsonExplored);

            if (fogData != null && exploredData != null && fogData.fog.Length == gridWidth * gridHeight && exploredData.explored.Length == gridWidth * gridHeight)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    for (int z = 0; z < gridHeight; z++)
                    {
                        int index = x + z * gridWidth;
                        fogGrid[x, z] = fogData.fog[index];
                        exploredGrid[x, z] = exploredData.explored[index];
                        dirtyCells[index] = true;
                    }
                }
                UpdateMeshColors();
            }
        }
    }
}