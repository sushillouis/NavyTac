using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class FogWarMgr : MonoBehaviour
{
    [Header("Fog Plane Settings")]
    public Material fogMaterial;
    [SerializeField] private Vector2 minFogPlaneSize = new Vector2(10f, 10f);
    
    [SerializeField] 
    private Vector2 _fogPlaneSize = new Vector2(1000f, 1000f);
    public Vector2 fogPlaneSize
    {
        get => _fogPlaneSize;
        set
        {
            if (_fogPlaneSize != value)
            {
                _fogPlaneSize = new Vector2(
                    Mathf.Max(value.x, minFogPlaneSize.x),
                    Mathf.Max(value.y, minFogPlaneSize.y)
                );
                RefreshFogSystem();
            }
        }
    }

    public float heightAboveMap = 50f;

    [Header("Grid Settings")]
    [Min(0.1f)] public float gridCellSize = 10f;
    public bool showGridGizmos = true;
    public List<Entity> revelers = new List<Entity>();

    [Header("Compute Shader")]
    public ComputeShader fogComputeShader;
    [SerializeField] private float updateInterval = 0.1f;

    // Compute resources
    private ComputeBuffer gridBuffer;
    private ComputeBuffer entitiesBuffer;
    private RenderTexture fogRenderTexture;
    private int clearKernel;
    private int revealKernel;
    private int updateKernel;

    private GameObject fogPlane;
    private Vector2 gridOrigin;
    private int gridWidth;
    public float visionRadius;
    private int gridHeight;
    private float updateTimer;

    struct EntityComputeData
    {
        public Vector3 position;
        public float radius;
    }

    void Start() {
        revelers = EntityMgr.inst.entities;
        InitializeFogSystem();
    }

    void OnValidate()
    {
        if (Application.isPlaying && fogPlane != null)
        {
            RefreshFogSystem();
        }
    }

    void Update()
    {
        updateTimer -= Time.deltaTime;
        if (updateTimer <= 0)
        {
            UpdateFog();
            updateTimer = updateInterval;
        }
    }

    void InitializeFogSystem()
    {
        InitializeFogPlane();
        InitializeGrid();
        InitializeComputeResources();
    }

    void RefreshFogSystem()
    {
        CleanupComputeResources();
        InitializeFogPlane();
        InitializeGrid();
        InitializeComputeResources();
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
        fogPlane.transform.position = new Vector3(0f, heightAboveMap, 0f);
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
        // Create compute buffers
        gridBuffer = new ComputeBuffer(gridWidth * gridHeight, sizeof(uint));
        
        // Create render texture
        fogRenderTexture = new RenderTexture(gridWidth, gridHeight, 0, RenderTextureFormat.ARGB32)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Point
        };
        fogRenderTexture.Create();

        // Set up material
        fogMaterial.SetTexture("_FogTex", fogRenderTexture);

        // Get kernel indices
        clearKernel = fogComputeShader.FindKernel("ClearGrid");
        revealKernel = fogComputeShader.FindKernel("RevealAreas");
        updateKernel = fogComputeShader.FindKernel("ApplyToTexture");
    }

    void UpdateFog()
{
    if (revelers.Count == 0) return;

    // Set parameters
    fogComputeShader.SetInt("GridWidth", gridWidth);
    fogComputeShader.SetInt("GridHeight", gridHeight);
    fogComputeShader.SetFloat("GridCellSize", gridCellSize);
    fogComputeShader.SetVector("GridOrigin", gridOrigin);
    fogComputeShader.SetVector("FogColor", Color.black);

    // Clear grid
    fogComputeShader.SetBuffer(clearKernel, "Grid", gridBuffer);
    DispatchCompute(clearKernel);

    // Reveal areas
    UpdateEntityBuffer();
    fogComputeShader.SetBuffer(revealKernel, "Grid", gridBuffer);
    fogComputeShader.SetBuffer(revealKernel, "Entities", entitiesBuffer);
    DispatchCompute(revealKernel);

    // Update texture
    fogComputeShader.SetTexture(updateKernel, "FogTexture", fogRenderTexture);
    fogComputeShader.SetBuffer(updateKernel, "Grid", gridBuffer);
    DispatchCompute(updateKernel);
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
        // Get FRESH position every update
        Vector3 pos = revelers[i].transform.position;
        entityData[i] = new EntityComputeData
        {
            position = pos,
            radius = revelers[i].length
        };
        
        // Debug log to verify movement
        Debug.Log($"Entity {i} Position: {pos}");
    }

    entitiesBuffer?.Release();
    entitiesBuffer = new ComputeBuffer(revelers.Count, Marshal.SizeOf<EntityComputeData>());
    entitiesBuffer.SetData(entityData);
    fogComputeShader.SetBuffer(revealKernel, "Entities", entitiesBuffer);
}

    // void DispatchKernel(int kernel, params ComputeBuffer[] buffers)
    // {
    //     foreach (var buffer in buffers)
    //     {
    //         fogComputeShader.SetBuffer(kernel, "Grid", buffer);
    //     }

    //     uint threadX, threadY, threadZ;
    //     fogComputeShader.GetKernelThreadGroupSizes(kernel, out threadX, out threadY, out threadZ);
        
    //     int groupsX = Mathf.CeilToInt(gridWidth / (float)threadX);
    //     int groupsY = Mathf.CeilToInt(gridHeight / (float)threadY);
        
    //     fogComputeShader.Dispatch(kernel, groupsX, groupsY, 1);
    // }

    void CleanupComputeResources()
    {
        gridBuffer?.Release();
        entitiesBuffer?.Release();
        if (fogRenderTexture != null && fogRenderTexture.IsCreated())
            fogRenderTexture.Release();
    }

    void OnDestroy()
    {
        CleanupComputeResources();
    }

    // Rest of existing methods (WorldToGridPosition, OnDrawGizmosSelected, etc.)
    public Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        float x = (worldPos.x - gridOrigin.x) / gridCellSize;
        float z = (worldPos.z - gridOrigin.y) / gridCellSize;

        return new Vector2Int(
            Mathf.Clamp(Mathf.FloorToInt(x), 0, gridWidth - 1),
            Mathf.Clamp(Mathf.FloorToInt(z), 0, gridHeight - 1)
        );
    }

    void OnDrawGizmosSelected()
    {
        if (!showGridGizmos || !Application.isPlaying) return;

        Gizmos.color = Color.cyan;
        Vector3 startPos = fogPlane.transform.position - new Vector3(fogPlaneSize.x/2, 0, fogPlaneSize.y/2);

        for (int x = 0; x <= gridWidth; x++)
        {
            Vector3 lineStart = startPos + new Vector3(x * gridCellSize, 0, 0);
            Vector3 lineEnd = lineStart + new Vector3(0, 0, gridHeight * gridCellSize);
            Gizmos.DrawLine(lineStart, lineEnd);
        }

        for (int z = 0; z <= gridHeight; z++)
        {
            Vector3 lineStart = startPos + new Vector3(0, 0, z * gridCellSize);
            Vector3 lineEnd = lineStart + new Vector3(gridWidth * gridCellSize, 0, 0);
            Gizmos.DrawLine(lineStart, lineEnd);
        }
    }

    public void SetFogVisibility(bool visible)
    {
        if (fogPlane != null) fogPlane.SetActive(visible);
    }
}