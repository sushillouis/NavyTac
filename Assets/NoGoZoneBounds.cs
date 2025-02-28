using UnityEngine;

public class NoGoZoneBounds : MonoBehaviour
{
    [Header("Local Bounds")]
    public Vector3 localSize = new Vector3(10, 5, 10); // Size relative to the GameObject

    [Header("Repulsion Settings")]
    public float repulsionStrength = 1000f;
    public float repulsionRadius = 5f;
    void OnEnable() => NoGoZoneManager.Register(this);
    void OnDisable() => NoGoZoneManager.Unregister(this);

    // Calculate world-space min/max bounds based on rotation/position
    public Vector3 WorldMinBounds
    {
        get
        {
            Vector3 localMin = -localSize / 2f;
            return transform.TransformPoint(localMin);
        }
    }

    public Vector3 WorldMaxBounds
    {
        get
        {
            Vector3 localMax = localSize / 2f;
            return transform.TransformPoint(localMax);
        }
    }

    // Get closest point in WORLD space (accounts for rotation)
    public Vector3 GetClosestPoint(Vector3 worldPosition)
    {
        // Convert world position to LOCAL space
        Vector3 localPos = transform.InverseTransformPoint(worldPosition);

        // Clamp within local bounds
        Vector3 clampedLocal = new Vector3(
            Mathf.Clamp(localPos.x, -localSize.x / 2f, localSize.x / 2f),
            Mathf.Clamp(localPos.y, -localSize.y / 2f, localSize.y / 2f),
            Mathf.Clamp(localPos.z, -localSize.z / 2f, localSize.z / 2f)
        );

        // Convert clamped point back to WORLD space
        return transform.TransformPoint(clampedLocal);
    }

    // Check if a point is inside the rotated bounds
    public bool Contains(Vector3 worldPosition)
    {
        Vector3 localPos = transform.InverseTransformPoint(worldPosition);
        return Mathf.Abs(localPos.x) <= localSize.x / 2f &&
               Mathf.Abs(localPos.y) <= localSize.y / 2f &&
               Mathf.Abs(localPos.z) <= localSize.z / 2f;
    }

    // Visualize rotated box in the editor
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.zero, localSize);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(Vector3.zero, localSize);
    }
}