using System.Collections.Generic;
using UnityEngine;

public class EnemyAIMgr : MonoBehaviour 
{
    [Header("Level 1 Settings")]
    public float clusterThreshold = 50f;  // Max distance between entities to consider a cluster
    public float formationRadius = 100f;    // Formation spacing radius
    public int currentLevel = 1;            // Current difficulty level
    public bool drawDebugVisuals = true;    // Toggle for cluster visualization

    private Vector3? lastTargetCluster;     // Last targeted cluster position
    private HashSet<Entity> commandedEntities = new HashSet<Entity>();
    private List<Entity> dividedEntities = new List<Entity>();

    private void Start() {
        EntityMgr.inst.OnEntityAdded += HandleEntityAdded;
    }

    private void OnDestroy() {
        if (EntityMgr.inst != null) {
            EntityMgr.inst.OnEntityAdded -= HandleEntityAdded;
        }
    }

    private void Update() {
        if (currentLevel == 1) {
            HandleLevel1Behavior();
        }
    }

    private void HandleEntityAdded(Entity newEntity) {
        if (newEntity.owner.name == "Ai" && 
            newEntity.creatorsEntity != null && 
            newEntity.creatorsEntity.owner.name == "Ai") {
            dividedEntities.Add(newEntity);
        }
    }

    private void HandleLevel1Behavior() {
        List<Entity> opponents = GetValidOpponents();
        List<Vector3> clusters = CalculateAllClusters(opponents);
        List<Entity> aiEntities = GetAIEntities();

        // Integrate divided entities
        if (dividedEntities.Count > 0) {
            aiEntities.AddRange(dividedEntities);
            dividedEntities.Clear();
        }

        if (clusters.Count == 0 || aiEntities.Count == 0) {
            lastTargetCluster = null;
            commandedEntities.Clear();
            return;
        }

        // Find nearest cluster to AI group center
        Vector3 aiCenter = CalculateGroupCenter(aiEntities);
        Vector3 nearestCluster = GetNearestCluster(clusters, aiCenter);

        if (ShouldRegroup(nearestCluster, aiEntities)) {
            ExecuteFormationMove(aiEntities, nearestCluster);
            lastTargetCluster = nearestCluster;
        }

        // Debug visualization
        if (drawDebugVisuals) {
            foreach (Vector3 cluster in clusters) {
                DrawClusterRadius(cluster, clusterThreshold, 
                    cluster == nearestCluster ? Color.green : Color.yellow);
            }
        }
    }

    private List<Vector3> CalculateAllClusters(List<Entity> entities) {
        List<Vector3> clusters = new List<Vector3>();
        HashSet<Entity> processed = new HashSet<Entity>();

        foreach (Entity entity in entities) {
            if (!processed.Contains(entity)) {
                // Find all entities in this cluster
                List<Entity> clusterMembers = new List<Entity>();
                Queue<Entity> toCheck = new Queue<Entity>();
                toCheck.Enqueue(entity);
                processed.Add(entity);

                while (toCheck.Count > 0) {
                    Entity current = toCheck.Dequeue();
                    clusterMembers.Add(current);

                    // Find nearby unprocessed entities
                    foreach (Entity other in entities) {
                        if (!processed.Contains(other) && 
                            Vector3.Distance(current.position, other.position) <= clusterThreshold) {
                            processed.Add(other);
                            toCheck.Enqueue(other);
                        }
                    }
                }

                // Calculate cluster center
                if (clusterMembers.Count > 0) {
                    clusters.Add(CalculateClusterCenter(clusterMembers));
                }
            }
        }
        return clusters;
    }

    private Vector3 CalculateClusterCenter(List<Entity> cluster) {
        Vector3 center = Vector3.zero;
        foreach (Entity entity in cluster) {
            center += entity.position;
        }
        return center / cluster.Count;
    }

    private Vector3 GetNearestCluster(List<Vector3> clusters, Vector3 referencePoint) {
        Vector3 nearest = clusters[0];
        float minDistance = float.MaxValue;

        foreach (Vector3 cluster in clusters) {
            float distance = Vector3.Distance(referencePoint, cluster);
            if (distance < minDistance) {
                minDistance = distance;
                nearest = cluster;
            }
        }
        return nearest;
    }

    private Vector3 CalculateGroupCenter(List<Entity> aiEntities) {
        Vector3 center = Vector3.zero;
        foreach (Entity entity in aiEntities) {
            center += entity.position;
        }
        return center / aiEntities.Count;
    }

    private bool ShouldRegroup(Vector3 currentTarget, List<Entity> aiEntities) {
        // First regroup if no previous target
        if (!lastTargetCluster.HasValue) return true;

        // Check cluster movement
        bool clusterMoved = Vector3.Distance(currentTarget, lastTargetCluster.Value) > formationRadius * 0.5f;

        // Check for new units
        bool newUnitsExist = false;
        foreach (Entity entity in aiEntities) {
            if (!commandedEntities.Contains(entity)) {
                newUnitsExist = true;
                break;
            }
        }

        return clusterMoved || newUnitsExist;
    }

    private void ExecuteFormationMove(List<Entity> aiEntities, Vector3 target) {
        AIMgr.inst.HandleMove(
            aiEntities,
            target,
            false,
            false,
            true,
            FormationType.Circle,
            formationRadius
        );

        // Update tracking
        commandedEntities.Clear();
        commandedEntities.UnionWith(aiEntities);
    }

    private void DrawClusterRadius(Vector3 center, float radius, Color color) {
        const int segments = 36;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);
        
        for (int i = 1; i <= segments; i++) {
            float angle = 2 * Mathf.PI * i / segments;
            Vector3 nextPoint = center + new Vector3(
                Mathf.Cos(angle) * radius,
                0,
                Mathf.Sin(angle) * radius
            );
            Debug.DrawLine(prevPoint, nextPoint, color);
            prevPoint = nextPoint;
        }
    }

    // Keep existing GetValidOpponents() and GetAIEntities() methods
    private List<Entity> GetValidOpponents() {
        List<Entity> opponents = new List<Entity>();
        foreach (Entity entity in EntityMgr.inst.entities) {
            if (entity.owner.name != "Ai" && 
                entity.entityClass != EntityClass.Missile && 
                entity.creatorsEntity == null) {
                opponents.Add(entity);
            }
        }
        return opponents;
    }

    private List<Entity> GetAIEntities() {
        List<Entity> aiEntities = new List<Entity>();
        foreach (Entity entity in EntityMgr.inst.entities) {
            if (entity.owner.name == "Ai" && 
                entity.entityClass != EntityClass.Missile && 
                entity.creatorsEntity == null) {
                aiEntities.Add(entity);
            }
        }
        return aiEntities;
    }
}