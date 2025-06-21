using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Level3EnemyAI : BaseEnemyAI
{
    private const float BaseDefenseRadiusLevel3 = 1200f;
    private const float DefenderHoldDistanceLevel3 = 150f;
    private const float DefenderOffsetFromBaseLevel3 = 250f;
    private static readonly Vector3 MapCenterLevel3 = Vector3.zero;
    private const float DefenderPositionToleranceSqLevel3 = 25f;

    private List<Entity> _reusableThreatsNearBaseList = new List<Entity>();
    private List<Entity> _reusableDefendersList = new List<Entity>();
    private List<Entity> _reusableAttackersList = new List<Entity>();

    public override void ProcessCombatBehavior(List<Entity> aiEntities)
    {
        if (opponentBase == null && !(aiBases.Count > 0 && aiEntities.Any(e => e != null)))
        {
            if (aiBases.Count == 0) return;
        }

        _reusableDefendersList.Clear();
        _reusableAttackersList.Clear();
        HashSet<Entity> assignedDefenders = new HashSet<Entity>();

        Entity primaryAiBase = null;
        if (aiBases.Count > 0)
        {
            primaryAiBase = aiBases[0];
        }

        if (primaryAiBase != null && aiEntities.Count > 0)
        {
            var defenderTypeConfigs = new Dictionary<EntityType, float>
            {
                { EntityType.DDG51, 0.1f },
                { EntityType.JARIUSV, 0.1f },
                { EntityType.SeaHunter, 0.1f }
            };

            var entitiesByType = aiEntities
                .Where(e => e != null && e.entityType != default(EntityType))
                .GroupBy(e => e.entityType)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var config in defenderTypeConfigs)
            {
                EntityType type = config.Key;
                float percentage = config.Value;

                if (entitiesByType.TryGetValue(type, out List<Entity> unitsOfType))
                {
                    int numToDefend;
                    if (unitsOfType.Count > 0)
                    {
                        int tenPercentOfUnits = Mathf.FloorToInt(unitsOfType.Count * percentage);
                        numToDefend = Mathf.Max(1, tenPercentOfUnits);
                        numToDefend = Mathf.Min(numToDefend, unitsOfType.Count);
                    }
                    else
                    {
                        numToDefend = 0;
                    }

                    for (int i = 0; i < numToDefend; i++)
                    {
                        Entity candidateDefender = unitsOfType[i];
                        if (candidateDefender != null && !assignedDefenders.Contains(candidateDefender))
                        {
                            _reusableDefendersList.Add(candidateDefender);
                            assignedDefenders.Add(candidateDefender);
                        }
                    }
                }
            }
        }

        foreach (Entity entity in aiEntities)
        {
            if (entity != null && !assignedDefenders.Contains(entity))
            {
                _reusableAttackersList.Add(entity);
            }
        }

        if (primaryAiBase != null)
        {
            HandleDefenderLogicLevel3(_reusableDefendersList, primaryAiBase);
        }

        if (opponentBase != null)
        {
            HandleAttackerLogicLevel3(_reusableAttackersList, opponentBase);
        }
    }

    private void HandleDefenderLogicLevel3(List<Entity> defenders, Entity aiBaseToDefend)
    {
        if (defenders.Count == 0 || aiBaseToDefend == null) return;

        Vector3 basePosition = aiBaseToDefend.position;
        float defenseRadiusSq = BaseDefenseRadiusLevel3 * BaseDefenseRadiusLevel3;

        _reusableThreatsNearBaseList.Clear();
        List<Entity> allEnemyTargets = playerEntitiesList;
        foreach (Entity enemy in allEnemyTargets)
        {
            if (enemy == null) continue;
            if ((enemy.position - basePosition).sqrMagnitude < defenseRadiusSq)
            {
                _reusableThreatsNearBaseList.Add(enemy);
            }
        }

        foreach (Entity defender in defenders)
        {
            if (defender == null) continue;
            UnitAI unitAI = defender.GetComponentInChildren<UnitAI>();

            if (_reusableThreatsNearBaseList.Count > 0)
            {
                Entity targetToAttack = null;
                float minSqrDistToDefender = float.MaxValue;
                foreach (Entity threat in _reusableThreatsNearBaseList)
                {
                    float sqrDist = (threat.position - defender.position).sqrMagnitude;
                    if (sqrDist < minSqrDistToDefender)
                    {
                        minSqrDistToDefender = sqrDist;
                        targetToAttack = threat;
                    }
                }

                if (targetToAttack != null)
                {
                    AIMgr.inst.HandleAttackMove(new List<Entity> { defender }, targetToAttack.position, targetToAttack, false);
                }
            }
            else
            {
                Vector3 directionFromBaseToCenter = (MapCenterLevel3 - basePosition).normalized;
                if (directionFromBaseToCenter == Vector3.zero)
                {
                    directionFromBaseToCenter = (aiBaseToDefend.transform.forward != Vector3.zero) ?
                                                 aiBaseToDefend.transform.forward.normalized : Vector3.forward;
                }

                Vector3 defendPosition = basePosition + directionFromBaseToCenter * DefenderOffsetFromBaseLevel3;

                float distanceToDefendPosSq = (defender.position - defendPosition).sqrMagnitude;

                if (distanceToDefendPosSq > DefenderPositionToleranceSqLevel3)
                {
                    AIMgr.inst.HandleMove(new List<Entity> { defender }, defendPosition, false, doneDistanceSq: DefenderPositionToleranceSqLevel3 / 4f);
                }
                else
                {
                    unitAI?.StopAndRemoveAllCommands();

                    if (MapCenterLevel3 != defender.position)
                    {
                        defender.transform.LookAt(MapCenterLevel3);
                    }
                }
            }
        }
    }

    private void HandleAttackerLogicLevel3(List<Entity> attackers, Entity globalTargetOpponentBase)
    {
        if (attackers.Count == 0) return;
        if (opponentBase == null) return;
        AIMgr.inst.HandleAttackMove(attackers, globalTargetOpponentBase.position, globalTargetOpponentBase, false, acquireTarget: true);
    }
}
