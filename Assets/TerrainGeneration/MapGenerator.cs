using JetBrains.Annotations;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public const int mapChunkSize =241; 
    public float mapScale;
    public float heightMultiplier;
    public AnimationCurve meshHeightCurve;
    MapDisplay display;

    // / <summary>
    // / Start is called on the frame when a script is enabled just before
    // / any of the Update methods is called the first time.
    /// </summary>
    void Start()
    {
        display = GetComponent<MapDisplay>();
    }

    public void GeneratePaintMapFromArray(float [,] newMap,MeshFilter targetMesh) {
        display.DrawMesh(MeshGenerator.GenerateTerrainMesh(newMap,heightMultiplier,meshHeightCurve,0,mapScale),targetMesh);
    }
}
