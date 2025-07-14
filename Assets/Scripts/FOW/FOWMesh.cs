using UnityEngine;

public class FogOfWarMesh : MonoBehaviour
{
    public int gridSize = 128; // Lower resolution for WebGL performance
    public float mapSize = 18250f;
    public float revealRadius = 500f; // Example vision radius
    public float fadeSpeed = 2f; // Smooth transition speed
    public float fogHeight = 100f; // The Y-level of the fog mesh

    private float[,] fogGrid;
    private Mesh mesh;
    private Vector3[] vertices;
    private Color[] colors;
    private int[] triangles;
    private float cellSize;
    private bool[] dirtyCells;

    void Start()
    {
        Debug.Log("FogOfWarMesh: Start() called. Initializing...");
        cellSize = mapSize / gridSize;
        fogGrid = new float[gridSize, gridSize];
        dirtyCells = new bool[gridSize * gridSize];
        InitializeMesh();
        Debug.Log("FogOfWarMesh: Initialization complete.");
    }

    void InitializeMesh()
    {
        Debug.Log("FogOfWarMesh: InitializeMesh() called.");
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        // Initialize vertices and colors
        int vertexCount = (gridSize + 1) * (gridSize + 1);
        vertices = new Vector3[vertexCount];
        colors = new Color[vertexCount];
        triangles = new int[gridSize * gridSize * 6];

        Debug.Log($"FogOfWarMesh: Grid Size: {gridSize}, Map Size: {mapSize}, Cell Size: {cellSize}");
        Debug.Log($"FogOfWarMesh: Vertex Count: {vertexCount}, Triangle Count: {triangles.Length}");

        float offset = mapSize / 2f;

        // Generate vertices on the XZ plane, centered at the origin
        for (int x = 0; x <= gridSize; x++)
        {
            for (int z = 0; z <= gridSize; z++)
            {
                int index = x + z * (gridSize + 1);
                vertices[index] = new Vector3(x * cellSize - offset, fogHeight, z * cellSize - offset);
                colors[index] = Color.black; // Fully fogged (alpha = 1)
            }
        }

        // Generate triangles
        int triIndex = 0;
        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                int v0 = x + z * (gridSize + 1);
                int v1 = v0 + 1;
                int v2 = v0 + (gridSize + 1);
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
        Debug.Log("FogOfWarMesh: Mesh created and assigned.");
    }

    public void RevealArea(Vector3 worldPos, float radius)
    {
        Debug.Log($"FogOfWarMesh: RevealArea called at World Pos: {worldPos} with Radius: {radius}");
        float offset = mapSize / 2f;
        int centerX = Mathf.FloorToInt((worldPos.x + offset) / cellSize);
        int centerZ = Mathf.FloorToInt((worldPos.z + offset) / cellSize);
        int radiusCells = Mathf.CeilToInt(radius / cellSize);
        Debug.Log($"FogOfWarMesh: Calculated Center Cell: ({centerX}, {centerZ}), Radius in Cells: {radiusCells}");

        int revealedCells = 0;
        // Mark affected cells as dirty
        for (int x = centerX - radiusCells; x <= centerX + radiusCells; x++)
        {
            for (int z = centerZ - radiusCells; z <= centerZ + radiusCells; z++)
            {
                if (x >= 0 && x < gridSize && z >= 0 && z < gridSize)
                {
                    float dist = Vector2.Distance(new Vector2(centerX, centerZ), new Vector2(x, z)) * cellSize;
                    if (dist <= radius)
                    {
                        fogGrid[x, z] = 1f; // Fully visible
                        dirtyCells[x + z * gridSize] = true;
                        revealedCells++;
                    }
                }
            }
        }
        if (revealedCells > 0)
        {
            Debug.Log($"FogOfWarMesh: Marked {revealedCells} cells as dirty for reveal.");
        }
    }

    void Update()
    {
        // Smoothly update visibility
        bool needsUpdate = false;
        int dirtyCellCount = 0;
        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                if (dirtyCells[x + z * gridSize])
                {
                    needsUpdate = true;
                    dirtyCellCount++;
                    // Interpolate for smooth transitions
                    fogGrid[x, z] = Mathf.Lerp(fogGrid[x, z], 1f, Time.deltaTime * fadeSpeed);
                    if (Mathf.Abs(fogGrid[x, z] - 1f) < 0.01f)
                        dirtyCells[x + z * gridSize] = false;
                }
            }
        }

        if (needsUpdate)
        {
            // This log can be spammy. It's useful for checking if updates are happening.
            // You might want to remove it after confirming it works.
            // Debug.Log($"FogOfWarMesh: Updating mesh with {dirtyCellCount} dirty cells.");

            // Update vertex colors
            for (int x = 0; x <= gridSize; x++)
            {
                for (int z = 0; z <= gridSize; z++)
                {
                    int index = x + z * (gridSize + 1);
                    float visibility = 0f;
                    // Average visibility from adjacent cells
                    int count = 0;
                    if (x < gridSize && z < gridSize) { visibility += fogGrid[x, z]; count++; }
                    if (x > 0 && z < gridSize) { visibility += fogGrid[x - 1, z]; count++; }
                    if (z > 0 && x < gridSize) { visibility += fogGrid[x, z - 1]; count++; }
                    if (x > 0 && z > 0) { visibility += fogGrid[x - 1, z - 1]; count++; }
                    visibility = count > 0 ? visibility / count : 0f;
                    colors[index] = new Color(0, 0, 0, 1 - visibility); // Alpha = 1 for fogged
                }
            }
            mesh.colors = colors;
        }
    }
}