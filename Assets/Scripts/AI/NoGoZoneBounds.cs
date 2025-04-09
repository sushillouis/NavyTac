using UnityEngine;

public class NoGoZoneBounds : MonoBehaviour
{
    [Header("Sphere Settings")]
    public float radius = 5f; // Local radius of the sphere

    [Header("Repulsion Settings")]
    public float repulsionStrength = 1000f;
    public float repulsionRadius = 5f;

    void OnEnable() => NoGoZoneManager.Register(this);
    void OnDisable() => NoGoZoneManager.Unregister(this);

    // World space center of the sphere
    public Vector3 Center => transform.position;

    // World space radius accounting for scale
    public float ScaledRadius => radius * Mathf.Max(transform.lossyScale.x, 
                                                    transform.lossyScale.y, 
                                                    transform.lossyScale.z);

    // Get closest point on sphere surface in WORLD space
    public Vector3 GetClosestPoint(Vector3 worldPosition)
    {
        Vector3 center = Center;
        Vector3 direction = worldPosition - center;
        float distance = direction.magnitude;
        float scaledRadius = ScaledRadius;

        if (distance < Mathf.Epsilon)
        {
            // If at center, return point in upward direction
            return center + Vector3.up * scaledRadius;
        }

        return center + (direction / distance) * scaledRadius;
    }

    // Check if a point is inside the sphere
    public bool Contains(Vector3 worldPosition)
    {
        Vector3 center = Center;
        float sqrDistance = (worldPosition - center).sqrMagnitude;
        float scaledRadius = ScaledRadius;
        return sqrDistance <= (scaledRadius * scaledRadius);
    }

    // Calculate penetration depth (positive if inside)
    public float GetPenetrationDepth(Vector3 position)
    {
        Vector3 center = Center;
        float scaledRadius = ScaledRadius;
        float distance = Vector3.Distance(position, center);
        return scaledRadius - distance;
    }

    // Visualize sphere in editor
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        float scaledRadius = ScaledRadius;
        Gizmos.DrawSphere(Center, scaledRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(Center, scaledRadius);
    }

    // Backwards compatibility properties (if needed)
    public Vector3 WorldMinBounds => Center - Vector3.one * ScaledRadius;
    public Vector3 WorldMaxBounds => Center + Vector3.one * ScaledRadius;
}