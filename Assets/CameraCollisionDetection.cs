using UnityEngine;

public class CameraCollisionDetection : MonoBehaviour
{
    CameraMgr cameraMgr;
    public float collisionRadius = 100f; // Minimum height above terrain

    void Start()
    {
        cameraMgr = CameraMgr.inst;
    }

    void LateUpdate() // Use LateUpdate to ensure it runs after movement logic
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain != null)
        {
            // Get the camera's current world position
            Vector3 cameraWorldPos = transform.position;
            
            // Sample terrain height at the camera's XZ position
            float terrainHeight = terrain.SampleHeight(cameraWorldPos);
            float requiredCameraY = terrainHeight + collisionRadius;

            // Calculate the camera's local offset relative to the YawNode
            Vector3 cameraLocalPos = cameraMgr.YawNode.transform.InverseTransformPoint(cameraWorldPos);
            float localCameraY = cameraLocalPos.y;

            // Determine the YawNode's required Y position to keep the camera above terrain
            float requiredYawY = requiredCameraY - localCameraY;

            // Clamp the YawNode's Y position to enforce the minimum height
            Vector3 yawNodePos = cameraMgr.YawNode.transform.position;
            if (yawNodePos.y < requiredYawY)
            {
                yawNodePos.y = requiredYawY;
                cameraMgr.YawNode.transform.position = yawNodePos;
            }
        }
    }
}