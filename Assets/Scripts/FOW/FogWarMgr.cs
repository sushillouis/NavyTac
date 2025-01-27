using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class FogWarMgr : MonoBehaviour
{
    public PlayerSide playerSide;
    
    [Header("Fog Plane Settings")]
    public Material fogMaterial;
    public Vector2 fogPlaneSize = new Vector2(18250f, 18250f);
    public float heightAboveMap = 50f;

    [Header("Grid Settings")]
    [Min(0.1f)] public float gridCellSize = 10f;
    public List<Entity> revelers = new List<Entity>();
    public List<Entity> nonRevelers = new List<Entity>();
    
    [Header("Compute Shader")]
    public ComputeShader fogComputeShader;
    [SerializeField] private float updateInterval = 0.1f;

    private ComputeBuffer gridBuffer;
    private ComputeBuffer entitiesBuffer;
    private ComputeBuffer visitedGridBuffer;
    private RenderTexture fogRenderTexture;
    private int clearKernel;
    private int revealKernel;
    private int updateKernel;

    private GameObject fogPlane;
    private Vector2 gridOrigin;
    private int gridWidth;
    private int gridHeight;
    private float updateTimer;

    struct EntityComputeData
    {
        public Vector3 position;
        public float radius;
    }

    void Start() {
        InitializeFogPlane();
        InitializeGrid();
        InitializeComputeResources();
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
        fogPlane.transform.position = new Vector3(200f, heightAboveMap, -800f);
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
        visitedGridBuffer = new ComputeBuffer(gridWidth * gridHeight, sizeof(uint));

        float[] gridData = new float[gridWidth * gridHeight];
        uint[] visitedGridData = new uint[gridWidth * gridHeight];
        gridBuffer.SetData(gridData);
        visitedGridBuffer.SetData(visitedGridData);

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

        fogComputeShader.SetBuffer(clearKernel, "Grid", gridBuffer);
        fogComputeShader.SetBuffer(revealKernel, "Grid", gridBuffer);
        fogComputeShader.SetBuffer(updateKernel, "Grid", gridBuffer);
        
        fogComputeShader.SetBuffer(revealKernel, "VisitedGrid", visitedGridBuffer);
        fogComputeShader.SetBuffer(updateKernel, "VisitedGrid", visitedGridBuffer);

        fogComputeShader.SetVector("FogColor", new Color(0.05f, 0.05f, 0.05f, 0.8f));
        fogComputeShader.SetVector("PreviouslyRevealedColor", new Color(0.5f, 0.5f, 0.5f, 0.5f));
    }

    void UpdateFog()
    {
        revelers = EntityMgr.inst.entities.FindAll(entity => 
            entity != null && 
            entity.owner != null && 
            entity.owner.playerSide == playerSide);
        
        nonRevelers = EntityMgr.inst.entities.FindAll(entity => 
            entity != null && 
            entity.owner != null && 
            entity.owner.playerSide != playerSide);
        
        foreach (Entity entity in nonRevelers)
        {
            if(entity.gameObject.activeSelf == false) continue;
            entity.gameObject.SetActive(false);
        }

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
                radius = Mathf.Max(revelers[i].length, 150f)
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
        if (fogRenderTexture != null && fogRenderTexture.IsCreated())
            fogRenderTexture.Release();
    }

    void OnDestroy()
    {
        CleanupComputeResources();
    }
}