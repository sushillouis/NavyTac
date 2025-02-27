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
    public List<Entity> nonRevelers = new List<Entity>();
    [Range(0,1000f)]
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
    private bool[] visibilityResults;
    private PlayerSide lastPlayerSide;

    struct EntityComputeData
    {
        public Vector3 position;
        public float radius;
    }
    public static FogWarMgr inst;
    private void Awake() {
        inst = this;
    }
    void Start() {
        if (!FOW) return;
        InitializeFogPlane();
        InitializeGrid();
        InitializeComputeResources();
        // lastPlayerSide = playerSide;
    }

    // void OnValidate()
    // {
    //     if (!FOW)
    //     {
    //         CleanupComputeResources();
            
    //         revelers.Clear();
    //         foreach (Entity entity in nonRevelers)
    //         {
    //             if (entity != null)
    //                 entity.gameObject.SetActive(true);
    //         }
    //         nonRevelers.Clear();
    //     }
    //     else
    //     {
    //         CleanupComputeResources();
            
    //         // InitializeFogPlane();
    //         InitializeGrid();
    //         InitializeComputeResources();
    //     }
    //     if (playerSide != lastPlayerSide)
    //     {
    //         lastPlayerSide = playerSide;
    //         CleanupComputeResources();

    //         revelers.Clear();
    //         foreach (Entity entity in nonRevelers)
    //         {
    //             if (entity != null)
    //                 entity.gameObject.SetActive(true);
    //         }
    //         nonRevelers.Clear();
    //         // InitializeFogPlane();
    //         InitializeGrid();
    //         InitializeComputeResources();
    //     } 
    // }
    void Update()
    {
        if (!FOW) return;
        updateTimer -= Time.deltaTime;
        if (updateTimer <= 0)
        {
            UpdateFog();
            updateTimer = updateInterval;
        }
        
        visibilityCheckTimer -= Time.deltaTime;
        if (visibilityCheckTimer <= 0)
        {
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
        nonRevelersBuffer = new ComputeBuffer(1, Marshal.SizeOf<EntityComputeData>());
        visibilityResultsBuffer = new ComputeBuffer(1, sizeof(uint));

        float[] gridData = new float[gridWidth * gridHeight];
        gridBuffer.SetData(gridData);
        visitedGridBuffer.SetData(gridData);

        fogRenderTexture = new RenderTexture(gridWidth, gridHeight, 0, RenderTextureFormat.ARGB32)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Point
        };
        fogRenderTexture.Create();

        fogMaterial.SetTexture("_FogTex", fogRenderTexture);

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
            playerSide.Contains(entity.owner.playerSide));

        nonRevelers = EntityMgr.inst.entities.FindAll(entity => 
            entity != null && 
            entity.owner != null && 
            !playerSide.Contains(entity.owner.playerSide));
        

        if (revelers.Count == 0) return;
        
        fogComputeShader.SetInt("GridWidth", gridWidth);
        fogComputeShader.SetInt("GridHeight", gridHeight);
        fogComputeShader.SetFloat("GridCellSize", gridCellSize);
        fogComputeShader.SetVector("GridOrigin", gridOrigin);

        DispatchCompute(clearKernel);
        UpdateEntityBuffer();
        fogComputeShader.SetBuffer(revealKernel, "Entities", entitiesBuffer);
        DispatchCompute(revealKernel);
        fogComputeShader.SetTexture(updateKernel, "FogTexture", fogRenderTexture);
        DispatchCompute(updateKernel);
    }

    private void UpdateNonRevealerVisibility()
    {
        // Filter out null entities to prevent processing invalid entries
        
        if (nonRevelers.Count == 0) return;

        EntityComputeData[] nonRevealerData = new EntityComputeData[nonRevelers.Count];
        for (int i = 0; i < nonRevelers.Count; i++)
        {
            Entity entity = nonRevelers[i];
            Vector3 pos = entity.transform.position;
            nonRevealerData[i] = new EntityComputeData
            {
                position = pos,
                radius = 10f
            };
        }

        // Release old buffers and create new ones with correct size
        nonRevelersBuffer?.Release();
        nonRevelersBuffer = new ComputeBuffer(nonRevelers.Count, Marshal.SizeOf<EntityComputeData>());
        nonRevelersBuffer.SetData(nonRevealerData);

        visibilityResultsBuffer?.Release();
        visibilityResultsBuffer = new ComputeBuffer(nonRevelers.Count, sizeof(uint));

        // Re-bind buffers to compute shader kernel
        fogComputeShader.SetBuffer(visibilityKernel, "NonRevelers", nonRevelersBuffer);
        fogComputeShader.SetBuffer(visibilityKernel, "VisibilityResults", visibilityResultsBuffer);

        // Update shader parameters
        fogComputeShader.SetInt("NonRevealerCount", nonRevelers.Count);
        fogComputeShader.SetFloat("GridCellSize", gridCellSize);
        fogComputeShader.SetVector("GridOrigin", gridOrigin);

        // Calculate dispatch groups
        fogComputeShader.GetKernelThreadGroupSizes(visibilityKernel, out uint threadGroupSize, out _, out _);
        int groups = Mathf.CeilToInt(nonRevelers.Count / (float)threadGroupSize);
        fogComputeShader.Dispatch(visibilityKernel, groups, 1, 1);

        // Retrieve and apply results
        uint[] results = new uint[nonRevelers.Count];
        visibilityResultsBuffer.GetData(results);

        for (int i = 0; i < nonRevelers.Count; i++)
        {
            bool isVisible = results[i] != 0;
            nonRevelers[i].transform.GetChild(0).gameObject.SetActive(isVisible);
            if(nonRevelers[i].entityType == EntityType.Rig_Balder) {
                nonRevelers[i].transform.gameObject.SetActive(isVisible);
            }
        }
    }

    void DispatchCompute(int kernel)
    {
        fogComputeShader.GetKernelThreadGroupSizes(kernel, out uint x, out uint y, out _);
        int groupsX = Mathf.CeilToInt(gridWidth / (float)x);
        int groupsY = Mathf.CeilToInt(gridHeight / (float)y);
        fogComputeShader.Dispatch(kernel, groupsX, groupsY, 1);
    }

    void UpdateEntityBuffer()
    {
        EntityComputeData[] entityData = new EntityComputeData[revelers.Count];
        for (int i = 0; i < revelers.Count; i++)
        {
            Vector3 pos = revelers[i].transform.position;
            entityData[i] = new EntityComputeData
            {
                position = pos,
                radius = Mathf.Max(revelers[i].length, fogRevealRadius)
            };
        }

        entitiesBuffer?.Release();
        entitiesBuffer = new ComputeBuffer(revelers.Count, Marshal.SizeOf<EntityComputeData>());
        entitiesBuffer.SetData(entityData);
    }

    void CleanupComputeResources()
    {
        gridBuffer?.Release();
        entitiesBuffer?.Release();
        visitedGridBuffer?.Release();
        nonRevelersBuffer?.Release();
        visibilityResultsBuffer?.Release();
        if (fogRenderTexture != null && fogRenderTexture.IsCreated())
            fogRenderTexture.Release();
    }

    void OnDestroy()
    {
        CleanupComputeResources();
    }
}