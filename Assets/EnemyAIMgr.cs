using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public enum AiState
{
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

public enum AiAttackLevel
{
    Low,
    Medium,
    High
}

public class EnemyAIMgr : MonoBehaviour
{
    // Add level configuration
    [Header("Game Levels")]
    public int currentLevel = 1;
    public float levelTransitionDistance = 5000f;

    [System.Serializable]
    public class AiData
    {
        public Entity entity;
        public AiState state;
        public Vector3 destination;
        
        // Scout parameters
        public Vector3 scoutCenter;
        public float scoutRadius = 4500f;
        public float detectionRadius = 1000f;
        public Entity detectedTarget;
        public bool isSelectedScout;
        public int assignedQuadrant;

        // Orbit parameters
        public float orbitRadius = 400f;
        public float safeDistance = 500f;
        public float avoidanceRadius = 200f;
        public Entity orbitTarget;
        public float currentOrbitAngle;
        public Vector3 orbitalVelocity;
    }

    // Configuration    
    public AiAttackLevel attackLevel;
    public float maxScoutRadius = 4000f;
    public int maxScouts = 4;
    public float targetSwitchDistance = 2000f;
    public float minScoutSpeed = 10f;
    public int fixedSeed = 12345;
    public float orbitSmoothTime = 2f;
    public float headingSmoothness = 2f;
    public float avoidanceWeight = 3f;

    // Runtime data
    public List<AiData> potentialScouts = new List<AiData>();
    public List<Vector3> scoutRegions = new List<Vector3>();

    IEnumerator Start()
    {
        Random.InitState(fixedSeed);
        InitializeScoutRegions();
        
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
        scoutRegions.Add(new Vector3(0, 0, maxScoutRadius));
        scoutRegions.Add(new Vector3(-maxScoutRadius, 0, 0));
        scoutRegions.Add(new Vector3(0, 0, -maxScoutRadius));
        scoutRegions.Add(new Vector3(maxScoutRadius, 0, 0));
    }

    void UpdateScoutSelection()
    {
        var eligibleScouts = potentialScouts.FindAll(a => a.entity.maxSpeed >= minScoutSpeed);
        eligibleScouts.Sort((a, b) => b.entity.maxSpeed.CompareTo(a.entity.maxSpeed));
        
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
        scout.scoutRadius = Mathf.Min(maxScoutRadius * 0.8f, 4000f);
        scout.detectionRadius = 500f;
        scout.assignedQuadrant = quadrantIndex;
        scout.destination = GetValidWaterPoint(scout);
        
        AIMgr.inst.HandleMove(new List<Entity>{scout.entity}, scout.destination, false, false, true);
    }

    void HandleEntityAdded(Entity entity)
    {
        if(entity.owner.name == "Ai")
        {
            var newAI = new AiData
            {
                entity = entity,
                state = currentLevel == 1 ? AiState.Move : AiState.Idle, // Force Move state in Level 1
                isSelectedScout = false,
                assignedQuadrant = -1,
                orbitTarget = null,
                orbitalVelocity = Vector3.zero,
                detectionRadius = currentLevel == 1 ? 1500f : 1000f
            };

            if(currentLevel == 1)
            {
                attackLevel = AiAttackLevel.Low;
                newAI.destination = newAI.entity.position + newAI.entity.transform.forward * 10000f;
                AIMgr.inst.HandleDumbMove(new List<Entity> { newAI.entity }, newAI.destination, false);
                newAI.state = AiState.Move;
            }

            potentialScouts.Add(newAI);
            if(currentLevel > 1) UpdateScoutSelection();
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
        // Global detection check for all states
        aiData.detectedTarget = FindNearestEntity(aiData);
        if (aiData.detectedTarget != null && 
            Vector3.Distance(aiData.entity.position, aiData.detectedTarget.position) <= aiData.detectionRadius)
        {
            if (aiData.state != AiState.Attack && aiData.state != AiState.Dead)
            {
                Debug.Log("Enemy detected: " + aiData.detectedTarget.name);
                aiData.state = AiState.Attack;
                attackLevel = AiAttackLevel.Low;
                aiData.entity.desiredSpeed = 0;
                aiData.entity.GetComponentInChildren<UnitAI>().StopAndRemoveAllCommands();
            }
        }

        switch (aiData.state)
        {
            case AiState.Move:
                HandleMoveState(aiData);
                break;
            case AiState.Scout:
                HandleScouting(aiData);
                break;
            case AiState.Orbit:
                HandleOrbitState(aiData);
                break;
            case AiState.Idle:
                break;
            case AiState.Chase:
                HandleChase(aiData);
                break;
            case AiState.Attack:
                HandleAttack(aiData);
                break;
            case AiState.Patrol:
            case AiState.Flee:
            case AiState.Dead:
                break;
        }
    }
}

    void HandleMoveState(AiData aiData)
    {
        aiData.detectedTarget = FindNearestEntity(aiData);
        
        if (aiData.detectedTarget != null && 
            Vector3.Distance(aiData.entity.position, aiData.detectedTarget.position) <= aiData.detectionRadius)
        {
            aiData.state = AiState.Attack;
            attackLevel = AiAttackLevel.Low;
            return;
        }

        // Continue moving straight if no targets
        AIMgr.inst.HandleDumbMove(new List<Entity> { aiData.entity }, aiData.destination, false);
    }

    void HandleScouting(AiData aiData)
    {
        aiData.detectedTarget = FindNearestEntity(aiData);
        
        if (aiData.detectedTarget != null)
        {
            float distance = Vector3.Distance(aiData.entity.position, aiData.detectedTarget.position);
            if (distance <= targetSwitchDistance)
            {
                float angleStep = 360f / potentialScouts.Count;
                for (int i = 0; i < potentialScouts.Count; i++)
                {
                    var scout = potentialScouts[i];
                    scout.state = AiState.Chase;
                    scout.orbitTarget = aiData.detectedTarget;
                    scout.currentOrbitAngle = i * angleStep;
                }
                return;
            }
        }

        if (Vector3.Distance(aiData.entity.position, aiData.destination) < 100f)
        {
            aiData.destination = GetValidWaterPoint(aiData);
        }

        AIMgr.inst.HandleMove(new List<Entity>{aiData.entity}, aiData.destination, false, false, true);
    }

    void HandleOrbitState(AiData aiData)
    {
        if (aiData.orbitTarget == null || 
            Vector3.Distance(aiData.entity.position, aiData.orbitTarget.position) > aiData.detectionRadius)
        {
            ReturnToScouting(aiData);
            return;
        }

        Vector3 toTarget = aiData.orbitTarget.position - aiData.entity.position;
        float currentDistance = toTarget.magnitude;
        
        float angularVelocity = (aiData.entity.speed / Mathf.Max(aiData.orbitRadius, 1f)) * Mathf.Rad2Deg;
        aiData.currentOrbitAngle += angularVelocity * Time.deltaTime;

        Vector3 orbitalOffset = Quaternion.Euler(0, aiData.currentOrbitAngle, 0) * Vector3.forward * aiData.orbitRadius;
        Vector3 desiredPosition = aiData.orbitTarget.position + orbitalOffset;
        
        Vector3 toDesired = (desiredPosition - aiData.entity.position).normalized;
        float targetHeading = Vector3.SignedAngle(Vector3.forward, toDesired, Vector3.up);
        
        aiData.entity.desiredHeading = Mathf.LerpAngle(
            aiData.entity.desiredHeading,
            targetHeading,
            Time.deltaTime * headingSmoothness
        );

        float distanceError = currentDistance - aiData.safeDistance;
        aiData.entity.desiredSpeed = Mathf.Clamp(
            aiData.entity.maxSpeed * (1 + distanceError / aiData.safeDistance),
            aiData.entity.minSpeed,
            aiData.entity.maxSpeed
        );

        AvoidObstacles(aiData);

        Entity newTarget = FindNearestEntity(aiData);
        if (newTarget != null && newTarget != aiData.orbitTarget)
        {
            float newTargetDistance = Vector3.Distance(aiData.entity.position, newTarget.position);
            if (newTargetDistance < currentDistance * 0.8f)
            {
                aiData.orbitTarget = newTarget;
                aiData.currentOrbitAngle = 0f;
            }
        }
    }

    void AvoidObstacles(AiData aiData)
    {
        Collider[] hits = Physics.OverlapSphere(aiData.entity.position, aiData.avoidanceRadius);
        Vector3 avoidanceDirection = Vector3.zero;
        
        foreach (Collider hit in hits)
        {
            if (hit.gameObject == aiData.entity.gameObject) continue;
            
            Vector3 toHit = aiData.entity.position - hit.transform.position;
            float weight = 1 - Mathf.Clamp01(toHit.magnitude / aiData.avoidanceRadius);
            avoidanceDirection += toHit.normalized * weight;
        }
        
        if (avoidanceDirection.magnitude > 0)
        {
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

    void ReturnToScouting(AiData aiData)
    {
        aiData.state = AiState.Scout;
        aiData.destination = GetValidWaterPoint(aiData);
        aiData.orbitTarget = null;
    }

    Vector3 GetValidWaterPoint(AiData scout)
    {
        const int maxAttempts = 10;
        for (int i = 0; i < maxAttempts; i++)
        {
            Vector3 candidate = GetStrategicScoutPoint(scout);
            Vector3 rayStart = candidate + Vector3.up * 100f;
            RaycastHit hit;
            
            if (Physics.Raycast(rayStart, Vector3.down, out hit, 200f, LayerMask.GetMask("Ocean")))
            {
                return hit.point;
            }
        }
        return scout.scoutCenter;
    }

    Vector3 GetStrategicScoutPoint(AiData scout)
    {
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

        foreach (Entity entity in EntityMgr.inst.entities)
        {
            if (entity == aiData.entity || entity.owner.name == "Ai") 
                continue;

            float distance = Vector3.Distance(aiData.entity.position, entity.position);
            if (distance < aiData.detectionRadius && distance < closestDistance)
            {
                nearest = entity;
                closestDistance = distance;
            }
        }

        return nearest;
    }

    void HandleChase(AiData aiData)
    {
        if (aiData.detectedTarget == null || aiData.detectedTarget.speed == 0)
        {
            aiData.state = AiState.Scout;
            aiData.destination = GetValidWaterPoint(aiData);
            return;
        }
    }

    void HandleAttack(AiData aiData)
    {
        switch(attackLevel)
        {
            case AiAttackLevel.Low:
                AiLowAttack(aiData);
                break;
            case AiAttackLevel.Medium:
                AiMediumAttack(aiData);
                break;
            case AiAttackLevel.High:
                AiHighAttack(aiData);
                break;
        }
    }
    
    void AiLowAttack(AiData aiData)
    {
        Debug.Log("Low attack");
        WeaponsMgr.inst.handleWeapon(aiData.entity,FindNearestEntity(aiData), WeaponBehaviors.AirInterceptor);
       
    }

    void AiMediumAttack(AiData aiData)
    {
        // Implement medium complexity attack patterns here
    }

    void AiHighAttack(AiData aiData)
    {
        // Implement advanced attack patterns here
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        foreach (var region in scoutRegions)
        {
            Gizmos.DrawWireSphere(region, maxScoutRadius * 0.8f);
        }
    }
}