using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AiState {
    Idle,
    Move,
    Attack,
    Flee,
    Scout,
    Patrol,
    Chase,
    Dead,
    Orbit
}

public enum AiAttackLevel {
    Low,
    Medium,
    High
}

public class EnemyAIMgr : MonoBehaviour 
{
    // Constants
    private const float MIN_DESTINATION_DISTANCE = 100f;
    private const float MAP_BOUNDARY = 9000f;
    private const float DETECTION_RATIO_THRESHOLD = 0.8f;
    private const int SCOUT_REGIONS_COUNT = 4;
    private const float RAYCAST_DISTANCE = 200f;

    [Header("Game Levels")]
    public int currentLevel = 1;
    public float levelTransitionDistance = 5000f;

    [System.Serializable]
    public class AiData {
        public Entity entity;
        public AiState state;
        public Vector3 destination;
        public Vector3 moveDirection;

        [System.Serializable]
        public class ScoutParameters {
            public Vector3 center;
            public float radius = 4500f;
            public float detectionRadius = 1000f;
            public bool isSelected;
            public int assignedQuadrant;
        }

        [System.Serializable]
        public class OrbitParameters {
            public float radius = 400f;
            public float safeDistance = 500f;
            public float avoidanceRadius = 200f;
            public Entity target;
            public float currentAngle;
            public Vector3 velocity;
        }

        public ScoutParameters scoutParams = new ScoutParameters();
        public OrbitParameters orbitParams = new OrbitParameters();
        public Entity detectedTarget;
    }

    [Header("AI Configuration")]
    public AiAttackLevel attackLevel;
    public float maxScoutRadius = 4000f;
    public int maxScouts = 4;
    public float targetSwitchDistance = 2000f;
    public float minScoutSpeed = 10f;
    public int fixedSeed = 12345;
    public float orbitSmoothTime = 2f;
    public float headingSmoothness = 2f;
    public float avoidanceWeight = 3f;

    [Header("Formation Settings")]
    public float attackFormationRadius = 400f;
    public FormationType attackFormationType = FormationType.Circle;

    [Header("Runtime Data")]
    public List<AiData> potentialScouts = new List<AiData>();
    public List<Vector3> scoutRegions = new List<Vector3>();
    private Dictionary<Entity, List<Entity>> activeAttackGroups = new Dictionary<Entity, List<Entity>>();

    private void Awake() {
        InitializeScoutRegions();
        Random.InitState(fixedSeed);
    }

    IEnumerator Start() {
        while (EntityMgr.inst == null)
            yield return null;

        EntityMgr.inst.OnEntityAdded += HandleEntityAdded;
        EntityMgr.inst.OnEntityRemoved += HandleEntityRemoved;
    }

    void OnDisable() {
        if (EntityMgr.inst != null) {
            EntityMgr.inst.OnEntityAdded -= HandleEntityAdded;
            EntityMgr.inst.OnEntityRemoved -= HandleEntityRemoved;
        }
    }

    void Update() {
        foreach (var aiData in potentialScouts) {
            UpdateDetection(aiData);
            HandleAIState(aiData);
        }
    }

    private void InitializeScoutRegions() {
        scoutRegions.Clear();
        for (int i = 0; i < SCOUT_REGIONS_COUNT; i++) {
            float angle = i * 90f;
            scoutRegions.Add(new Vector3(
                maxScoutRadius * Mathf.Cos(angle * Mathf.Deg2Rad),
                0,
                maxScoutRadius * Mathf.Sin(angle * Mathf.Deg2Rad)
            ));
        }
    }

    private void HandleEntityAdded(Entity entity) {
        if (entity.owner.name == "Ai" && entity.entityClass != EntityClass.Missile && entity.creatorsEntity == null) {
            var newAI = CreateAiData(entity);
            potentialScouts.Add(newAI);
          
            if (currentLevel > 1)
                UpdateScoutSelection();
        }
    }

    private AiData CreateAiData(Entity entity) {
        Vector3 initialDirection = entity.transform.forward.normalized;
        initialDirection.y = 0;

        return new AiData {
            entity = entity,
            state = currentLevel == 1 ? AiState.Move : AiState.Idle,
            destination = GetInitialStraightDestination(entity.position, initialDirection),
            moveDirection = initialDirection,
            scoutParams = { detectionRadius = 1000f }
        };
    }

    private Vector3 GetInitialStraightDestination(Vector3 position, Vector3 direction) {
        return position + direction.normalized * 500f;
    }

