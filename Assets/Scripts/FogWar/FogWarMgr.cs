using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;


public class FogWarMgr : MonoBehaviour
{
    
    public bool FOW;
    public List<PlayerSide> playerSide;
    
    [Header("Fog Plane Settings")]
    public Material fogMaterial;
    public Vector2 fogPlaneSize = new Vector3(18250f, 18250f,18250f);
    public float heightAboveMap = 50f;
    public Color previouslyRevealedColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    public Color fogColor = new Color(0.05f, 0.05f, 0.05f, 0f);
    [Header("Grid Settings")]
    [Min(0.1f)] public float gridCellSize = 10f;
    public List<Entity> revelers = new List<Entity>();
    private List<Entity> _nonRevelers = new List<Entity>();
    public List<Entity> nonRevelers {
    get {
        // Remove null and destroyed entities before returning
        _nonRevelers.RemoveAll(e => e == null || e.Equals(null));
        return _nonRevelers;
    }
    private set => _nonRevelers = value;
}
    [Range(0,10000f)]
    public float fogRevealRadius = 150f;
    [Header("Compute Shader")]
    public ComputeShader fogComputeShader;
    [SerializeField] private float updateInterval = 0.1f;
    [SerializeField] private float visibilityCheckInterval = 0.2f;
    
    private ComputeBuffer gridBuffer;
    private ComputeBuffer entitiesBuffer;
    private ComputeBuffer visitedGridBuffer;
    private ComputeBuffer nonRevelersBuffer;
    private ComputeBuffer visibilityResultsBuffer;
    private RenderTexture fogRenderTexture;
    private int clearKernel;
    private int revealKernel;
    private int updateKernel;
    private int visibilityKernel;
    

    private GameObject fogPlane;
    private Vector2 gridOrigin;
    private int gridWidth;
    private int gridHeight;
    private float updateTimer;
    private float visibilityCheckTimer;
    // private bool[] visibilityResults; // Original code had this, assuming it's managed or unused as intended
    // private PlayerSide lastPlayerSide; // Original code had this, assuming it's managed or unused as intended

    struct EntityComputeData
    {
        public Vector3 position;
        public float radius;
    }
    public static FogWarMgr inst;

    // Added for FOW toggling logic
    private bool isInitialized = false;
    private bool lastFOWState;

    private void Awake()
    {
        inst = this;
        lastFOWState = FOW; // Initialize with the value set in Inspector or by default

        if (FOW)
        {
            PerformInitialization();
        }
        // If FOW is false initially, PerformInitialization is not called,
        // and fogPlane (being private) will be null.
    }
    
    void PerformInitialization()
    {
        if (isInitialized) return;

        InitializeFogPlane(); 
        // InitializeFogPlane creates the fogPlane GameObject. If it fails, fogPlane might be null.
        if (fogPlane == null) {
             Debug.LogError("FogWarMgr: FogPlane is null after InitializeFogPlane. Cannot initialize FOW.");
             return; // Do not set isInitialized = true if essential components are missing
        }
        
        InitializeGrid(); 
        InitializeComputeResources(); 
        
        isInitialized = true; 
        
        // Ensure the fog plane is active since FOW is being enabled/is enabled.
        // InitializeFogPlane creates an active GameObject by default, but this is an explicit confirmation.
        if (fogPlane != null)
        {
            fogPlane.SetActive(true);
        }
    }

    void Start() {
        // Original Start method is empty
    }

