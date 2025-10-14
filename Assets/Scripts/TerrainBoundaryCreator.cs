using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
[AddComponentMenu("Terrain/Terrain Boundary Creator")]
public class TerrainBoundaryCreator : MonoBehaviour
{
    [FormerlySerializedAs("terrain")]
    [Tooltip("Optional: leave empty to process every active Terrain in the scene")]
    public Terrain targetTerrain;

    [Tooltip("Spacing between terrain samples in world units")]
    public float spacing = 5f;

    [Tooltip("Allowed deviation from y = 0 when deciding if a sample is on the boundary")]
    public float heightTolerance = 0.05f;

    [Tooltip("Remove previously generated boundary data (and any child markers) before creating new ones")]
    public bool clearExistingChildren = true;

    [SerializeField, HideInInspector]
    public List<Vector3> boundaryPositions = new List<Vector3>();

    public IReadOnlyList<Vector3> BoundaryPositions => boundaryPositions;

    [ContextMenu("Create Terrain Boundary (Selected Terrain)")]
    public void CreateBoundary()
    {
        Terrain resolvedTerrain = ResolveTerrain();
        if (resolvedTerrain == null)
        {
            Debug.LogError("Assign a Terrain or ensure there is an active Terrain in the scene.");
            return;
        }

        PrepareForGeneration();
        float dedupeScale = CalculateDedupeScale();
        HashSet<Vector3Int> dedupeSet = BuildDedupeSet(dedupeScale);

        int created = CreateBoundaryForTerrain(resolvedTerrain, dedupeSet, dedupeScale);
        Debug.Log($"Created {created} boundary markers for terrain '{resolvedTerrain.name}'.");

#if UNITY_EDITOR
        if (!EditorApplication.isPlaying)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        }
#endif
    }

    [ContextMenu("Create Terrain Boundary (All Terrains)")]
    public void CreateBoundaryForAllTerrains()
    {
        Terrain[] terrains = FindObjectsOfType<Terrain>();
        if (terrains.Length == 0)
        {
            Debug.LogWarning("No Terrain objects found in the scene.");
            return;
        }

        PrepareForGeneration();
        float dedupeScale = CalculateDedupeScale();
        HashSet<Vector3Int> dedupeSet = BuildDedupeSet(dedupeScale);

        int createdTotal = 0;
        int processedTerrains = 0;
        for (int i = 0; i < terrains.Length; i++)
        {
            if (targetTerrain != null && terrains[i] != targetTerrain)
            {
                continue;
            }

            createdTotal += CreateBoundaryForTerrain(terrains[i], dedupeSet, dedupeScale);
            processedTerrains++;
        }

        if (createdTotal > 0)
        {
            Debug.Log($"Created {createdTotal} boundary markers across {processedTerrains} terrain(s).");
        }
        else
        {
            Debug.LogWarning("No boundary markers created. Check tolerance and spacing settings, and ensure the terrain intersects y = 0.");
        }

#if UNITY_EDITOR
        if (!EditorApplication.isPlaying)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        }
