using UnityEngine;

public class CameraCollisionDetection : MonoBehaviour
{
    CameraMgr cameraMgr;
    public float collisionRadius = 50f; // Minimum height above terrain
    private float epsilon = 0.001f; // Small offset to prevent floating-point issues

    void Start()
    {
        cameraMgr = CameraMgr.inst;
        if (cameraMgr == null)
        {
            //Debug.LogError("CameraMgr.inst is null. CameraCollisionDetection will not work.");
            enabled = false;
            return;
        }
        if (cameraMgr.YawNode == null)
        {
            //Debug.LogError("cameraMgr.YawNode is null. CameraCollisionDetection will not work.");
            enabled = false;
            return;
        }
    }

    void LateUpdate()
    {
        Vector3 cameraWorldPos = transform.position;
        Terrain terrain = FindTerrainAtPosition(cameraWorldPos);
        
        if (terrain == null) return; // Exit if no terrain is found

        float terrainHeight = terrain.SampleHeight(cameraWorldPos);
        float requiredCameraY = terrainHeight + collisionRadius;

        // Calculate the camera's local offset relative to YawNode
        Vector3 cameraLocalPos = cameraMgr.YawNode.transform.InverseTransformPoint(cameraWorldPos);
        float localCameraY = cameraLocalPos.y;

        // Required YawNode Y to keep camera above terrain
        float requiredYawY = requiredCameraY - localCameraY;

        // Apply clamping with epsilon to prevent micro-adjustments
        Vector3 yawNodePos = cameraMgr.YawNode.transform.position;
        if (yawNodePos.y < requiredYawY - epsilon)
        {
            yawNodePos.y = requiredYawY;
            cameraMgr.YawNode.transform.position = yawNodePos;
        }
    }

    // Finds the terrain that contains the given XZ position
    private Terrain FindTerrainAtPosition(Vector3 position)
    {
        Terrain[] terrains = Terrain.activeTerrains;
        foreach (Terrain terrain in terrains)
        {
            TerrainData data = terrain.terrainData;
            Vector3 terrainPos = terrain.transform.position;
            if (position.x >= terrainPos.x && position.x <= terrainPos.x + data.size.x &&
                position.z >= terrainPos.z && position.z <= terrainPos.z + data.size.z)
            {
                return terrain;
            }
        }
        return null;
    }
}