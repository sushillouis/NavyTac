using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Level2EnemyAI : BaseEnemyAI
{
    [Header("Tuning")]
    public float reactionTime;
    public Vector3 moveTill;
    public TrainingState trainingState;

    [Header("Capture State")]
    public bool neutralBaseCaptured = false;

    private bool hasBegunCombat = false;
    private bool processedCapture = false;

    public List<Entity> idleEntities = new List<Entity>();
    public List<Entity> retreatingEntities = new List<Entity>();
    public List<Entity> aggressiveEntities = new List<Entity>();
    public List<Entity> capturingEntities = new List<Entity>();

    public override void ProcessCombatBehavior(List<Entity> allAiEntities)
    {
        Debug.Log($"[AI STATE] Frame Update: Aggressive({aggressiveEntities.Count}), Retreating({retreatingEntities.Count}), Idle({idleEntities.Count}), Capturing({capturingEntities.Count})");

        // Early out if any entity is greyed
        foreach (Entity ent in allAiEntities)
        {
            if (ent != null && ent.isGreyed) return;
        }

        // Handle empty list + reset
        if (allAiEntities == null || allAiEntities.Count == 0)
        {
            if (aggressiveEntities.Count == 0 && retreatingEntities.Count == 0 && idleEntities.Count == 0 && capturingEntities.Count == 0)
            {
                if (hasBegunCombat)
                {
                    Debug.Log("Level2EnemyAI: All units are gone. Resetting combat flag.");
                    hasBegunCombat = false;
                    processedCapture = false;
                    neutralBaseCaptured = false;
                }
            }
            return;
        }

        // Start once
        if (!hasBegunCombat)
        {
            StartCombat(allAiEntities);
            return;
        }

       
            AddNewUnits(allAiEntities);

            // If neutral base just got captured, convert capturing -> idle once.
            if (!processedCapture && (neutralBaseCaptured || IsNeutralCaptured()))
            {
                HandleNeutralBaseCaptured();
            }

            UpdateRetreatingEntities();
            UpdateAggressiveEntities();
            UpdateIdleEntities();
            UpdateCapturingEntities(); 
    }

    private void StartCombat(List<Entity> initialEntities)
    {
        Debug.Log("Level2EnemyAI: Starting new combat behavior processing.");
        hasBegunCombat = true;
        processedCapture = false;

        // Split initial entities between capturing and aggressive
        // Set capturingPercentage between 10% and 40% based on difficulty level (linear interpolation between 0.33 and 0.66)
        float minPercent = 0.10f;
        float maxPercent = 0.40f;
        float minDifficulty = 0.33f;
        float maxDifficulty = 0.66f;
        float difficulty = ScenarioGenerator.inst.difficultyLevel; // assumed 0..1
        float t = Mathf.InverseLerp(minDifficulty, maxDifficulty, Mathf.Clamp01(difficulty));
        float capturingPercentage = Mathf.Lerp(minPercent, maxPercent, Mathf.Clamp01(t));
        int capturingCount = Mathf.RoundToInt(initialEntities.Count * capturingPercentage);

        var shuffled = new List<Entity>(initialEntities);
        for (int i = 0; i < shuffled.Count; i++)
        {
            int j = Random.Range(i, shuffled.Count);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        capturingEntities.AddRange(shuffled.Take(capturingCount).Where(e => e != null));
        aggressiveEntities.AddRange(shuffled.Skip(capturingCount).Where(e => e != null));

        Debug.Log($"Level2EnemyAI: Assigned {capturingEntities.Count} to capturing, {aggressiveEntities.Count} to aggressive.");

        SetReactionTime();
        // SetMoveTill();

        Debug.Log($"Level2EnemyAI: ReactionTime={reactionTime}, MoveTill={moveTill}.");

        EnemyAIMgr.inst.StartCoroutine(TakeInitialActionAfterDelay(reactionTime, new List<Entity>(aggressiveEntities)));
        EnemyAIMgr.inst.StartCoroutine(TakeInitialCaptureActionAfterDelay(reactionTime, new List<Entity>(capturingEntities)));
    }

    private void AddNewUnits(List<Entity> currentEntities)
    {
        foreach (var ent in currentEntities)
        {
            if (ent == null) continue;

            bool known = idleEntities.Contains(ent) || aggressiveEntities.Contains(ent) || retreatingEntities.Contains(ent) || capturingEntities.Contains(ent);
            if (!known)
            {
                // After neutral capture, all new units start idle as requested.
                Debug.Log($"Level2EnemyAI: New unit {ent.name} detected. Adding to idle.");
                idleEntities.Add(ent);
            }
        }
    }

    private void UpdateAggressiveEntities()
    {
        var toRetreat = new List<Entity>();
        var toIdle = new List<Entity>();

        foreach (var ent in aggressiveEntities)
        {
            if (ent != null && ent.isBeingAttacked) toRetreat.Add(ent);
        }

        foreach (var ent in toRetreat)
        {
            Debug.Log($"Level2EnemyAI: Aggressive {ent.name} under attack. Retreating.");
            aggressiveEntities.Remove(ent);
            if (!retreatingEntities.Contains(ent)) retreatingEntities.Add(ent);

            if (aiBases.Count > 0 && aiBases[0] != null)
            {
                AIMgr.inst.HandleMove(new List<Entity> { ent }, aiBases[0].transform.position);
            }
        }
        foreach (var ent in aggressiveEntities)
        {
            if (ent != null && !ent.isBeingAttacked && ent.isAttacking == false && ent.speed == 0  )
            {
                idleEntities.Add(ent);
                aggressiveEntities.Remove(ent);
            }
        }
    }

    private void UpdateRetreatingEntities()
    {
        var toIdle = new List<Entity>();
        foreach (var ent in retreatingEntities)
        {
            if (ent != null && !ent.isBeingAttacked) toIdle.Add(ent);
        }

        foreach (var ent in toIdle)
        {
            Debug.Log($"Level2EnemyAI: Retreating {ent.name} safe. To idle.");
            retreatingEntities.Remove(ent);
            if (!idleEntities.Contains(ent)) idleEntities.Add(ent);
        }
    }

    private void UpdateIdleEntities()
    {
        if (idleEntities.Count == 0) return;

        Debug.Log($"Level2EnemyAI: {idleEntities.Count} idle -> aggressive.");
        AIMgr.inst.HandleAttackMove(idleEntities, opponentBase.transform.position, opponentBase, acquireTarget: true, useLowestCruiseSpeed: true);

        aggressiveEntities.AddRange(idleEntities.Where(e => e != null));
        idleEntities.Clear();
    }

    private void UpdateCapturingEntities()
    {
        // If already captured, do nothing here; capturers are converted in HandleNeutralBaseCaptured().
        if (processedCapture) return;

        if (neutralBases == null || neutralBases.Count == 0 || neutralBases[0] == null) return;
        if (aiBases == null || aiBases.Count == 0 || aiBases[0] == null) return;

        var toRetreat = new List<Entity>();
        foreach (var e in capturingEntities)
        {
            if (e != null && e.isBeingAttacked) toRetreat.Add(e);
        }

        foreach (var e in toRetreat)
        {
            Debug.Log($"Level2EnemyAI: Capturer {e.name} under attack. Retreating to base.");
            capturingEntities.Remove(e);
            if (!retreatingEntities.Contains(e)) retreatingEntities.Add(e);

            AIMgr.inst.HandleMove(new List<Entity> { e }, aiBases[0].transform.position, useLowestCruiseSpeed: true);
            EnemyAIMgr.inst.StartCoroutine(ReturnToCaptureWhenSafe(e));
        }
    }

    private IEnumerator ReturnToCaptureWhenSafe(Entity e)
    {
        // Wait while attacked
        while (e != null && e.isBeingAttacked) yield return null;

        // Small debounce
        yield return new WaitForSeconds(2f);

        if (e == null) yield break;

        // If neutral already captured while retreating, prefer idle conversion
        if (processedCapture || neutralBaseCaptured || IsNeutralCaptured())
        {
            retreatingEntities.Remove(e);
            if (!idleEntities.Contains(e)) idleEntities.Add(e);
            yield break;
        }

        // Return to capture
        retreatingEntities.Remove(e);
        if (!capturingEntities.Contains(e)) capturingEntities.Add(e);

        if (neutralBases != null && neutralBases.Count > 0 && neutralBases[0] != null)
        {
            Debug.Log($"Level2EnemyAI: {e.name} returning to capture point.");
            AIMgr.inst.HandleMove(new List<Entity> { e }, neutralBases[0].transform.position);
        }
    }

    private IEnumerator TakeInitialActionAfterDelay(float delay, List<Entity> entitiesToCommand)
    {
        yield return new WaitForSeconds(delay);

        var valid = entitiesToCommand.Where(e => e != null && aggressiveEntities.Contains(e)).ToList();
        if (valid.Count > 0)
        {
            Debug.Log($"Level2EnemyAI: Initial delay over. Commanding {valid.Count} aggressive units.");
            AIMgr.inst.HandleAttackMove(valid, opponentBase.transform.position, opponentBase, acquireTarget: true, useLowestCruiseSpeed: true);
        }
        else
        {
            Debug.Log("Level2EnemyAI: All initial aggressive units lost or retreated.");
        }
    }

    private IEnumerator TakeInitialCaptureActionAfterDelay(float delay, List<Entity> entitiesToCommand)
    {
        yield return new WaitForSeconds(delay);

        // If captured meanwhile, skip sending to neutral.
        if (processedCapture || neutralBaseCaptured || IsNeutralCaptured()) yield break;

        var valid = entitiesToCommand.Where(e => e != null && capturingEntities.Contains(e)).ToList();
        if (valid.Count > 0 && neutralBases != null && neutralBases.Count > 0 && neutralBases[0] != null)
        {
            Debug.Log($"Level2EnemyAI: Initial capture delay over. Commanding {valid.Count} to capture.");
            AIMgr.inst.HandleMove(valid, neutralBases[0].transform.position, useLowestCruiseSpeed: true);
        }
        else
        {
            Debug.Log("Level2EnemyAI: Initial capturers lost or target invalid.");
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

    // ---- Neutral base capture handling ----

    // Call this from your capture system when the neutral base is secured.
    public void OnNeutralBaseCaptured()
    {
        neutralBaseCaptured = true;
        HandleNeutralBaseCaptured();
    }

    // One-time conversion: capturers -> idle. Future new units already go to idle in AddNewUnits().
    private void HandleNeutralBaseCaptured()
    {
        processedCapture = true;

        // Cancel capture role: move everyone to idle state.
        foreach (var e in capturingEntities.ToList())
        {
            if (e == null) continue;
            if (!idleEntities.Contains(e)) idleEntities.Add(e);
        }
        capturingEntities.Clear();

        Debug.Log("Level2EnemyAI: Neutral base captured. Capturing units converted to idle. New units will remain idle.");
    }

    // Optional detector. Replace with your real ownership test if available.
    private bool IsNeutralCaptured()
    {
        return CaptureNeutralBaseMgr.inst.CheckCaptureCompletion();
    }
}
