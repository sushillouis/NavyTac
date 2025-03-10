// using System.Collections.Generic;
// using UnityEngine;
// using System.Linq;

// public class EnemyAIMgr : MonoBehaviour 
// {
//     [Header("Level 1 Settings")]
//     public float clusterThreshold = 50f;  // Max distance between entities to consider a cluster
//     public float formationRadius = 100f;    // Formation spacing radius
//     public int currentLevel = 1;            // Current difficulty level
//     public bool drawDebugVisuals = true;    // Toggle for cluster visualization

//     private Vector3? lastTargetCluster;     // Last targeted cluster position
//     private HashSet<Entity> commandedEntities = new HashSet<Entity>();
//     private List<Entity> dividedEntities = new List<Entity>();

//     private void Start() {
//         EntityMgr.inst.OnEntityAdded += HandleEntityAdded;
//     }

//     private void OnDestroy() {
//         if (EntityMgr.inst != null) {
//             EntityMgr.inst.OnEntityAdded -= HandleEntityAdded;
//         }
//     }

//     private void Update() {
//         if (currentLevel == 1) {
//             HandleLevel1Behavior();
//         }
//     }

//     private void HandleEntityAdded(Entity newEntity) {
//         if (newEntity.owner.name == "Ai" && 
//             newEntity.creatorsEntity != null && 
//             newEntity.creatorsEntity.owner.name == "Ai") {
//             dividedEntities.Add(newEntity);
//         }
//     }

//     private void HandleLevel1Behavior() {
//         List<Entity> opponents = GetValidOpponents();
//         List<Vector3> clusters = CalculateAllClusters(opponents);
//         List<Entity> aiEntities = GetAIEntities();

//         // Integrate divided entities
//         if (dividedEntities.Count > 0) {
//             aiEntities.AddRange(dividedEntities);
//             dividedEntities.Clear();
//         }

//         if (clusters.Count == 0 || aiEntities.Count == 0) {
//             lastTargetCluster = null;
//             commandedEntities.Clear();
//             return;
//         }

//         // Find nearest cluster to AI group center
//         Vector3 aiCenter = CalculateGroupCenter(aiEntities);
//         Vector3 nearestCluster = GetNearestCluster(clusters, aiCenter);

//         if (ShouldRegroup(nearestCluster, aiEntities)) {
//             ExecuteFormationMove(aiEntities, nearestCluster);
//             lastTargetCluster = nearestCluster;
//         }

//         // Debug visualization
//         if (drawDebugVisuals) {
//             foreach (Vector3 cluster in clusters) {
//                 DrawClusterRadius(cluster, clusterThreshold, 
//                     cluster == nearestCluster ? Color.green : Color.yellow);
//             }
//         }
//     }

//     private List<Vector3> CalculateAllClusters(List<Entity> entities) {
//         List<Vector3> clusters = new List<Vector3>();
//         HashSet<Entity> processed = new HashSet<Entity>();

//         foreach (Entity entity in entities) {
//             if (!processed.Contains(entity)) {
//                 // Find all entities in this cluster
//                 List<Entity> clusterMembers = new List<Entity>();
//                 Queue<Entity> toCheck = new Queue<Entity>();
//                 toCheck.Enqueue(entity);
//                 processed.Add(entity);

//                 while (toCheck.Count > 0) {
//                     Entity current = toCheck.Dequeue();
//                     clusterMembers.Add(current);

//                     // Find nearby unprocessed entities
//                     foreach (Entity other in entities) {
//                         if (!processed.Contains(other) && 
//                             Vector3.Distance(current.position, other.position) <= clusterThreshold) {
//                             processed.Add(other);
//                             toCheck.Enqueue(other);
//                         }
//                     }
//                 }

//                 // Calculate cluster center
//                 if (clusterMembers.Count > 0) {
//                     clusters.Add(CalculateClusterCenter(clusterMembers));
//                 }
//             }
//         }
//         return clusters;
//     }

//     private Vector3 CalculateClusterCenter(List<Entity> cluster) {
//         Vector3 center = Vector3.zero;
//         foreach (Entity entity in cluster) {
//             center += entity.position;
//         }
//         return center / cluster.Count;
//     }

