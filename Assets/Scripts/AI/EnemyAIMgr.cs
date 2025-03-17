using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EnemyAIMgr : MonoBehaviour 
{
    public int currentLevel = 1;
    public bool drawDebugVisuals = true;
    
    private enum AIState
    {
        FindingBase,
        MovingToBase,
        Attacking // New state
    }
    
    private AIState currentState = AIState.FindingBase;
    private Entity opponentBase;

    private void Start() {
        EntityMgr.inst.OnEntityAdded += HandleEntityAdded;
    }

    public static EnemyAIMgr inst;
    void Awake() {
        inst = this;
    }

    private void OnDestroy() {
        if (EntityMgr.inst != null) {
            EntityMgr.inst.OnEntityAdded -= HandleEntityAdded;
        }
    }

    private void Update() {
        if (currentLevel == 1) {
            UpdateStateMachine();
        }
    }

    private void UpdateStateMachine() {
        switch (currentState) {
            case AIState.FindingBase:
                HandleFindingBaseState();
                break;
            case AIState.MovingToBase:
                HandleMovingToBaseState();
                break;
            case AIState.Attacking: // New case
                HandleAttackingState();
                break;
        }
    }

    private void HandleFindingBaseState() {
        opponentBase = FindOpponentBase();
        if (opponentBase != null) {
            currentState = AIState.MovingToBase;
            MoveAllEntitiesToBase();
        }
    }

    private void HandleMovingToBaseState() {
        // Check if any AI entity has enemies in range
        bool enemiesInRange = GetAIEntities().Any(aiEntity => {
            WeaponsAspect weapon = aiEntity.GetComponentInChildren<WeaponsAspect>();
            return weapon != null && IsEnemyInRange(aiEntity, weapon.weapon.range);
        });

        if (enemiesInRange) {
            currentState = AIState.Attacking;
        } else {
            MoveAllEntitiesToBase();
        }
    }

    private void HandleAttackingState() {
        bool anyEnemyInRange = false;
        List<Entity> aiEntities = GetAIEntities();

        foreach (Entity aiEntity in aiEntities) {
            WeaponAspect weapon = aiEntity.GetComponentInChildren<WeaponsAspect>();
            if (weapon == null) continue;

            float range = weapon.weapon.range;
            Entity nearestEnemy = GetNearestEnemy(aiEntity, range);

            if (nearestEnemy != null) {
                anyEnemyInRange = true;
                AIMgr.inst.HandleMove(new List<Entity> { aiEntity }, nearestEnemy.position, false);
            } else {
                AIMgr.inst.HandleMove(new List<Entity> { aiEntity }, opponentBase.position, false);
            }
        }

        if (!anyEnemyInRange) {
            currentState = AIState.MovingToBase;
            MoveAllEntitiesToBase();
        }
    }

    private bool IsEnemyInRange(Entity aiEntity, float range) {
        return EntityMgr.inst.entities.Any(e => 
            e.owner.name != "Ai" && 
            Vector3.Distance(e.position, aiEntity.position) <= range);
    }

    private Entity GetNearestEnemy(Entity aiEntity, float maxRange) {
        return EntityMgr.inst.entities
            .Where(e => e.owner.name != "Ai" && Vector3.Distance(e.position, aiEntity.position) <= maxRange)
            .OrderBy(e => Vector3.Distance(e.position, aiEntity.position))
            .FirstOrDefault();
    }

    private void MoveAllEntitiesToBase() {
        if (opponentBase == null) {
            currentState = AIState.FindingBase;
            return;
        }

        List<Entity> aiEntities = GetAIEntities();
        AIMgr.inst.HandleMove(aiEntities, opponentBase.position, false);
    }

    private Entity FindOpponentBase() {
        return EntityMgr.inst.entities.FirstOrDefault(e => 
            e.owner.name != "Ai" && e.entityRole == EntityRole.Base);
    }

    private List<Entity> GetAIEntities() {
        return EntityMgr.inst.entities.Where(e => 
            e.owner.name == "Ai" && 
            e.entityClass != EntityClass.Missile && 
            e.creatorsEntity == null && 
            e.entityRole != EntityRole.Base).ToList();
    }

    private void HandleEntityAdded(Entity newEntity) {
        if (newEntity.owner.name != "Ai" || 
            newEntity.entityClass == EntityClass.Missile ||
            newEntity.creatorsEntity != null || 
            newEntity.entityRole == EntityRole.Base) return;

        switch (currentState) {
            case AIState.MovingToBase:
                AIMgr.inst.HandleMove(new List<Entity> { newEntity }, opponentBase.position, false);
                break;
            case AIState.Attacking:
                WeaponsAspect weapon = newEntity.GetComponentInChildren<WeaponsAspect>();
                Entity target = weapon != null ? GetNearestEnemy(newEntity, weapon.weapon.range) : null;
                AIMgr.inst.HandleMove(new List<Entity> { newEntity }, 
                    target?.position ?? opponentBase.position, 
                    false);
                break;
        }
    }
}