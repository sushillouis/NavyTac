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
    private Dictionary<Entity, Vector3> formationPositions = new Dictionary<Entity, Vector3>();

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
        // Placeholder for handling new entities if needed
    }

    private void HandleLevel1Behavior() {
        // State 1: Find Base
        Entity opponentBase = FindOpponentBase();
        if (opponentBase == null){
            Debug.Log("AI WON");
            return;

        } 

        // Combat behavior runs every frame to allow attacking while moving
        HandleCombatBehavior(opponentBase);

        // If base hasn't changed, continue with current behavior
        if (OldBase != null && OldBase == opponentBase) return;

        // State 2: Move - Base found or changed, set up formation
        OldBase = opponentBase;
        aiEntities = GetAIEntities();
        if (aiEntities.Count == 0) return;
        aiEntities = aiEntities.OrderBy(e => e.name).ToList();
        Vector3 basePos = opponentBase.position;
        FormWedgeFormation(basePos);
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

        // Assign formation positions to entities and store them
        formationPositions.Clear();
        for (int i = 0; i < n; i++)
        {
            formationPositions[aiEntities[i]] = positions[i];
            AIMgr.inst.HandleMove(new List<Entity> { aiEntities[i] }, positions[i], false);
        }
    }

    private Dictionary<Entity, bool> isBaseAttackMode = new Dictionary<Entity, bool>(); // Class-level dictionary

private void HandleCombatBehavior(Entity opponentBase)
{
    foreach (Entity aiEntity in aiEntities)
    {
        // Skip if entity has no assigned formation position
        if (!formationPositions.ContainsKey(aiEntity)) continue;

        // Ensure isBaseAttackMode is initialized for this entity
        if (!isBaseAttackMode.ContainsKey(aiEntity))
            isBaseAttackMode[aiEntity] = false;

        // Determine attack range
        float range = aiEntity.GetComponentInChildren<WeaponsAspect>()?.weapon.range ?? 600f;

        // Check for enemies within range
        Entity nearestEnemy = FindNearestEnemy(aiEntity);
        if (nearestEnemy != null && Vector3.Distance(aiEntity.position, nearestEnemy.position) < range)
        {
            // Stop moving only if the entity is in base attack mode (after formation move is done)
            if (isBaseAttackMode[aiEntity])
            {
                AIMgr.inst.HandleMove(new List<Entity> { aiEntity }, aiEntity.position, false);
                formationPositions[aiEntity] = aiEntity.position;
            }
            WeaponsMgr.inst.handleWeapon(aiEntity, nearestEnemy);
        }
        else
        {
            // No enemies in range, handle movement
            float distanceToTarget = Vector3.Distance(aiEntity.position, formationPositions[aiEntity]);
            if (!isBaseAttackMode[aiEntity] && distanceToTarget < 100f)
            {
                // Formation position reached, switch to base attack mode
                Debug.Log("Switching to base attack");
                isBaseAttackMode[aiEntity] = true;
                formationPositions[aiEntity] = opponentBase.position;
                AIMgr.inst.HandleMove(new List<Entity> { aiEntity }, opponentBase.position, false);
            }
            else if (isBaseAttackMode[aiEntity])
            {
                // Resume moving toward the base if in base attack mode
                AIMgr.inst.HandleMove(new List<Entity> { aiEntity }, opponentBase.position, false);
            }
        }
    }
}

    private Entity FindNearestEnemy(Entity aiEntity) {
        Entity nearest = null;
        float minDist = float.MaxValue;

        foreach (Entity entity in EntityMgr.inst.entities) {
            if (entity.owner.name != "Ai" && entity.entityClass != EntityClass.Missile) {
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
}