//     private Vector3 GetNearestCluster(List<Vector3> clusters, Vector3 referencePoint) {
//         Vector3 nearest = clusters[0];
//         float minDistance = float.MaxValue;

//         foreach (Vector3 cluster in clusters) {
//             float distance = Vector3.Distance(referencePoint, cluster);
//             if (distance < minDistance) {
//                 minDistance = distance;
//                 nearest = cluster;
//             }
//         }
//         return nearest;
//     }

//     private Vector3 CalculateGroupCenter(List<Entity> aiEntities) {
//         Vector3 center = Vector3.zero;
//         foreach (Entity entity in aiEntities) {
//             center += entity.position;
//         }
//         return center / aiEntities.Count;
//     }

//     private bool ShouldRegroup(Vector3 currentTarget, List<Entity> aiEntities) {
//         // First regroup if no previous target
//         if (!lastTargetCluster.HasValue) return true;

//         // Check cluster movement
//         bool clusterMoved = Vector3.Distance(currentTarget, lastTargetCluster.Value) > formationRadius * 0.5f;

//         // Check for new units
//         bool newUnitsExist = false;
//         foreach (Entity entity in aiEntities) {
//             if (!commandedEntities.Contains(entity)) {
//                 newUnitsExist = true;
//                 break;
//             }
//         }

//         return clusterMoved || newUnitsExist;
//     }

//     private void ExecuteFormationMove(List<Entity> aiEntities, Vector3 target) {
//         AIMgr.inst.HandleMove(
//             aiEntities.Where(Entity=> Entity.entityType !=EntityType.Rig_Balder).ToList(),
//             target,
//             false,
//             false,
//             true,
//             FormationType.Vee,
//             formationRadius
//         );

//         // Update tracking
//         commandedEntities.Clear();
//         commandedEntities.UnionWith(aiEntities);
//     }

//     private void DrawClusterRadius(Vector3 center, float radius, Color color) {
//         const int segments = 36;
//         Vector3 prevPoint = center + new Vector3(radius, 0, 0);
        
//         for (int i = 1; i <= segments; i++) {
//             float angle = 2 * Mathf.PI * i / segments;
//             Vector3 nextPoint = center + new Vector3(
//                 Mathf.Cos(angle) * radius,
//                 0,
//                 Mathf.Sin(angle) * radius
//             );
//             Debug.DrawLine(prevPoint, nextPoint, color);
//             prevPoint = nextPoint;
//         }
//     }

//     // Keep existing GetValidOpponents() and GetAIEntities() methods
//     private List<Entity> GetValidOpponents() {
//         List<Entity> opponents = new List<Entity>();
//         foreach (Entity entity in EntityMgr.inst.entities) {
//             if (entity.owner.name != "Ai" && 
//                 entity.entityClass != EntityClass.Missile && 
//                 entity.creatorsEntity == null) {
//                 opponents.Add(entity);
//             }
//         }
//         return opponents;
//     }

//     private List<Entity> GetAIEntities() {
//         List<Entity> aiEntities = new List<Entity>();
//         foreach (Entity entity in EntityMgr.inst.entities) {
//             if (entity.owner.name == "Ai" && 
//                 entity.entityClass != EntityClass.Missile && 
//                 entity.creatorsEntity == null) {
//                 aiEntities.Add(entity);
//             }
//         }
//         return aiEntities;
//     }
// }

// 
// using System.Collections.Generic;
// using UnityEngine;
// using System.Linq;
// using System;

// public class EnemyAIMgr : MonoBehaviour 
// {
//     [Header("Level 1 Settings")]
//     public float baseAttackRadius = 10f;  // Distance from base to maintain
//     public int currentLevel = 1;        
//     public bool drawDebugVisuals = true;   
//     public List <Entity> aiEntities;  

//     private void Start() {
//         EntityMgr.inst.OnEntityAdded += HandleEntityAdded;
//     }

//     public static EnemyAIMgr inst;
//      void Awake()
//     {
//         inst = this;
//     }

//     private void OnDestroy() {
//         if (EntityMgr.inst != null) {
//             EntityMgr.inst.OnEntityAdded -= HandleEntityAdded;
//         }
//     }

//     private void Update() {
//         if (currentLevel == 1) {
            
