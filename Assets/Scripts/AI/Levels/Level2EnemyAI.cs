using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Level2EnemyAI : BaseEnemyAI
{
    private bool started;
    private List<Entity> captureGroup = new List<Entity>();
    private List<Entity> greyedCaptureGroup = new List<Entity>();
    private List<Entity> greyedAttackers = new List<Entity>();
    private Coroutine monitorCo;
    private float monitorInterval = 2f;
    private int maxCapture = 3;     // send up to 3 JARIUSV
    private int reinforceCount = 2; // send 2 more when needed

    public override void ProcessCombatBehavior(List<Entity> aiEntities)
    {
        if (opponentBase == null || aiEntities == null || aiEntities.Count == 0) return;
        if (started) return; // run once to avoid re-sending spam
        started = true;

        captureGroup.Clear();
        greyedCaptureGroup.Clear();
        greyedAttackers.Clear();

        var jari = aiEntities.Where(e => e != null && e.entityType == EntityType.JARIUSV).ToList();
        var attackers = new List<Entity>(aiEntities);

        if (neutralBases != null && neutralBases.Count > 0 && jari.Count > 0)
        {
            var take = jari.Take(Mathf.Min(maxCapture, jari.Count)).ToList();
            captureGroup.AddRange(take);
            foreach (var c in take) attackers.Remove(c);

            // SEND CAPTURE GROUP NOW
            var nb = neutralBases[0];
            IssueAttackMove(captureGroup, nb.transform.position, targetBase: null); // capture
        }

        // SEND ALL REMAINING TO OPPONENT BASE
        IssueAttackMove(attackers.Where(e => e != null && e.entityType != EntityType.AntiShipMissile).ToList(),
                        opponentBase.position, opponentBase.GetComponent<Entity>());

        // start monitor for reinforcements and commit if captured/lost
        if (monitorCo != null) EnemyAIMgr.inst.StopCoroutine(monitorCo);
        monitorCo = EnemyAIMgr.inst.StartCoroutine(MonitorNeutralAndReinforce());
    }

    public override void ResetState()
    {
        base.ResetState();
        started = false;
        captureGroup.Clear();
        greyedCaptureGroup.Clear();
        greyedAttackers.Clear();
        if (monitorCo != null) { EnemyAIMgr.inst.StopCoroutine(monitorCo); monitorCo = null; }
    }

    private void IssueAttackMove(List<Entity> units, Vector3 pos, Entity targetBase)
    {
        if (units == null || units.Count == 0) return;

        var activeUnits = units.Where(e => e != null && !e.isGreyed).ToList();
        var greyedUnits = units.Where(e => e != null && e.isGreyed).ToList();

        if (targetBase == null && neutralBases != null && neutralBases.Count > 0)
        {
            greyedCaptureGroup.AddRange(greyedUnits);
        }
        else
        {
            greyedAttackers.AddRange(greyedUnits);
        }

        if (activeUnits.Count == 0) return;
        // attack-move in one call, no jitter
        AIMgr.inst.HandleAttackMove(activeUnits, pos, targetBase, false, acquireTarget: true);
    }

    private IEnumerator MonitorNeutralAndReinforce()
    {
        if (neutralBases == null || neutralBases.Count == 0) yield break;
        var nb = neutralBases[0];

        while (true)
        {
            yield return new WaitForSeconds(monitorInterval);
            if (nb == null) yield break;

            // Re-issue commands to un-greyed units
            var newlyActiveCapture = greyedCaptureGroup.Where(e => e != null && !e.isGreyed).ToList();
            if (newlyActiveCapture.Count > 0)
            {
                IssueAttackMove(newlyActiveCapture, nb.transform.position, null);
                greyedCaptureGroup.RemoveAll(e => newlyActiveCapture.Contains(e));
            }

            var newlyActiveAttackers = greyedAttackers.Where(e => e != null && !e.isGreyed).ToList();
            if (newlyActiveAttackers.Count > 0)
            {
                IssueAttackMove(newlyActiveAttackers, opponentBase.position, opponentBase.GetComponent<Entity>());
                greyedAttackers.RemoveAll(e => newlyActiveAttackers.Contains(e));
            }

            // counts near neutral
            int ours = 0, enemy = 0;
            float r = 1400f;

            foreach (var e in EntityMgr.inst.entities)
            {
                if (e == null || e.transform == null || e.owner == null) continue;
                if (Vector3.Distance(e.transform.position, nb.transform.position) > r) continue;
                if (e.entityType == EntityType.AntiShipMissile) continue;

                if (e.owner == PlayerMgr.inst.player1) ours++;
                else if (e.owner == PlayerMgr.inst.player2) enemy++;
            }

            // clean captureGroup of destroyed refs
            captureGroup.RemoveAll(x => x == null);

            // reinforce if wiped or outnumbered
            if (captureGroup.Count == 0 || enemy > ours)
            {
                var candidates = EntityMgr.inst.entities
                    .Where(e => e != null
                                && e.owner == PlayerMgr.inst.player1
                                && e.entityType != EntityType.AntiShipMissile
                                && !captureGroup.Contains(e))
                    .OrderBy(e => Vector3.Distance(e.transform.position, nb.transform.position))
                    .Take(reinforceCount)
                    .ToList();

                if (candidates.Count > 0)
                {
                    captureGroup.AddRange(candidates);
                    IssueAttackMove(candidates, nb.transform.position, targetBase: null);
                }
            }

            // if neutral captured by anyone or clearly lost, commit all to opponent base
            // if (nb.owner != PlayerMgr.inst.neutral || enemy > ours + 2)
            // {
            //     var allUs = EntityMgr.inst.entities
            //         .Where(e => e != null
            //                     && e.owner == PlayerMgr.inst.player1
            //                     && e.entityType != EntityType.AntiShipMissile)
            //         .ToList();

            //     IssueAttackMove(allUs, opponentBase.position, opponentBase.GetComponent<Entity>());
            //     yield break;
            // }
        }
    }
}
