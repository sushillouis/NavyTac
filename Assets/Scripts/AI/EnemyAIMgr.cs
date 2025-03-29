using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EnemyAIMgr : MonoBehaviour
{
    public int currentLevel = 1;
    public static EnemyAIMgr inst;

    private Entity oldBase = null;
    // Removed lastMoveTargets to fix move command re-issuing

    void Awake() => inst = this;

    private void Update()
    {
        if (currentLevel == 1)
            HandleLevel1Behavior();
    }

    private void HandleLevel1Behavior()
    {
        Entity opponentBase = FindOpponentBase();

        if (opponentBase == null)
        {
            Debug.Log("AI WON");
            return;
        }

        List<Entity> aiEntities = GetAIEntities();
        if (aiEntities.Count == 0) return;

        if (oldBase != opponentBase)
            oldBase = opponentBase;

        HandleCombatBehavior(opponentBase, aiEntities);
    }

    private void HandleCombatBehavior(Entity opponentBase, List<Entity> aiEntities)
    {
        // Null-check opponentBase to prevent destroyed reference errors
        if (opponentBase == null) return;

        foreach (Entity aiEntity in aiEntities)
        {
            float range = aiEntity.GetComponentInChildren<WeaponsAspect>()?.weapon.range ?? 600f;
            Entity nearestEnemy = FindNearestEnemy(aiEntity, range);

            if (nearestEnemy != null)
            {
                UnitAI unitAI = aiEntity.GetComponentInChildren<UnitAI>();
                unitAI?.StopAndRemoveAllCommands();
                WeaponsMgr.inst.handleWeapon(aiEntity, nearestEnemy);
            }
            else
            {
                // Always issue move command when no enemies are in range
                AIMgr.inst.HandleMove(new List<Entity> { aiEntity }, opponentBase.position, false);
            }
        }
    }

    private Entity FindNearestEnemy(Entity aiEntity, float range)
    {
        Vector3 aiPos = aiEntity.position;
        float sqrRange = range * range;

        return EntityMgr.inst.entities
            .Where(e => e != null && 
                        e.owner != null && 
                        !e.owner.name.Equals("Ai", System.StringComparison.OrdinalIgnoreCase) && // Case-insensitive check
                        e.entityClass != EntityClass.Missile)
            .Where(e => (e.position - aiPos).sqrMagnitude < sqrRange)
            .OrderBy(e => (e.position - aiPos).sqrMagnitude)
            .FirstOrDefault();
    }

    private Entity FindOpponentBase()
    {
        return EntityMgr.inst.entities
            .FirstOrDefault(e => e != null && 
                                  e.owner != null && 
                                  !e.owner.name.Equals("Ai", System.StringComparison.OrdinalIgnoreCase) && 
                                  e.entityRole == EntityRole.Base);
    }

    private List<Entity> GetAIEntities()
    {
        return EntityMgr.inst.entities
            .Where(e => e != null && 
                        e.owner != null && 
                        e.owner.name.Equals("Ai", System.StringComparison.OrdinalIgnoreCase) && 
                        e.entityClass != EntityClass.Missile)
            .ToList();
    }
}