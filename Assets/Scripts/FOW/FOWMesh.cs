using UnityEngine;

public class FogOfWarMesh : MonoBehaviour
{
    public int gridSize = 128; // Lower resolution for WebGL performance
    public float mapSize = 18250f;
    public float revealRadius = 500f; // Example vision radius
    public float fadeSpeed = 2f; // Smooth transition speed
    public float fogHeight = 100f; // The Y-level of the fog mesh
    private Color revealedColor = new Color(0f, 0f, 0f, 0.7f); // Grey with 50% opacity for revealed but not visible

    private float[,] fogGrid; // Current visibility (0 = not visible, 1 = visible)
    private bool[,] exploredGrid; // Tracks if cell was ever visible
    private Mesh mesh;
    private Vector3[] vertices;
    private Color[] colors;
    private int[] triangles;
    private float cellSize;
    private bool[] dirtyCells;

    void Start()
    {
        cellSize = mapSize / gridSize;
        fogGrid = new float[gridSize, gridSize];
        exploredGrid = new bool[gridSize, gridSize];
        dirtyCells = new bool[gridSize * gridSize];
        InitializeMesh();
        // LoadFogState(); // Optionally load saved state
    }

    void InitializeMesh()
    {
        mesh = new Mesh();
        mesh.MarkDynamic(); // Optimize for dynamic updates
        GetComponent<MeshFilter>().mesh = mesh;

        // Initialize vertices and colors
        int vertexCount = (gridSize + 1) * (gridSize + 1);
        vertices = new Vector3[vertexCount];
        colors = new Color[vertexCount];
        triangles = new int[gridSize * gridSize * 6];

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
    }

    public void RevealArea(Vector3 worldPos, float radius)
    {
        float offset = mapSize / 2f;
        int centerX = Mathf.FloorToInt((worldPos.x + offset) / cellSize);
        int centerZ = Mathf.FloorToInt((worldPos.z + offset) / cellSize);
        int radiusCells = Mathf.CeilToInt(radius / cellSize);

        // Mark cells as visible and explored
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
                        if (!exploredGrid[x, z])
                        {
                            exploredGrid[x, z] = true; // Mark as explored
                        }
                        dirtyCells[x + z * gridSize] = true;
                    }
                }
            }
        }
    }

    void Update()
    {
        bool needsUpdate = false;
        // First, reset all cells that were previously visible but are no longer
        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                if (fogGrid[x, z] > 0)
                {
                    dirtyCells[x + z * gridSize] = true;
                }
            }
        }

        // Then, fade out visibility for all dirty cells
        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                if (dirtyCells[x + z * gridSize])
                {
                    needsUpdate = true;
                    // Fade out visibility
                    fogGrid[x, z] = Mathf.Lerp(fogGrid[x, z], 0f, Time.deltaTime * fadeSpeed);
                    if (Mathf.Abs(fogGrid[x, z]) < 0.01f)
                    {
                        fogGrid[x, z] = 0f; // Snap to 0 for stability
                        dirtyCells[x + z * gridSize] = false; // Stop updating this cell once it's faded
                    }
                }
            }
        }

        if (needsUpdate)
        {
            UpdateMeshColors();
        }
    }

    void UpdateMeshColors()
    {
        // Update vertex colors
        for (int x = 0; x <= gridSize; x++)
        {
            for (int z = 0; z <= gridSize; z++)
            {
                int index = x + z * (gridSize + 1);
                float visibility = 0f;
                bool isExplored = false;
                int count = 0;

                // Average visibility and check explored state from adjacent cells
                if (x < gridSize && z < gridSize) { visibility += fogGrid[x, z]; isExplored |= exploredGrid[x, z]; count++; }
                if (x > 0 && z < gridSize) { visibility += fogGrid[x - 1, z]; isExplored |= exploredGrid[x - 1, z]; count++; }
                if (z > 0 && x < gridSize) { visibility += fogGrid[x, z - 1]; isExplored |= exploredGrid[x, z - 1]; count++; }
                if (x > 0 && z > 0) { visibility += fogGrid[x - 1, z - 1]; isExplored |= exploredGrid[x - 1, z - 1]; count++; }
                
                if (count > 0)
                {
                    visibility /= count;
                }

                // Set color based on state
                if (visibility > 0.01f)
                    colors[index] = new Color(0, 0, 0, 1 - visibility); // Visible: fade to transparent
                else if (isExplored)
                    colors[index] = revealedColor; // Revealed but not visible: grey
                else
                    colors[index] = Color.black; // Hidden: fully fogged
            }
        }
        mesh.colors = colors;
    }

    // Helper classes for serialization of 2D arrays
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

    // Save fog state for persistence
    public void SaveFogState()
    {
        FogData fogData = new FogData { fog = new float[gridSize * gridSize] };
        ExploredData exploredData = new ExploredData { explored = new bool[gridSize * gridSize] };

        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                int index = x + z * gridSize;
                fogData.fog[index] = fogGrid[x, z];
                exploredData.explored[index] = exploredGrid[x, z];
            }
        }

        string jsonFog = JsonUtility.ToJson(fogData);
        string jsonExplored = JsonUtility.ToJson(exploredData);
        PlayerPrefs.SetString("FogOfWarState", jsonFog);
        PlayerPrefs.SetString("ExploredState", jsonExplored);
        PlayerPrefs.Save();
    }

    // Load fog state
    void LoadFogState()
    {
        if (PlayerPrefs.HasKey("FogOfWarState") && PlayerPrefs.HasKey("ExploredState"))
        {
            string jsonFog = PlayerPrefs.GetString("FogOfWarState");
            string jsonExplored = PlayerPrefs.GetString("ExploredState");

            FogData fogData = JsonUtility.FromJson<FogData>(jsonFog);
            ExploredData exploredData = JsonUtility.FromJson<ExploredData>(jsonExplored);

            if (fogData != null && exploredData != null && fogData.fog.Length == gridSize * gridSize && exploredData.explored.Length == gridSize * gridSize)
            {
                for (int x = 0; x < gridSize; x++)
                {
                    for (int z = 0; z < gridSize; z++)
                    {
                        int index = x + z * gridSize;
                        fogGrid[x, z] = fogData.fog[index];
                        exploredGrid[x, z] = exploredData.explored[index];
                    }
                }
                // Force a full mesh update after loading
                UpdateMeshColors();
            }
        }
    }
}