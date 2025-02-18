using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AiState
{
    Idle,
    Move,
    Attack,
    Flee,
    Scout,
    Patrol,
    Chase,
    Dead
}

public class EnemyAIMgr : MonoBehaviour
{
    [System.Serializable]
    public class AiData
    {
        public Entity entity;
        public AiState state;
        public Vector3 destination;
        public float moveSpeed;
        
        // Scout parameters
        public Vector3 scoutCenter;
        public float scoutRadius = 4500f;
        public float detectionRadius = 1000f;
        public Entity detectedTarget;
        public bool isSelectedScout;
        public int assignedQuadrant;
    }

    // Configuration
    public float maxScoutRadius = 4000f; // Patrol region radius (half size of total area)
    public int maxScouts = 4;
    public float targetSwitchDistance = 2000f;
    public float minScoutSpeed = 10f; // Minimum speed to qualify as a scout
    public int fixedSeed = 12345;    // Fixed seed for consistent random pattern

    // Runtime data
    public List<AiData> potentialScouts = new List<AiData>();
    public List<Vector3> scoutRegions = new List<Vector3>();

    IEnumerator Start()
    {
        // Set the fixed random seed so that scout patterns are identical each run
        Random.InitState(fixedSeed);

        InitializeScoutRegions();
        
        // Wait for the EntityMgr to be ready
        while (EntityMgr.inst == null)
        {
            yield return null;
        }

        EntityMgr.inst.OnEntityAdded += HandleEntityAdded;
        EntityMgr.inst.OnEntityRemoved += HandleEntityRemoved;
    }

    void OnDisable()
    {
        if (EntityMgr.inst != null)
        {
            EntityMgr.inst.OnEntityAdded -= HandleEntityAdded;
            EntityMgr.inst.OnEntityRemoved -= HandleEntityRemoved;
        }
    }

    void InitializeScoutRegions()
    {
        // Divide map into 4 regions (North, West, South, East)
        scoutRegions.Add(new Vector3(0, 0, maxScoutRadius));    // North
        scoutRegions.Add(new Vector3(-maxScoutRadius, 0, 0));     // West
        scoutRegions.Add(new Vector3(0, 0, -maxScoutRadius));     // South
        scoutRegions.Add(new Vector3(maxScoutRadius, 0, 0));      // East
    }

    void UpdateScoutSelection()
    {
        // Filter entities that meet the minimum speed requirement
        var eligibleScouts = potentialScouts.FindAll(a => a.moveSpeed >= minScoutSpeed);
        
        // Order by speed descending so that faster units are preferred
        eligibleScouts.Sort((a, b) => b.moveSpeed.CompareTo(a.moveSpeed));
        
        // Assign scout status to the top candidates (up to maxScouts)
        for (int i = 0; i < potentialScouts.Count; i++)
        {
            bool isScout = i < eligibleScouts.Count && i < maxScouts;
            potentialScouts[i].isSelectedScout = isScout;
            
            if (isScout && potentialScouts[i].state != AiState.Scout)
            {
                InitializeScout(potentialScouts[i], scoutRegions[i % scoutRegions.Count], i % scoutRegions.Count);
            }
            else if (!isScout && potentialScouts[i].state == AiState.Scout)
            {
                potentialScouts[i].state = AiState.Idle;
            }
        }
    }

    void InitializeScout(AiData scout, Vector3 regionCenter, int quadrantIndex)
    {
        scout.state = AiState.Scout;
        scout.scoutCenter = regionCenter;
        scout.scoutRadius = maxScoutRadius * 0.8f; // 80% of the region's radius
        scout.detectionRadius = 1500f;
        scout.assignedQuadrant = quadrantIndex;
        scout.destination = GetValidWaterPoint(scout);
    }

    void HandleEntityAdded(Entity entity)
    {
        if (entity.owner.name == "Ai")
        {
            var newAI = new AiData
            {
                entity = entity,
                state = AiState.Idle,
                moveSpeed = entity.maxSpeed,
                isSelectedScout = false,
                assignedQuadrant = -1
            };

            potentialScouts.Add(newAI);
            UpdateScoutSelection();
        }
    }

