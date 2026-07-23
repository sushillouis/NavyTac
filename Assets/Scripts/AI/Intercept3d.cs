using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Intercept3d : Intercept
{
    public Intercept3d(Entity entity, Entity target) : base(entity, target) { }

    Oriented3dPhysics phx3d;

    // Terminal-phase trigger
    public float terminalPhaseDistanceSq;

    // Abort / timeout settings (tweak in inspector or at construction)
    public float maxPursuitDistance = 20000f;         // absolute hard limit (meters)
    public float maxPursuitDistanceSq;                // cached squared
    public float runawayFactor = 4f;                  // if target distance grows > runawayFactor * initial distance -> abort
    public float lostTargetTimeout = 3f;              // seconds to wait after losing target before aborting
    public float movingAwayTimeout = 2f;              // seconds target must be moving away to abort
    public bool destroyOnAbort = true;                // toggle: destroy missile when aborting

    // Internal state
    float lostTargetTimer = 0f;
    float movingAwayTimer = 0f;
    float initialTargetDistanceSq = float.PositiveInfinity;
    float previousTargetDistanceSq = float.PositiveInfinity;

    // Short history to detect circling / no-closure
    List<float> distanceHistory = new List<float>();
    public int distanceHistorySize = 20;         // number of samples to keep
    public float minClosureDelta = 1.0f;         // minimum net reduction (meters) across history to be considered "closing"
    public float sampleInterval = 0.05f;         // sample every X seconds
    float sampleTimer = 0f;

    public override void Init()
    {
        base.Init();

        phx3d = entity.GetComponentInChildren<Oriented3dPhysics>();

        if (phx3d != null)
        {
            terminalPhaseDistanceSq = entity.maxSpeed * entity.maxSpeed * 9; // ~3 seconds worth of travel
        }
        else
        {
            terminalPhaseDistanceSq = 0;
        }

        maxPursuitDistanceSq = maxPursuitDistance * maxPursuitDistance;

        if (targetEntity != null)
        {
            var d = targetEntity.position - entity.position;
            initialTargetDistanceSq = d.sqrMagnitude;
            previousTargetDistanceSq = initialTargetDistanceSq;
            distanceHistory.Clear();
            distanceHistory.Add(Mathf.Sqrt(previousTargetDistanceSq));
        }
    }

    public override void Tick()
    {
        int boundary = 10000;
        if (entity.position.x > boundary || entity.position.x < -boundary || entity.position.z > boundary || entity.position.z < -boundary)
        {
            WeaponsMgr.inst.DestroyEntity(entity);
            return;
        }

        // If target missing or inactive: start lost-target timer and abort if it exceeds lostTargetTimeout
        if (targetEntity == null || !targetEntity.gameObject.activeSelf)
        {
            lostTargetTimer += Time.deltaTime;
            // keep moving straight while waiting for possible reacquire
            entity.desiredHeading = entity.heading;
            entity.desiredSpeed = entity.maxSpeed;
            diffToMovePosition = Vector3.positiveInfinity;

            if (lostTargetTimer >= lostTargetTimeout)
            {
                // abort intercept
                EndIntercept("target_lost_timeout");
                return;
            }

            return;
        }

        // Reset lost target timer if we have a valid target
        lostTargetTimer = 0f;

        // Compute current vector to target first so base.Tick() uses current info
        diffToMovePosition = targetEntity.position - entity.position;
        float currentDistSq = diffToMovePosition.sqrMagnitude;
        float currentDist = Mathf.Sqrt(currentDistSq);

        // Initialize initial distance if it wasn't set earlier
        if (initialTargetDistanceSq == float.PositiveInfinity)
        {
            initialTargetDistanceSq = currentDistSq;
        }

        // Abort if the target is now farther than absolute maximum pursuit distance
        if (currentDistSq > maxPursuitDistanceSq)
        {
            EndIntercept("exceeded_max_pursuit_distance");
            return;
        }

        // Abort if target has run away beyond runawayFactor * initial distance
        if (currentDistSq > initialTargetDistanceSq * runawayFactor)
        {
            EndIntercept("target_ran_away");
            return;
        }

        // If target is consistently moving away (distance increasing), count up a timer
        if (currentDistSq > previousTargetDistanceSq + 1f) // small hysteresis, 1 unit
        {
            movingAwayTimer += Time.deltaTime;
            if (movingAwayTimer >= movingAwayTimeout)
            {
                EndIntercept("target_moving_away");
                return;
            }
        }
        else
        {
            movingAwayTimer = 0f;
        }

        // Save for next tick
        previousTargetDistanceSq = currentDistSq;

        // Sample distance history at a lower rate to detect circling/no-closure
        sampleTimer += Time.deltaTime;
        if (sampleTimer >= sampleInterval)
        {
            sampleTimer = 0f;
            distanceHistory.Add(currentDist);
            if (distanceHistory.Count > distanceHistorySize)
                distanceHistory.RemoveAt(0);

            // Only evaluate once we have a full history
            if (distanceHistory.Count == distanceHistorySize)
            {
                float first = distanceHistory[0];
                float last = distanceHistory[distanceHistory.Count - 1];

                // If net closure is below threshold (i.e., not closing) -> consider circling/stall and abort
                if (first - last < minClosureDelta)
                {
                    EndIntercept("no_net_closure_or_circling");
                    return;
                }

                // Additional circling heuristic: tiny oscillation range across history
                float max = float.MinValue;
                float min = float.MaxValue;
                for (int i = 0; i < distanceHistory.Count; ++i)
                {
                    if (distanceHistory[i] > max) max = distanceHistory[i];
                    if (distanceHistory[i] < min) min = distanceHistory[i];
                }

                // If distances only oscillate in a tight band (e.g., < 5 meters) we likely are circling
                if (max - min < 5f)
                {
                    EndIntercept("tight_oscillation_circling");
                    return;
                }
            }
        }

        // Call base guidance (now that diffToMovePosition is up to date)
        base.Tick();

        // Force missile to run at max speed for homing
        entity.desiredSpeed = entity.maxSpeed;

        // Terminal-phase altitude matching
        if (diffToMovePosition.sqrMagnitude < terminalPhaseDistanceSq)
        {
            if (phx3d == null)
            {
                // safety guard
            }
            else if (targetEntity.TryGetComponent<Oriented3dPhysics>(out var targetPhx3d))
            {
                phx3d.desiredAltitude = targetPhx3d.altitude;
            }
            else
            {
                phx3d.desiredAltitude = Mathf.Max(targetEntity.position.y, 10f);
            }
        }
    }

    public override bool IsDone()
    {
        // 10m radius (distance squared < 100)
        return diffToMovePosition.sqrMagnitude < 100;
    }

    public override void Stop()
    {
        base.Stop();
    }

    // Centralized abort handler. Add logging or cleanup here.
    void EndIntercept(string reason)
    {
        // Stop steering and disable further proximity checks
        diffToMovePosition = Vector3.positiveInfinity;
        entity.desiredSpeed = 0f;
        entity.desiredHeading = entity.heading;

        // Reset timers/state
        lostTargetTimer = 0f;
        movingAwayTimer = 0f;
        distanceHistory.Clear();
        previousTargetDistanceSq = float.PositiveInfinity;
        initialTargetDistanceSq = float.PositiveInfinity;

        // Call Stop to run any base-class cleanup
        Stop();

        // Destroy the missile on abort if configured
        if (destroyOnAbort && entity != null && entity.entityType == EntityType.AntiShipMissile)
        {
            FXMgr.inst.CreateExplosionAt(entity.position, 1);
            WeaponsMgr.inst.DestroyEntity(entity);
        }

        Debug.Log($"Intercept ended: {reason}");
    }
}