    private void UpdateScoutSelection() {
        var eligibleScouts = GetEligibleScouts();
        for (int i = 0; i < potentialScouts.Count; i++) {
            bool shouldBeScout = i < eligibleScouts.Count && i < maxScouts;
            var scout = potentialScouts[i];

            scout.scoutParams.isSelected = shouldBeScout;
          
            if (shouldBeScout && scout.state != AiState.Scout)
                InitializeScout(scout, i % scoutRegions.Count);
            else if (!shouldBeScout && scout.state == AiState.Scout)
                scout.state = AiState.Idle;
        }
    }

    private List<AiData> GetEligibleScouts() {
        var eligible = potentialScouts.FindAll(a => a.entity.maxSpeed >= minScoutSpeed);
        eligible.Sort((a, b) => b.entity.maxSpeed.CompareTo(a.entity.maxSpeed));
        return eligible;
    }

    private void InitializeScout(AiData scout, int quadrantIndex) {
        scout.state = AiState.Scout;
        scout.scoutParams.center = scoutRegions[quadrantIndex];
        scout.scoutParams.radius = Mathf.Min(maxScoutRadius * 0.8f, 4000f);
        scout.scoutParams.assignedQuadrant = quadrantIndex;
        scout.destination = GetValidWaterPoint(scout);
      
        AIMgr.inst.HandleMove(new List<Entity>{scout.entity}, scout.destination, false, false, true);
    }

    private void UpdateDetection(AiData aiData) {
        aiData.detectedTarget = FindNearestEntity(aiData);
        
        if (aiData.detectedTarget != null && 
            Vector3.Distance(aiData.entity.position, aiData.detectedTarget.position) <= aiData.scoutParams.detectionRadius) {
            if (aiData.state != AiState.Attack && aiData.state != AiState.Dead) {
                TransitionToAttackState(aiData);
            }
        }
        else if (aiData.state == AiState.Attack) {
            ReturnToMoveState(aiData);
        }
    }

    private void TransitionToAttackState(AiData aiData) {
        Debug.Log($"Enemy detected: {aiData.detectedTarget.name}");
        aiData.state = AiState.Attack;
        attackLevel = AiAttackLevel.Low;

        if (!activeAttackGroups.ContainsKey(aiData.detectedTarget)) {
            activeAttackGroups[aiData.detectedTarget] = new List<Entity>();
        }

        if (!activeAttackGroups[aiData.detectedTarget].Contains(aiData.entity)) {
            activeAttackGroups[aiData.detectedTarget].Add(aiData.entity);
        }

        UpdateAttackFormation(aiData.detectedTarget);
    }

    private void UpdateAttackFormation(Entity target) {
        if (!activeAttackGroups.ContainsKey(target)) return;

        List<Entity> attackGroup = activeAttackGroups[target];
        if (attackGroup.Count == 0) return;

        AIMgr.inst.HandleMove(
            attackGroup,
            target.position,
            false,
            false,
            true,
            attackFormationType,
            attackFormationRadius
        );
    }

    private void HandleAIState(AiData aiData) {
        switch (aiData.state) {
            case AiState.Move:   HandleMoveState(aiData); break;
            case AiState.Scout:  HandleScouting(aiData);  break;
            case AiState.Orbit:  HandleOrbitState(aiData); break;
            case AiState.Chase:  HandleChase(aiData);     break;
            case AiState.Attack: HandleAttack(aiData);    break;
        }
    }

    private void HandleMoveState(AiData aiData) {
        if (aiData.detectedTarget != null) return;

        if (Vector3.Distance(aiData.entity.position, aiData.destination) < MIN_DESTINATION_DISTANCE) {
            aiData.destination = GetNextStraightDestination(aiData);
        }

        AIMgr.inst.HandleMove(new List<Entity> { aiData.entity }, aiData.destination, false);
    }

    private Vector3 GetNextStraightDestination(AiData aiData) {
        Vector3 desiredDestination = aiData.entity.position + aiData.moveDirection.normalized * 500f;
        
        Vector3 clampedDestination = new Vector3(
            Mathf.Clamp(desiredDestination.x, -MAP_BOUNDARY, MAP_BOUNDARY),
            0,
            Mathf.Clamp(desiredDestination.z, -MAP_BOUNDARY, MAP_BOUNDARY)
        );

        bool hitXBoundary = Mathf.Abs(desiredDestination.x - clampedDestination.x) > 0.1f;
        bool hitZBoundary = Mathf.Abs(desiredDestination.z - clampedDestination.z) > 0.1f;

        if (hitXBoundary) aiData.moveDirection.x *= -1;
        if (hitZBoundary) aiData.moveDirection.z *= -1;

        return clampedDestination;
    }

