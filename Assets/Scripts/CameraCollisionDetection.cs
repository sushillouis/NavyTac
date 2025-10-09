using UnityEngine;

public class CameraCollisionDetection : MonoBehaviour
{
    private CameraMgr cameraMgr;
    public float collisionRadius = 50f;
    public LayerMask terrainLayer; // Assign "Terrain" layer in Inspector
    private float epsilon = 0.001f;

    void Start()
    {
        cameraMgr = CameraMgr.inst;
        if (cameraMgr == null || cameraMgr.YawNode == null)
        {
            enabled = false;
            return;
        }
    }

    void LateUpdate()
    {
        try
        {
            Vector3 cameraWorldPos = transform.position;
            float maxRayDistance = 20000f;

            // Cast a ray straight down to detect any collider on terrain layer
            if (Physics.Raycast(cameraWorldPos + Vector3.up * maxRayDistance, Vector3.down,
                out RaycastHit hit, maxRayDistance * 2f, terrainLayer, QueryTriggerInteraction.Ignore))
            {
                float groundY = hit.point.y;
                float requiredCameraY = groundY + collisionRadius;

                // Calculate local offset relative to yaw node
                Vector3 cameraLocalPos = cameraMgr.YawNode.transform.InverseTransformPoint(cameraWorldPos);
                float localCameraY = cameraLocalPos.y;
                float requiredYawY = requiredCameraY - localCameraY;

                Vector3 yawNodePos = cameraMgr.YawNode.transform.position;
                if (yawNodePos.y < requiredYawY - epsilon)
                {
                    yawNodePos.y = requiredYawY;
                    cameraMgr.YawNode.transform.position = yawNodePos;
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"CameraCollisionDetection error: {ex.Message}");
        }
    }
}