    void Update()
    {

        if (ReplayMgr.inst != null && ReplayMgr.inst.isReplaying)
        {
            FOW = false;
        }
        else
        {
            FOW = true;
        }
        // Handle runtime changes of the FOW flag
        if (FOW != lastFOWState)
        {

            if (FOW) // FOW has been turned ON
            {
                if (!isInitialized)
                {
                    PerformInitialization(); // This will also attempt to activate the fogPlane
                }
                else if (fogPlane != null) // Already initialized, just ensure plane is active
                {
                    fogPlane.SetActive(true);
                }
            }
            else // FOW has been turned OFF
            {
                if (fogPlane != null) // If plane exists, deactivate it
                {
                    fogPlane.SetActive(false);
                }
                // When FOW is turned off, make all entities visible
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
            lastFOWState = FOW; // Update the stored state
        }

        // If Fog of War is disabled, do nothing further in Update.
        if (!FOW)
        {
            // Defensive check: if FOW is false, ensure plane is off.
            // This handles cases where fogPlane might be activated by other means
            // while FOW is false, or if it was pre-assigned and active.
            if (isInitialized && fogPlane != null && fogPlane.activeSelf)
            {
                fogPlane.SetActive(false);
            }
            
            // Ensure all entities are visible if FOW is off
            // This handles the case where FOW is initially false or turned off during runtime
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

        // If FOW is enabled, but the system isn't initialized.
        if (!isInitialized)
        {
            // This could happen if FOW was true at start but PerformInitialization failed,
            // or if FOW was toggled true and PerformInitialization failed.
            // Attempt initialization again.
            PerformInitialization();
            if (!isInitialized) // If still not initialized after attempt, abort.
            {
                // Debug.LogWarning("FogWarMgr: FOW is active but system failed to initialize. Fog updates skipped.");
                return;
            }
        }
        
        // At this point, FOW is true and isInitialized should be true.
        // The fogPlane should be active. This is a defensive check/correction.
        if (fogPlane != null && !fogPlane.activeSelf)
        {
            // Debug.LogWarning("FogWarMgr: FOW is active and initialized, but fogPlane was inactive. Reactivating.");
            fogPlane.SetActive(true);
        }

        // --- Original Update logic for when FOW is active and initialized ---
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

    void InitializeFogPlane()
    {
        if (fogPlane == null)
        {
            fogPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            fogPlane.name = "FogOfWarPlane";
            Destroy(fogPlane.GetComponent<MeshCollider>());
        }

        float scaleX = fogPlaneSize.x / 10f;
        float scaleZ = fogPlaneSize.y / 10f;
        fogPlane.transform.localScale = new Vector3(scaleX, 1f, scaleZ);
        fogPlane.transform.position = new Vector3(0, heightAboveMap, 0);
        fogPlane.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        if (fogMaterial != null)
        {
            Renderer planeRenderer = fogPlane.GetComponent<Renderer>();
            planeRenderer.material = fogMaterial;
        }
    }

    void InitializeGrid()
    {
        gridWidth = Mathf.Max(1, Mathf.CeilToInt(fogPlaneSize.x / gridCellSize));
        gridHeight = Mathf.Max(1, Mathf.CeilToInt(fogPlaneSize.y / gridCellSize));
        gridOrigin = new Vector2(
            fogPlane.transform.position.x - fogPlaneSize.x / 2,
            fogPlane.transform.position.z - fogPlaneSize.y / 2
        );
    }

    void InitializeComputeResources()
    {
        gridBuffer = new ComputeBuffer(gridWidth * gridHeight, sizeof(float));
        visitedGridBuffer = new ComputeBuffer(gridWidth * gridHeight, sizeof(float));
        // Ensure nonRevelersBuffer is initialized with a minimum size if nonRevelers can be empty initially
        // Or handle its creation/recreation in UpdateNonRevealerVisibility more dynamically.
        // Original code initializes with size 1, which might be okay if shader handles 0 entities.
        int initialNonRevelerBufferSize = Mathf.Max(1, nonRevelers.Count > 0 ? nonRevelers.Count : 1);
        nonRevelersBuffer = new ComputeBuffer(initialNonRevelerBufferSize, Marshal.SizeOf<EntityComputeData>());
        visibilityResultsBuffer = new ComputeBuffer(initialNonRevelerBufferSize, sizeof(uint));


        float[] gridData = new float[gridWidth * gridHeight];
        gridBuffer.SetData(gridData);
        visitedGridBuffer.SetData(gridData);

        fogRenderTexture = new RenderTexture(gridWidth, gridHeight, 0, RenderTextureFormat.ARGB32)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Point
        };
        fogRenderTexture.Create();

        if (fogMaterial != null) // Check fogMaterial before using it
        {
            fogMaterial.SetTexture("_FogTex", fogRenderTexture);
        }


        clearKernel = fogComputeShader.FindKernel("ClearGrid");
        revealKernel = fogComputeShader.FindKernel("RevealAreas");
        updateKernel = fogComputeShader.FindKernel("ApplyToTexture");
        visibilityKernel = fogComputeShader.FindKernel("CalculateVisibility");

        fogComputeShader.SetBuffer(clearKernel, "Grid", gridBuffer);
        fogComputeShader.SetBuffer(revealKernel, "Grid", gridBuffer);
        fogComputeShader.SetBuffer(updateKernel, "Grid", gridBuffer);
        fogComputeShader.SetBuffer(revealKernel, "VisitedGrid", visitedGridBuffer);
        fogComputeShader.SetBuffer(updateKernel, "VisitedGrid", visitedGridBuffer);
        fogComputeShader.SetBuffer(visibilityKernel, "Grid", gridBuffer);
        fogComputeShader.SetBuffer(visibilityKernel, "NonRevelers", nonRevelersBuffer);
        fogComputeShader.SetBuffer(visibilityKernel, "VisibilityResults", visibilityResultsBuffer);

        fogComputeShader.SetVector("FogColor", new Color(0.05f, 0.05f, 0.05f, 0.5f));
        fogComputeShader.SetVector("PreviouslyRevealedColor", previouslyRevealedColor);
    }

    void UpdateFog()
    {
        revelers = EntityMgr.inst.entities.FindAll(entity => 
            entity != null && 
            entity.owner != null && 
            entity.gameObject.activeInHierarchy &&
            playerSide.Contains(entity.owner.playerSide) && 
            entity.entityClass != EntityClass.Missile);

        // _nonRevelers is updated directly here, bypassing the property setter's null check temporarily.
        // The getter will clean it up when accessed.
        _nonRevelers = EntityMgr.inst.entities.FindAll(entity => 
            entity != null && 
            entity.owner != null && 
            entity.gameObject.activeInHierarchy &&
            !playerSide.Contains(entity.owner.playerSide) && 
            entity.entityClass != EntityClass.Missile);
        

        if (revelers.Count == 0)
        {
            // If there are no revealers, we still need to clear the grid and apply to texture
            // to show full fog or previously revealed areas.
            // The original code returns here, which might leave stale fog if all revealers disappear.
            // Consider clearing and updating texture even with 0 revealers.
            // For now, keeping original behavior:
             return;
        }

        fogComputeShader.SetInt("EntitiesCount", revelers.Count);
        fogComputeShader.SetInt("GridWidth", gridWidth);
        fogComputeShader.SetInt("GridHeight", gridHeight);
        fogComputeShader.SetFloat("GridCellSize", gridCellSize);
        fogComputeShader.SetVector("GridOrigin", gridOrigin);

        DispatchCompute(clearKernel);
        UpdateEntityBuffer(); // This updates entitiesBuffer based on revelers list
        if (entitiesBuffer == null) return; // entitiesBuffer might be null if revelers.Count was 0 and UpdateEntityBuffer bailed.
                                            // However, UpdateEntityBuffer creates it if revelers.Count > 0.
                                            // If revelers.Count is 0, we returned above.

        fogComputeShader.SetBuffer(revealKernel, "Entities", entitiesBuffer);
        DispatchCompute(revealKernel);
        fogComputeShader.SetTexture(updateKernel, "FogTexture", fogRenderTexture);
        DispatchCompute(updateKernel);
    }

    private HashSet<Entity> revealedRigBalders = new HashSet<Entity>();
    private void UpdateRevealerVisibility()
    {
        foreach (Entity reveler in revelers)
        {
            if (reveler == null || reveler.transform == null) continue;
            reveler.isVisible = true; // Revelers are always visible to their side
        }
    }

    private void UpdateNonRevealerVisibility()
    {
        // Use the property to get a cleaned list
        List<Entity> currentNonRevelers = this.nonRevelers;

        if (currentNonRevelers.Count == 0)
        {
            // If there are no non-revelers to check, ensure any previously processed entities
            // that might now be revelers or destroyed are handled.
            // For simplicity, if no non-revelers, nothing to make visible/invisible via this path.
            return;
        }

        EntityComputeData[] nonRevealerData = new EntityComputeData[currentNonRevelers.Count];
        for (int i = 0; i < currentNonRevelers.Count; i++)
        {
            Entity entity = currentNonRevelers[i];
            // Entity list from property is already null-checked.
            // if (entity == null || entity.transform == null) continue; // Redundant due to property getter
            
            Vector3 pos = entity.transform.position;
            nonRevealerData[i] = new EntityComputeData
            {
                position = pos,
                radius = 10f // This radius seems fixed, ensure it's intended.
            };
        }

        // Resize buffers if necessary
        if (nonRevelersBuffer == null || nonRevelersBuffer.count < currentNonRevelers.Count)
        {
            nonRevelersBuffer?.Release();
            nonRevelersBuffer = new ComputeBuffer(currentNonRevelers.Count, Marshal.SizeOf<EntityComputeData>());
            
            visibilityResultsBuffer?.Release();
            visibilityResultsBuffer = new ComputeBuffer(currentNonRevelers.Count, sizeof(uint));

            // Rebind to kernel if buffers were recreated
            fogComputeShader.SetBuffer(visibilityKernel, "NonRevelers", nonRevelersBuffer);
            fogComputeShader.SetBuffer(visibilityKernel, "VisibilityResults", visibilityResultsBuffer);
        }
        
        nonRevelersBuffer.SetData(nonRevealerData, 0, 0, currentNonRevelers.Count);


        fogComputeShader.SetInt("NonRevealerCount", currentNonRevelers.Count);
        // Grid parameters should already be set by UpdateFog or PerformInitialization
        // fogComputeShader.SetFloat("GridCellSize", gridCellSize); 
        // fogComputeShader.SetVector("GridOrigin", gridOrigin);

        fogComputeShader.GetKernelThreadGroupSizes(visibilityKernel, out uint threadGroupSize, out _, out _);
        int groups = Mathf.CeilToInt(currentNonRevelers.Count / (float)threadGroupSize);
        if (groups > 0) // Only dispatch if there are entities to process
        {
            fogComputeShader.Dispatch(visibilityKernel, groups, 1, 1);

            uint[] results = new uint[currentNonRevelers.Count];
            visibilityResultsBuffer.GetData(results, 0, 0, currentNonRevelers.Count);

            for (int i = 0; i < currentNonRevelers.Count; i++)
            {
                Entity entity = currentNonRevelers[i];
                // if (entity == null) continue; // Should be handled by property getter

                bool isVisible = results[i] != 0;

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
    }

    void DispatchCompute(int kernel)
    {
        if (!isInitialized || fogComputeShader == null) return; // Guard against calls if not ready
        fogComputeShader.GetKernelThreadGroupSizes(kernel, out uint x, out uint y, out _);
        int groupsX = Mathf.CeilToInt(gridWidth / (float)x);
        int groupsY = Mathf.CeilToInt(gridHeight / (float)y);
        if (groupsX > 0 && groupsY > 0) // Ensure groups are valid
        {
            fogComputeShader.Dispatch(kernel, groupsX, groupsY, 1);
        }
    }
    public void CleanupEntities()
    {
        revelers.RemoveAll(e => e == null || e.Equals(null));
        _nonRevelers.RemoveAll(e => e == null || e.Equals(null));
    }
    void UpdateEntityBuffer()
    {
        if (revelers.Count == 0)
        {
            // If entitiesBuffer exists, we might want to clear it or handle it.
            // For now, if no revelers, perhaps no buffer update is needed or it should be sized to 1 with dummy data.
            // Or, ensure entitiesBuffer is released if not used.
            // Releasing and re-creating buffers frequently can be a performance hit.
            // Consider resizing or having a max-sized buffer.
            // For simplicity, matching original implication: if no revelers, buffer might not be needed for revealKernel.
            // However, revealKernel expects "Entities" buffer.
            // Let's ensure it's always valid if revealKernel is dispatched.
            // The UpdateFog method returns if revelers.Count == 0 before calling this.
            return;
        }

        EntityComputeData[] entityData = new EntityComputeData[revelers.Count];
        for (int i = 0; i < revelers.Count; i++)
        {
            // revelers list should be clean from UpdateFog
            Vector3 pos = revelers[i].transform.position;
            entityData[i] = new EntityComputeData
            {
                position = pos,
                radius = Mathf.Max(revelers[i].length, fogRevealRadius)
            };
        }

        if (entitiesBuffer == null || entitiesBuffer.count < revelers.Count)
        {
            entitiesBuffer?.Release();
            entitiesBuffer = new ComputeBuffer(revelers.Count, Marshal.SizeOf<EntityComputeData>());
            // If buffer is recreated, it needs to be set on the kernel again if not already handled
            // fogComputeShader.SetBuffer(revealKernel, "Entities", entitiesBuffer); // This is done in UpdateFog
        }
        entitiesBuffer.SetData(entityData);
    }

    void CleanupComputeResources()
    {
        gridBuffer?.Release();
        entitiesBuffer?.Release();
        visitedGridBuffer?.Release();
        nonRevelersBuffer?.Release();
        visibilityResultsBuffer?.Release();
        if (fogRenderTexture != null) // Check if it's not null before releasing
        {
            if (fogRenderTexture.IsCreated()) // Check if it's created before releasing
            {
                fogRenderTexture.Release();
            }
            fogRenderTexture = null; // Set to null after release
        }
    }
    public void ResetFog()
    {
        if (!FOW || !isInitialized) return; // Also check if initialized

        float[] gridData = new float[gridWidth * gridHeight];
        if (gridBuffer != null && gridBuffer.IsValid()) gridBuffer.SetData(gridData);
        if (visitedGridBuffer != null && visitedGridBuffer.IsValid()) visitedGridBuffer.SetData(gridData);
        
        // Update the fog texture to show fully obscured areas
        if (fogComputeShader != null && fogRenderTexture != null && fogRenderTexture.IsCreated())
        {
             fogComputeShader.SetTexture(updateKernel, "FogTexture", fogRenderTexture);
             DispatchCompute(updateKernel);
        }


        revealedRigBalders.Clear();
        UpdateNonRevealerVisibility(); // Force update visibility
    }
    void OnDisable()
    {
        // Clearing lists here might be okay, but consider if the component is just disabled temporarily.
        // If re-enabled, it might need to repopulate these lists.
        // revelers.Clear();
        // _nonRevelers.Clear(); // Access private field directly for clearing
    }

    void OnDestroy()
    {
        CleanupComputeResources();
        // Lists will be garbage collected with the object if not cleared,
        // but explicit clearing can help if there are external references or for clarity.
        revelers.Clear();
        _nonRevelers.Clear();
    }
}