    private void ReturnToMoveState(AiData aiData) {
        LeaveAttackGroup(aiData);
        aiData.state = AiState.Move;
        aiData.destination = GetNextStraightDestination(aiData);
        aiData.entity.desiredSpeed = aiData.entity.maxSpeed;
        aiData.entity.GetComponentInChildren<UnitAI>().StopAndRemoveAllCommands();
    }

    private void HandleScouting(AiData aiData) {
        if (ShouldSwitchToChase(aiData)) {
            TransitionAllScoutsToChase(aiData.detectedTarget);
            return;
        }

        if (Vector3.Distance(aiData.entity.position, aiData.destination) < MIN_DESTINATION_DISTANCE) {
            aiData.destination = GetValidWaterPoint(aiData);
        }

        AIMgr.inst.HandleMove(new List<Entity>{aiData.entity}, aiData.destination, false, false, true);
    }

    private bool ShouldSwitchToChase(AiData aiData) {
        return aiData.detectedTarget != null && 
               Vector3.Distance(aiData.entity.position, aiData.detectedTarget.position) <= targetSwitchDistance;
    }

    private void TransitionAllScoutsToChase(Entity target) {
        float angleStep = 360f / potentialScouts.Count;
        for (int i = 0; i < potentialScouts.Count; i++) {
            var scout = potentialScouts[i];
            scout.state = AiState.Chase;
            scout.orbitParams.target = target;
            scout.orbitParams.currentAngle = i * angleStep;
        }
    }

    private Vector3 GetValidWaterPoint(AiData scout) {
        for (int i = 0; i < 10; i++) {
            var candidate = GetStrategicScoutPoint(scout);
            if (IsValidWaterPosition(candidate)) 
                return candidate;
        }
        return scout.scoutParams.center;
    }

    private bool IsValidWaterPosition(Vector3 position) {
        var rayStart = position + Vector3.up * 100f;
        return Physics.Raycast(rayStart, Vector3.down, RAYCAST_DISTANCE, LayerMask.GetMask("Ocean"));
    }

    private Vector3 GetStrategicScoutPoint(AiData scout) {
        Vector2 randomCircle = Random.insideUnitCircle * scout.scoutParams.radius;
        return scout.scoutParams.center + new Vector3(
            randomCircle.x * 0.8f,
            0,
            randomCircle.y * 0.8f
        );
    }

    private Entity FindNearestEntity(AiData aiData) {
        Entity nearest = null;
        float closestDistance = Mathf.Infinity;

        foreach (Entity entity in EntityMgr.inst.entities) {
            if (entity == aiData.entity || entity.owner.name == "Ai" || WeaponsMgr.inst.weapons.Contains(entity)||entity.creatorsEntity!=null) continue;

            float distance = Vector3.Distance(aiData.entity.position, entity.position);
            if (distance < aiData.scoutParams.detectionRadius && distance < closestDistance) {
                nearest = entity;
                closestDistance = distance;
            }
        }
        return nearest;
    }

    private void HandleOrbitState(AiData aiData) {
        if (aiData.orbitParams.target == null) {
            ReturnToScouting(aiData);
            return;
        }

        UpdateOrbitPosition(aiData);
        UpdateOrbitHeading(aiData);
        AdjustOrbitSpeed(aiData);
        AvoidObstacles(aiData);
        CheckForBetterTarget(aiData);
    }

    private void UpdateOrbitPosition(AiData aiData) {
        float angularVelocity = (aiData.entity.speed / aiData.orbitParams.radius) * Mathf.Rad2Deg;
        aiData.orbitParams.currentAngle += angularVelocity * Time.deltaTime;

        Vector3 orbitalOffset = Quaternion.Euler(0, aiData.orbitParams.currentAngle, 0) 
                              * Vector3.forward 
                              * aiData.orbitParams.radius;
      
        aiData.destination = aiData.orbitParams.target.position + orbitalOffset;
    }

    private void UpdateOrbitHeading(AiData aiData) {
        Vector3 toDesired = (aiData.destination - aiData.entity.position).normalized;
        float targetHeading = Vector3.SignedAngle(Vector3.forward, toDesired, Vector3.up);
      
        aiData.entity.desiredHeading = Mathf.LerpAngle(
            aiData.entity.desiredHeading,
            targetHeading,
            Time.deltaTime * headingSmoothness
        );
    }

