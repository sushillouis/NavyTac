using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Level2EnemyAI : BaseEnemyAI
{
    private bool _level2CommandsStarted = false;
    private float BatchDelay = 45f;
    private Vector3 _aiBasePosition;
    private List<Entity> _batch1 = new List<Entity>();
    private List<Entity> _batch2 = new List<Entity>();
    private Coroutine _level2Coroutine;

    public override void ProcessCombatBehavior(List<Entity> aiEntities)
    {
        if (opponentBase == null) return;
        if (aiEntities.Count == 0) return;

        if (!_level2CommandsStarted && aiBases.Count > 0)
        {
            _aiBasePosition = aiBases[0].position;
            _level2CommandsStarted = true;

            var validTypes = new HashSet<EntityType>
            {
                EntityType.DDG51,
                EntityType.JARIUSV,
                EntityType.SeaHunter
            };

            var filteredEntities = aiEntities
                .Where(e => e != null && validTypes.Contains(e.entityType))
                .ToList();

            CreateBatches(filteredEntities);

            if (OpenOceanMain.inst.currentTrainingState == TrainingState.Adaptive)
            {
                float diff = GameMgr.inst.difficultyLevel;
                BatchDelay = Mathf.Lerp(60f, 20f, (diff - 0.33f) / (0.66f - 0.33f));
            }

            if (_level2Coroutine != null)
            {
                EnemyAIMgr.inst.StopCoroutine(_level2Coroutine);
            }

            _level2Coroutine = EnemyAIMgr.inst.StartCoroutine(RunLevel2CommandSequence());
        }
    }

    public override void ResetState()
    {
        base.ResetState();
        _level2CommandsStarted = false;
        _aiBasePosition = Vector3.zero;

        _batch1.Clear();
        _batch2.Clear();

        if (_level2Coroutine != null)
        {
            EnemyAIMgr.inst.StopCoroutine(_level2Coroutine);
            _level2Coroutine = null;
        }
    }

    private void CreateBatches(List<Entity> allEntities)
    {
        _batch1.Clear();
        _batch2.Clear();

        foreach (EntityType type in new[] { EntityType.DDG51, EntityType.JARIUSV, EntityType.SeaHunter })
        {
            var entitiesOfType = allEntities.Where(e => e.entityType == type).ToList();
            int half = Mathf.CeilToInt(entitiesOfType.Count / 2f);

            _batch1.AddRange(entitiesOfType.Take(half));
            _batch2.AddRange(entitiesOfType.Skip(half));
        }
    }

    private IEnumerator RunLevel2CommandSequence()
    {
        IssueDirectCommand(_batch1, GetStagingPosition(1000f), true, false);
        yield return new WaitForSeconds(BatchDelay);
        IssueDirectCommand(_batch1, opponentBase.position, true);
        IssueDirectCommand(_batch2, GetStagingPosition(1000f), true, false);

        yield return new WaitForSeconds(BatchDelay * 2);
        IssueDirectCommand(_batch2, opponentBase.position, true);
    }

    private Vector3 GetStagingPosition(float position)
    {
        if (_aiBasePosition == Vector3.zero)
        {
            return new Vector3(position, 0f, position);
        }

        Vector3 stagingDir = (Vector3.zero - _aiBasePosition).normalized;
        Vector3 stagingPos = _aiBasePosition + stagingDir * position;
        return stagingPos;
    }

    private void IssueDirectCommand(List<Entity> entities, Vector3 position, bool isAttackMove, bool towardOpponentBase = true)
    {
        if (entities.Count == 0) return;

        if (isAttackMove)
        {
            if (towardOpponentBase && opponentBase != null)
            {
                AIMgr.inst.HandleAttackMove(entities, position, opponentBase, false, acquireTarget: true);
            }
            else
            {
                foreach (Entity entity in entities)
                {
                    if (entity == null) continue;

                    float minAbsJitter = 0f;
                    float maxAbsJitter = 500f;

                    float randomMagnitudeX = Random.Range(minAbsJitter, maxAbsJitter);
                    float offsetX = (Random.value < 0.5f) ? -randomMagnitudeX : randomMagnitudeX;

                    float randomMagnitudeZ = Random.Range(minAbsJitter, maxAbsJitter);
                    float offsetZ = (Random.value < 0.5f) ? -randomMagnitudeZ : randomMagnitudeZ;

                    Vector3 jitteredPosition = position + new Vector3(offsetX, 0, offsetZ);

                    AIMgr.inst.HandleAttackMove(new List<Entity> { entity }, jitteredPosition, null, false, acquireTarget: true);
                }
            }
        }
        else
        {
            foreach (Entity entity in entities)
            {
                if (entity == null) continue;

                float minAbsJitter = 0f;
                float maxAbsJitter = 500f;

                float randomMagnitudeX = Random.Range(minAbsJitter, maxAbsJitter);
                float offsetX = (Random.value < 0.5f) ? -randomMagnitudeX : randomMagnitudeX;

                float randomMagnitudeZ = Random.Range(minAbsJitter, maxAbsJitter);
                float offsetZ = (Random.value < 0.5f) ? -randomMagnitudeZ : randomMagnitudeZ;

                Vector3 jitteredPosition = position + new Vector3(offsetX, 0, offsetZ);

                AIMgr.inst.HandleMove(new List<Entity> { entity }, jitteredPosition, false);
            }
        }
    }
}
