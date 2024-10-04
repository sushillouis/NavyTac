using UnityEngine;

public class MapDisplay : MonoBehaviour
{
    public void DrawMesh(MeshData meshData, MeshFilter targetMeshFilter) {
        targetMeshFilter.sharedMesh = meshData.CreateMesh();
    }
}