    private void AdjustOrbitSpeed(AiData aiData) {
        float currentDistance = Vector3.Distance(aiData.entity.position, aiData.orbitParams.target.position);
        float distanceError = currentDistance - aiData.orbitParams.safeDistance;
      
        aiData.entity.desiredSpeed = Mathf.Clamp(
            aiData.entity.maxSpeed * (1 + distanceError / aiData.orbitParams.safeDistance),
            aiData.entity.minSpeed,
            aiData.entity.maxSpeed
        );
    }

    private void CheckForBetterTarget(AiData aiData) {
        Entity newTarget = FindNearestEntity(aiData);
        if (newTarget != null && newTarget != aiData.orbitParams.target) {
            float newDistance = Vector3.Distance(aiData.entity.position, newTarget.position);
            if (newDistance < Vector3.Distance(aiData.entity.position, aiData.orbitParams.target.position) * DETECTION_RATIO_THRESHOLD) {
                aiData.orbitParams.target = newTarget;
                aiData.orbitParams.currentAngle = 0f;
            }
        }
    }

    private void AvoidObstacles(AiData aiData) {
        Collider[] hits = Physics.OverlapSphere(aiData.entity.position, aiData.orbitParams.avoidanceRadius);
        Vector3 avoidanceDirection = Vector3.zero;

        foreach (Collider hit in hits) {
            if (hit.gameObject == aiData.entity.gameObject) continue;
          
            Vector3 toHit = aiData.entity.position - hit.transform.position;
            float weight = 1 - Mathf.Clamp01(toHit.magnitude / aiData.orbitParams.avoidanceRadius);
            avoidanceDirection += toHit.normalized * weight;
        }

        if (avoidanceDirection.magnitude > 0) {
            Vector3 currentDirection = aiData.entity.transform.forward;
            Vector3 blendedDirection = Vector3.Lerp(
                currentDirection,
                avoidanceDirection.normalized,
                Time.deltaTime * avoidanceWeight
            );
          
            aiData.entity.desiredHeading = Vector3.SignedAngle(
                Vector3.forward, 
                blendedDirection.normalized, 
                Vector3.up
            );
        }
    }

    private void HandleChase(AiData aiData) {
        if (aiData.detectedTarget == null || aiData.detectedTarget.speed == 0)
            ReturnToScouting(aiData);
    }

    private void ReturnToScouting(AiData aiData) {
        aiData.state = AiState.Scout;
        aiData.destination = GetValidWaterPoint(aiData);
        aiData.orbitParams.target = null;
    }

    private void HandleAttack(AiData aiData) {
        if (aiData.detectedTarget == null || 
            Vector3.Distance(aiData.entity.position, aiData.detectedTarget.position) > aiData.scoutParams.detectionRadius) {
            LeaveAttackGroup(aiData);
            aiData.state = AiState.Move;
            aiData.destination = GetNextStraightDestination(aiData);
            return;
        }

        if (activeAttackGroups.ContainsKey(aiData.detectedTarget)) {
            UpdateAttackFormation(aiData.detectedTarget);
        }

        switch (attackLevel) {
            case AiAttackLevel.Low:
                WeaponsMgr.inst.handleWeapon(aiData.entity, aiData.detectedTarget, WeaponBehaviors.AirInterceptor);
                break;
            case AiAttackLevel.Medium:
                // Implement medium attack logic
                break;
            case AiAttackLevel.High:
                // Implement advanced attack logic
                break;
        }
    }

    private void LeaveAttackGroup(AiData aiData) {
        if (aiData.detectedTarget != null && activeAttackGroups.ContainsKey(aiData.detectedTarget)) {
            activeAttackGroups[aiData.detectedTarget].Remove(aiData.entity);
            if (activeAttackGroups[aiData.detectedTarget].Count == 0) {
                activeAttackGroups.Remove(aiData.detectedTarget);
            }
        }
    }

    void OnDrawGizmosSelected() {
        Gizmos.color = Color.cyan;
        foreach (var region in scoutRegions) {
            Gizmos.DrawWireSphere(region, maxScoutRadius * 0.8f);
        }
    }

    private void HandleEntityRemoved(Entity entity) {
        potentialScouts.RemoveAll(ai => ai.entity == entity);
        foreach (var group in activeAttackGroups.Values) {
            group.Remove(entity);
        }
    }
}