//             HandleLevel1Behavior();
//         }
//     }

//     private void HandleEntityAdded(Entity newEntity) {
//         // No specific handling needed for this implementation
//     }
//     Entity OldBase = null;
//     private void HandleLevel1Behavior() {
//         Entity opponentBase = FindOpponentBase();
        
//         if (opponentBase == null) return;
//         if(OldBase != null && OldBase == opponentBase){
//             return;
//         }
//         OldBase = opponentBase;
//         aiEntities = GetAIEntities();
//         if (aiEntities.Count == 0) return;

//         // Order entities consistently for stable positioning
//         aiEntities = aiEntities.OrderBy(e => e.name).ToList();
//         Vector3 basePos = opponentBase.position;

//         for (int i = 0; i < aiEntities.Count; i++) {
//             Entity entity = aiEntities[i];
//             float angle = (360f / aiEntities.Count) * i;
//             Vector3 direction = new Vector3(
//                 Mathf.Sin(angle * Mathf.Deg2Rad),
//                 0,
//                 Mathf.Cos(angle * Mathf.Deg2Rad)
//             );
//             Vector3 targetPos = basePos + direction * baseAttackRadius;

//             // Command individual movement
//             AIMgr.inst.HandleMove(
//                 new List<Entity> { entity },
//                 targetPos, false
//             );
//         }

//         // Debug visualization
//         if (drawDebugVisuals) {
//             DrawBaseRadius(basePos, baseAttackRadius);
//         }
//     }

//     private Entity FindOpponentBase() {
//         foreach (Entity entity in EntityMgr.inst.entities) {
//             if (entity.owner.name != "Ai" && entity.entityRole == EntityRole.Base) {
//                 return entity;
//             }
//         }
//         return null;
//     }

//     private List<Entity> GetAIEntities() {
//         List<Entity> aiEntities = new List<Entity>();
//         foreach (Entity entity in EntityMgr.inst.entities) {
//             if (entity.owner.name == "Ai" && 
//                 entity.entityClass != EntityClass.Missile && 
//                 entity.creatorsEntity == null) {
//                 aiEntities.Add(entity);
//             }
//         }
//         return aiEntities;
//     }

//     private void DrawBaseRadius(Vector3 center, float radius) {
//         const int segments = 36;
//         Vector3 prevPoint = center + new Vector3(radius, 0, 0);
        
//         for (int i = 1; i <= segments; i++) {
//             float angle = 2 * Mathf.PI * i / segments;
//             Vector3 nextPoint = center + new Vector3(
//                 Mathf.Cos(angle) * radius,
//                 0,
//                 Mathf.Sin(angle) * radius
//             );
//             Debug.DrawLine(prevPoint, nextPoint, Color.blue);
//             prevPoint = nextPoint;
//         }
//     }
// }'