#endif
    }

    private Terrain ResolveTerrain()
    {
        if (targetTerrain != null)
        {
            return targetTerrain;
        }

        return Terrain.activeTerrain;
    }

    private int CreateBoundaryForTerrain(Terrain terrain, HashSet<Vector3Int> dedupeSet, float dedupeScale)
    {
        if (terrain == null)
        {
            return 0;
        }

        if (spacing <= 0f)
        {
            Debug.LogWarning("Spacing must be greater than zero. Using a default of 1.0.");
            spacing = 1f;
        }

        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainOrigin = terrain.transform.position;
        Vector3 terrainSize = terrainData.size;

        int heightmapResolution = terrainData.heightmapResolution;
        int maxIndex = heightmapResolution - 1;
        if (maxIndex <= 0)
        {
            Debug.LogWarning($"Terrain '{terrain.name}' has an invalid heightmap resolution.");
            return 0;
        }

        float[,] heights = terrainData.GetHeights(0, 0, heightmapResolution, heightmapResolution);
        float sampleDeltaX = terrainSize.x / maxIndex;
        float sampleDeltaZ = terrainSize.z / maxIndex;

        int strideX = Mathf.Max(1, Mathf.RoundToInt(spacing / sampleDeltaX));
        int strideZ = Mathf.Max(1, Mathf.RoundToInt(spacing / sampleDeltaZ));

        int createdCount = 0;

        for (int z = 0; z < heightmapResolution; z += strideZ)
        {
            float worldZ = terrainOrigin.z + (z * sampleDeltaZ);

            for (int x = 0; x < heightmapResolution; x += strideX)
            {
                float worldX = terrainOrigin.x + (x * sampleDeltaX);
                float normalizedHeight = heights[z, x];
                float worldY = terrainOrigin.y + (normalizedHeight * terrainSize.y);

                if (Mathf.Abs(worldY) <= heightTolerance)
                {
                    Vector3 boundaryPosition = new Vector3(worldX, worldY, worldZ);
                    if (TryAddBoundaryPosition(boundaryPosition, dedupeSet, dedupeScale))
                    {
                        createdCount++;
                    }
                }

                int nextX = Mathf.Min(heightmapResolution - 1, x + strideX);
                int nextZ = Mathf.Min(heightmapResolution - 1, z + strideZ);

                if (nextX != x)
                {
                    float worldXNext = terrainOrigin.x + (nextX * sampleDeltaX);
                    float worldYNext = terrainOrigin.y + (heights[z, nextX] * terrainSize.y);

                    if (HasZeroCrossing(worldY, worldYNext))
                    {
                        float t = Mathf.Clamp01(worldY / (worldY - worldYNext));
                        float boundaryX = Mathf.Lerp(worldX, worldXNext, t);
                        float boundaryY = Mathf.Lerp(worldY, worldYNext, t);
                        Vector3 boundaryPosition = new Vector3(boundaryX, boundaryY, worldZ);

                        if (TryAddBoundaryPosition(boundaryPosition, dedupeSet, dedupeScale))
                        {
                            createdCount++;
                        }
                    }
                }

                if (nextZ != z)
                {
                    float worldZNext = terrainOrigin.z + (nextZ * sampleDeltaZ);
                    float worldYNext = terrainOrigin.y + (heights[nextZ, x] * terrainSize.y);

                    if (HasZeroCrossing(worldY, worldYNext))
                    {
                        float t = Mathf.Clamp01(worldY / (worldY - worldYNext));
                        float boundaryZ = Mathf.Lerp(worldZ, worldZNext, t);
                        float boundaryY = Mathf.Lerp(worldY, worldYNext, t);
                        Vector3 boundaryPosition = new Vector3(worldX, boundaryY, boundaryZ);

                        if (TryAddBoundaryPosition(boundaryPosition, dedupeSet, dedupeScale))
                        {
                            createdCount++;
                        }
                    }
                }
            }
        }

        return createdCount;
    }

    private void PrepareForGeneration()
    {
        if (!clearExistingChildren)
        {
            return;
        }

        boundaryPositions.Clear();
        ClearChildren();
    }

    private float CalculateDedupeScale()
    {
        return Mathf.Max(1f, 1f / Mathf.Max(heightTolerance, 0.001f));
    }

    private HashSet<Vector3Int> BuildDedupeSet(float dedupeScale)
    {
        HashSet<Vector3Int> dedupeSet = new HashSet<Vector3Int>();
        for (int i = 0; i < boundaryPositions.Count; i++)
        {
            dedupeSet.Add(QuantizeForDedupe(boundaryPositions[i], dedupeScale));
        }

        return dedupeSet;
    }

    private void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
                continue;
            }
#endif
            Destroy(transform.GetChild(i).gameObject);
        }
    }

    private bool TryAddBoundaryPosition(Vector3 position, HashSet<Vector3Int> dedupeSet, float dedupeScale)
    {
        Vector3Int key = QuantizeForDedupe(position, dedupeScale);
        if (!dedupeSet.Add(key))
        {
            return false;
        }

        boundaryPositions.Add(position);
        return true;
    }

    private void OnValidate()
    {
        spacing = Mathf.Max(0.01f, spacing);
        heightTolerance = Mathf.Max(0f, heightTolerance);
    }

    private bool HasZeroCrossing(float a, float b)
    {
        if (Mathf.Abs(a) <= heightTolerance || Mathf.Abs(b) <= heightTolerance)
        {
            return true;
        }

        return (a < 0f && b > 0f) || (a > 0f && b < 0f);
    }

    private static Vector3Int QuantizeForDedupe(Vector3 position, float scale)
    {
        return new Vector3Int(
            Mathf.RoundToInt(position.x * scale),
            Mathf.RoundToInt(position.y * scale),
            Mathf.RoundToInt(position.z * scale));
    }
}
