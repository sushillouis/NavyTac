using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class Level1EnemyAI : BaseEnemyAI
{
    public float reactionTime;
    public Vector3 moveTill;
    public TrainingState trainingState;

    private bool hasBegunCombat = false;

    public List<Entity> idleEntities = new List<Entity>();
    public List<Entity> retreatingEntities = new List<Entity>();
    public List<Entity> aggressiveEntities = new List<Entity>();

    public override void ResetState()
    {
        base.ResetState();
        hasBegunCombat = false;
        idleEntities.Clear();
        retreatingEntities.Clear();
        aggressiveEntities.Clear();
    }

    public override void ProcessCombatBehavior(List<Entity> allAiEntities)
    {
        Debug.Log($"[AI STATE] Frame Update: Aggressive({aggressiveEntities.Count}), Retreating({retreatingEntities.Count}), Idle({idleEntities.Count})");
        foreach(Entity ent in allAiEntities)
        {
            if(ent != null && ent.isGreyed)
            {
                return;
            }
        }
        if (allAiEntities == null || allAiEntities.Count == 0)
        {
            if (aggressiveEntities.Count == 0 && retreatingEntities.Count == 0 && idleEntities.Count == 0)
            {
                if(hasBegunCombat)
                {
                    Debug.Log("Level1EnemyAI: All units are gone. Resetting combat flag.");
                    hasBegunCombat = false;
                }
            }
            return;
        }

        if (!hasBegunCombat)
        {
            StartCombat(allAiEntities);
            return;
        }
        if (OpenOceanMain.inst.currentTrainingState == TrainingState.Adaptive & ScenarioGenerator.inst.difficultyLevel >= 0.27f)
        {
            AddNewUnits(allAiEntities);

            UpdateRetreatingEntities();
            UpdateAggressiveEntities();
            UpdateIdleEntities();

        }
        else
        {
            return;
        }
        
    }

    private void StartCombat(List<Entity> initialEntities)
    {
        Debug.Log("Level1EnemyAI: Starting new combat behavior processing.");
        hasBegunCombat = true;
        
        aggressiveEntities.AddRange(initialEntities);

        SetReactionTime();
        SetMoveTill();
        
        Debug.Log($"Level1EnemyAI: Reaction time set to {reactionTime}. Move till set to {moveTill}.");
        Debug.Log($"Level1EnemyAI: Commanding {aggressiveEntities.Count} entities to take action after delay.");

        EnemyAIMgr.inst.StartCoroutine(TakeInitialActionAfterDelay(reactionTime, new List<Entity>(aggressiveEntities)));
    }

    private void AddNewUnits(List<Entity> currentEntities)
    {
        foreach (var ent in currentEntities)
        {
            if (ent != null && !idleEntities.Contains(ent) && !aggressiveEntities.Contains(ent) && !retreatingEntities.Contains(ent))
            {
                Debug.Log($"Level1EnemyAI: New unit {ent.name} detected. Adding to idle.");
                idleEntities.Add(ent);
            }
        }
    }

    private void UpdateAggressiveEntities()
    {
        List<Entity> entitiesToRetreat = new List<Entity>();
        
        foreach(var ent in aggressiveEntities)
        {
            if (ent != null && ent.isBeingAttacked)
            {
                entitiesToRetreat.Add(ent);
            }
        }

        foreach(var ent in entitiesToRetreat)
        {
            Debug.Log($"Level1EnemyAI: Aggressive entity {ent.name} is being attacked. Moving to retreating.");
            aggressiveEntities.Remove(ent);
            retreatingEntities.Add(ent);
            if (aiBases.Count > 0 && aiBases[0] != null)
            {
                AIMgr.inst.HandleMove(new List<Entity> { ent }, aiBases[0].transform.position);
            }
        }
    }

    private void UpdateRetreatingEntities()
    {
        List<Entity> entitiesToMakeIdle = new List<Entity>();
        foreach(var ent in retreatingEntities)
        {
            if (ent != null && !ent.isBeingAttacked)
            {
                entitiesToMakeIdle.Add(ent);
            }
        }
        
        foreach(var ent in entitiesToMakeIdle)
        {
            Debug.Log($"Level1EnemyAI: Retreating entity {ent.name} is no longer being attacked. Moving to idle.");
            retreatingEntities.Remove(ent);
            idleEntities.Add(ent);
        }
    }

    private void UpdateIdleEntities()
    {
        if (idleEntities.Count > 0)
        {
            Debug.Log($"Level1EnemyAI: {idleEntities.Count} idle entities are now becoming aggressive.");
            AIMgr.inst.HandleAttackMove(idleEntities, opponentBase.transform.position, null, acquireTarget: true, useLowestCruiseSpeed: true);
            
            aggressiveEntities.AddRange(idleEntities);
            idleEntities.Clear();
        }
    }

    private IEnumerator TakeInitialActionAfterDelay(float delay, List<Entity> entitiesToCommand)
    {
        yield return new WaitForSeconds(delay);

        var validEntities = entitiesToCommand.Where(e => e != null && aggressiveEntities.Contains(e)).ToList();
        
        if (validEntities.Count > 0)
        {
            Debug.Log($"Level1EnemyAI: Initial reaction delay over. Commanding {validEntities.Count} entities.");
            AIMgr.inst.HandleAttackMove(validEntities, opponentBase.transform.position, null, acquireTarget: true, useLowestCruiseSpeed: true);
        }
        else
        {
            Debug.Log("Level1EnemyAI: Initial entities were all lost or began retreating before action could be taken.");
        }
    }

    public void SetReactionTime()
    {
        if (trainingState == TrainingState.Adaptive)
        {
            float difficulty = ScenarioGenerator.inst.difficultyLevel;
            reactionTime = Mathf.Max(0.5f, 30.0f - (difficulty * 45.45f));
        }
        else
        {
            reactionTime = 15.0f;
        }
    }

    public void SetMoveTill()
    {
        if (trainingState == TrainingState.Adaptive)
        {
            float difficulty = ScenarioGenerator.inst.difficultyLevel;
            float t = Mathf.Clamp01(difficulty / 0.33f);
            moveTill = Vector3.Lerp(Vector3.zero, aiBases[0].transform.position, t);
        }
        else
        {
            moveTill = (aiBases[0].transform.position + new Vector3(0, 0, 0f)) / 2f;
        }
    }
}