using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class EnemyAIMgr : MonoBehaviour 
{
    [Header("Level 1 Settings")]
    public float baseAttackRadius = 10f;  // Distance from base to maintain
    public float minEngagementDistance = 5f; // Minimum distance to keep from opponent entities
    public int currentLevel = 1;        
    public bool drawDebugVisuals = true;   
    public List<Entity> aiEntities;  

    private Entity OldBase = null;

    private void Start() {
        EntityMgr.inst.OnEntityAdded += HandleEntityAdded;
    }

    public static EnemyAIMgr inst;
    void Awake()
    {
        inst = this;
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
        // No specific handling needed for this implementation
    }

    private void HandleLevel1Behavior() {
        Entity opponentBase = FindOpponentBase();
        
        if (opponentBase == null) return;
        if (OldBase != null && OldBase == opponentBase) return;
        
        OldBase = opponentBase;
        aiEntities = GetAIEntities();
        if (aiEntities.Count == 0) return;

        // Sort entities for consistent formation
        aiEntities = aiEntities.OrderBy(e => e.name).ToList();
        Vector3 basePos = opponentBase.position;

        // Create wedge formation approaching the base
        FormWedgeFormation(basePos);

        // Find and attack nearest enemy
        HandleCombatBehavior(opponentBase);

        if (drawDebugVisuals) {
            DrawBaseRadius(basePos, baseAttackRadius);
        }
    }

    private void FormWedgeFormation(Vector3 targetPos)
    {
        if (aiEntities.Count == 0) return;

        // Define wedge parameters
        float spacing = 100f; // Adjust as needed
        Vector3 approachDirection = (targetPos - aiEntities[0].position).normalized;
        Vector3 rightVector = Vector3.Cross(approachDirection, Vector3.up).normalized;

        int n = aiEntities.Count;
        int m = n / 2; // Number of entities per arm (adjusted for odd/even)
        if (n % 2 == 1) m = (n - 1) / 2;

        float width = spacing * (n / 2f); // Width scales with number of entities
        float formationDepth = spacing * (m + 1); // Depth from front to rear

        // Calculate key positions for inverted wedge
        Vector3 frontCenter = targetPos - approachDirection * baseAttackRadius;
        Vector3 point = frontCenter - approachDirection * formationDepth; // Point at the back
        Vector3 frontLeft = frontCenter - rightVector * width; // Left front position
        Vector3 frontRight = frontCenter + rightVector * width; // Right front position

        List<Vector3> positions = new List<Vector3>();

        // Generate left arm positions (from back to front)
        for (int k = 1; k <= m; k++)
        {
            float t = (float)k / (m + 1);
            Vector3 leftPos = point + (frontLeft - point) * t;
            positions.Add(leftPos);
        }

        // Add center point (back) for odd number of entities
        if (n % 2 == 1)
        {
            positions.Add(point);
        }

        // Generate right arm positions (from back to front)
        for (int k = 1; k <= m; k++)
        {
            float t = (float)k / (m + 1);
            Vector3 rightPos = point + (frontRight - point) * t;
            positions.Add(rightPos);
        }

        // Sort positions from front to back based on distance along approach direction
        positions = positions.OrderByDescending(p => Vector3.Dot(p - point, approachDirection)).ToList();

        // Sort entities with lower priority first (higher index in priorityList = closer to target)
        aiEntities = aiEntities.OrderBy(e => GameMgr.inst.priorityList.IndexOf(e.entityType)).ToList();

        // Assign positions to entities
        for (int i = 0; i < n; i++)
        {
            AIMgr.inst.HandleMove(new List<Entity> { aiEntities[i] }, positions[i], false);
        }
    }
    private void HandleCombatBehavior(Entity opponentBase) {
        foreach (Entity aiEntity in aiEntities) {
            Entity nearestEnemy = FindNearestEnemy(aiEntity, opponentBase);
            if (nearestEnemy != null) {
                Vector3 toEnemy = nearestEnemy.position - aiEntity.position;
                float distance = toEnemy.magnitude;
                
                // Maintain minimum distance
                if (distance < minEngagementDistance) {
                    Vector3 retreatPos = aiEntity.position - toEnemy.normalized * (minEngagementDistance - distance);
                    AIMgr.inst.HandleMove(new List<Entity> { aiEntity }, retreatPos, false);
                }
                WeaponsMgr.inst.handleWeapon(aiEntity, nearestEnemy);
            }
        }
    }

    private Entity FindNearestEnemy(Entity aiEntity, Entity opponentBase) {
        Entity nearest = null;
        float minDist = float.MaxValue;

        foreach (Entity entity in EntityMgr.inst.entities) {
            if (entity.owner.name != "Ai" && entity != opponentBase) {
                float dist = Vector3.Distance(aiEntity.position, entity.position);
                if (dist < minDist) {
                    minDist = dist;
                    nearest = entity;
                }
            }
        }
        return nearest;
    }

    private Entity FindOpponentBase() {
        foreach (Entity entity in EntityMgr.inst.entities) {
            if (entity.owner.name != "Ai" && entity.entityRole == EntityRole.Base) {
                return entity;
            }
        }
        return null;
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

    private void DrawBaseRadius(Vector3 center, float radius) {
        const int segments = 36;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);
        
        for (int i = 1; i <= segments; i++) {
            float angle = 2 * Mathf.PI * i / segments;
            Vector3 nextPoint = center + new Vector3(
                Mathf.Cos(angle) * radius,
                0,
                Mathf.Sin(angle) * radius
            );
            Debug.DrawLine(prevPoint, nextPoint, Color.blue);
            prevPoint = nextPoint;
        }
    }
}