    void HandleEntityRemoved(Entity entity)
    {
        potentialScouts.RemoveAll(aiData => aiData.entity == entity);
        UpdateScoutSelection();
    }

    void Update()
    {
        foreach (var aiData in potentialScouts)
        {
            if (aiData.isSelectedScout && aiData.state == AiState.Scout)
            {
                HandleLargeMapScouting(aiData);
            }
        }
    }

    void HandleLargeMapScouting(AiData aiData)
    {
        // Try to detect a target using the current scout's logic
        aiData.detectedTarget = FindNearestEntity(aiData);
        
        if (aiData.detectedTarget != null)
        {
            float distance = Vector3.Distance(aiData.entity.transform.position,
                                              aiData.detectedTarget.transform.position);
            
            // If the detected target is close enough, command ALL scouts to move toward it.
            if (distance <= targetSwitchDistance)
            {
                // Update each AI in the potentialScouts list
                foreach (var scout in potentialScouts)
                {
                    scout.state = AiState.Move;
                    scout.destination = aiData.detectedTarget.transform.position;
                }
                
                // Gather all entities from the potentialScouts list
                List<Entity> allEntities = new List<Entity>();
                foreach (var scout in potentialScouts)
                {
                    allEntities.Add(scout.entity);
                }
                
                // Command all entities to move to the target location.
                AIMgr.inst.HandleMove(
                    allEntities, 
                    aiData.detectedTarget.transform.position,
                    false,  // Set to false for this example (no pathfinding)
                    false
                );
                return;
            }
        }
        
        // If no valid target is detected, continue with normal patrol logic.
        if (Vector3.Distance(aiData.entity.transform.position, aiData.destination) < 100f)
        {
            aiData.destination = GetValidWaterPoint(aiData);
        }
        
        AIMgr.inst.HandleMove(
            new List<Entity> { aiData.entity },
            aiData.destination,
            false,
            false
        );
    }

    /// <summary>
    /// Generates a candidate point within the scout's region, then validates that it is on water.
    /// </summary>
    Vector3 GetValidWaterPoint(AiData scout)
    {
        const int maxAttempts = 10;
        for (int i = 0; i < maxAttempts; i++)
        {
            // Generate a candidate point relative to the scout's designated region.
            Vector3 candidate = GetStrategicScoutPoint(scout);
            
            // Offset from the candidate to ensure the ray starts above the water.
            Vector3 rayStart = candidate + Vector3.up * 100f;
            RaycastHit hit;
            
            // Perform a raycast that only collides with objects on the "Water" layer.
            if (Physics.Raycast(rayStart, Vector3.down, out hit, 200f, LayerMask.GetMask("Ocean")))
            {
                // Return the hit point on the water surface.
                return hit.point;
            }
        }
        // Fallback: If no water point is found, return the scout's center.
        return scout.scoutCenter;
    }

    /// <summary>
    /// Generates a random point within a circle (with an edge bias) around the scout's region center.
    /// </summary>
    Vector3 GetStrategicScoutPoint(AiData scout)
    {
        // Edge bias to nudge points toward the outer areas of the circle.
        float edgeBias = 0.8f;
        Vector2 randomCircle = Random.insideUnitCircle * scout.scoutRadius;
        
        return scout.scoutCenter + new Vector3(
            randomCircle.x * edgeBias,
            0,
            randomCircle.y * edgeBias
        );
    }

    Entity FindNearestEntity(AiData aiData)
    {
        Entity nearest = null;
        float closestDistance = Mathf.Infinity;
        Vector3 aiPosition = aiData.entity.transform.position;

        foreach (Entity entity in EntityMgr.inst.entities)
        {
            // Skip self and other AI entities
            if (entity == aiData.entity || entity.owner.name == "Ai") 
                continue;

            float distance = Vector3.Distance(aiPosition, entity.transform.position);
            if (distance < aiData.detectionRadius && distance < closestDistance)
            {
                nearest = entity;
                closestDistance = distance;
            }
        }

        return nearest;
    }

    void OnDrawGizmosSelected()
    {
        // Visualize scout regions in the editor for debugging.
        Gizmos.color = Color.cyan;
        foreach (var region in scoutRegions)
        {
            Gizmos.DrawWireSphere(region, maxScoutRadius * 0.8f);
        }
    }
}
