using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks; // Added for Parallel.ForEach

[Serializable]
public class SubPotential{
    public Vector3 diff;
    public float distance;
    public Vector3 direction;
    public Transform pfTransform;
    public Collider cachedPftCollider; // Cached collider for pfTransform
}

[Serializable]
public class Potential
{
    public Entity ownship;
    public Entity target;
    public float distance;
    public Vector3 diff;
    public Vector3 relativeVelocity; // As calculated by CPAInfo: target.velocity - ownship.velocity
    public Vector3 direction; //normalized diff
    public CPAInfo cpaInfo;
    public float targetAngle;

    public int framecount;

    public List<SubPotential> subPotentials;

    public Potential(Entity own, Entity tgt)
    {
        ownship = own;
        target = tgt;
        cpaInfo = new CPAInfo(own, target);
        subPotentials = new List<SubPotential>();
        if (own != null && own.ai != null && own.ai.pfList != null)
        {
            foreach(Transform t in own.ai.pfList) {
                if (t == null) continue;
                SubPotential subPotential = new SubPotential();
                subPotential.pfTransform = t;
                subPotential.cachedPftCollider = t.GetComponent<Collider>();
                subPotentials.Add(subPotential);
            }
        }
    }

    // This method is not directly used by DistanceMgr in its current update flow.
    void InitDefaults()
    {
        distance = 0;
        diff = Vector3.zero;
        relativeVelocity = Vector3.zero;
        direction = Vector3.zero;
        if (ownship != null && target != null)
        {
            cpaInfo = new CPAInfo(ownship, target);
        } else {
            cpaInfo = null; 
        }
        targetAngle = 0;
    }

    // This method is not directly used by DistanceMgr's new multi-threaded update.
    // DistanceMgr performs similar calculations with more detailed collider logic.
    public void ReCompute() {
        framecount = Time.frameCount;

        if (target == null || ownship == null) return;

        diff = target.position - ownship.position;
        distance = diff.magnitude;
        direction = diff.normalized;
        
        if (cpaInfo == null) cpaInfo = new CPAInfo(ownship, target);
        cpaInfo.ReCompute();
        relativeVelocity = cpaInfo.relativeVelocity; 
        targetAngle = cpaInfo.targetAngle;
        
        foreach(SubPotential sp in subPotentials) {
            if (sp == null || sp.pfTransform == null || target == null) continue;
            sp.diff = target.position - sp.pfTransform.position;
            sp.direction = sp.diff.normalized;
            sp.distance = sp.diff.magnitude;
        }
    }
}

[System.Serializable]
public class CPAInfo
{
    public Entity ownship; // Used by original ReCompute, and for context in constructor
    public Entity target;  // Used by original ReCompute, and for context in constructor
    public Vector3 ownShipPosition = Vector3.zero; // Position of ownship at CPA
    public Vector3 targetPosition = Vector3.zero;  // Position of target at CPA
    public float time = 0; // Time to CPA
    public float range = 0; // Range at CPA
    public float targetRelativeBearing = 0;
    public float targetAbsBearing = 0;
    public float targetAngle;
    public Vector3 relativeVelocity = Vector3.zero; // target.velocity - ownship.velocity

    private Vector3 _velDiff = Vector3.zero;    // ownship.velocity - target.velocity
    private Vector3 _posDiff = Vector3.zero;    // ownship.position - target.position (or closest points based)
    private float _relSpeedSquared = 0;

    public CPAInfo(Entity e1, Entity e2)
    {
        ownship = e1;
        target = e2;
    }

    // Original ReCompute, relies on Unity API via Entity properties
    public void ReCompute()
    {
        if (ownship == null || target == null) return;

        _velDiff = ownship.velocity - target.velocity;

        Collider ownshipCollider = ownship.GetComponent<Collider>();
        Collider targetCollider = target.GetComponent<Collider>();

        if (ownshipCollider != null && targetCollider != null)
        {
            Vector3 closestPointOnOwnship = ownshipCollider.ClosestPoint(target.position);
            Vector3 closestPointOnTarget = targetCollider.ClosestPoint(ownship.position);
            _posDiff = closestPointOnOwnship - closestPointOnTarget;
        }
        else
        {
            _posDiff = ownship.position - target.position;
        }
        
        relativeVelocity = target.velocity - ownship.velocity;
        _relSpeedSquared = _velDiff.sqrMagnitude;

        if (_relSpeedSquared < Utils.EPSILON * 10)
            time = 0;
        else
            time = -Vector3.Dot(_posDiff, _velDiff) / _relSpeedSquared;
        
        if (time < 0) time = 0;

        ownShipPosition = ownship.position + ownship.velocity * time;
        targetPosition = target.position + target.velocity * time;
        
        Vector3 cpaVector = targetPosition - ownShipPosition;
        range = cpaVector.magnitude;

        targetAbsBearing = Utils.Degrees360(Utils.VectorToHeadingDegrees(cpaVector));
        targetRelativeBearing = Utils.Degrees360(Utils.AngleDiffPosNeg(targetAbsBearing, ownship.heading));
        targetAngle = Utils.Degrees360(targetAbsBearing + 180 - target.heading);
    }

    // Thread-safe version of ReCompute, takes all necessary data as parameters
    public void ReCompute_ThreadSafe(Vector3 osPos, Vector3 osVel, float osHeading,
                                     Vector3 tgtPos, Vector3 tgtVel, float tgtHeading,
                                     Vector3 precomputedPosDiff)
    {
        _velDiff = osVel - tgtVel;
        _posDiff = precomputedPosDiff; // Use precomputed value (e.g., from ClosestPoint or center-to-center)

        relativeVelocity = tgtVel - osVel; 
        _relSpeedSquared = _velDiff.sqrMagnitude;

        if (_relSpeedSquared < Utils.EPSILON * 10)
            time = 0;
        else
            time = -Vector3.Dot(_posDiff, _velDiff) / _relSpeedSquared;
        
        if (time < 0) time = 0; // CPA is in the past

        ownShipPosition = osPos + osVel * time;
        targetPosition = tgtPos + tgtVel * time;
        
        Vector3 cpaVector = targetPosition - ownShipPosition;
        range = cpaVector.magnitude;

        targetAbsBearing = Utils.Degrees360(Utils.VectorToHeadingDegrees(cpaVector));
        targetRelativeBearing = Utils.Degrees360(Utils.AngleDiffPosNeg(targetAbsBearing, osHeading));
        targetAngle = Utils.Degrees360(targetAbsBearing + 180 - tgtHeading);
    }
};

// Helper class for passing data to worker threads for SubPotential calculation
class SubPotentialTaskData {
    public SubPotential subPotentialToUpdate; // Reference to the SubPotential object
    public Vector3 pfPosition;
    public Vector3 targetEntityPosition;
    public float precalculatedDistance;
    public Vector3 diff;
    public Vector3 direction;
}

// Helper class for passing data to worker threads for Potential calculation
class PotentialCalculationTaskData {
    public Potential potentialToUpdateP1;
    public Potential potentialToUpdateP2;
    public int frameCount;

    // Data for P1 (ent1 -> ent2)
    public Vector3 ownshipPositionP1;
    public Vector3 ownshipVelocityP1;
    public float ownshipHeadingP1;
    public Vector3 targetPositionP1;
    public Vector3 targetVelocityP1;
    public float targetHeadingP1;
    public float precalculatedDistanceP1;
    public Vector3 diffP1;
    public Vector3 directionP1;
    public Vector3 cpaP1_posDiff; // Precomputed position difference for CPA calc
    public List<SubPotentialTaskData> subPotentialsDataP1;

    // Data for P2 (ent2 -> ent1)
    public Vector3 ownshipPositionP2;
    public Vector3 ownshipVelocityP2;
    public float ownshipHeadingP2;
    public Vector3 targetPositionP2;
    public Vector3 targetVelocityP2;
    public float targetHeadingP2;
    public float precalculatedDistanceP2;
    public Vector3 diffP2;
    public Vector3 directionP2;
    public Vector3 cpaP2_posDiff; // Precomputed position difference for CPA calc
    public List<SubPotentialTaskData> subPotentialsDataP2;
}


public class DistanceMgr : MonoBehaviour
{
    public static DistanceMgr inst;
    private void Awake()
    {
        inst = this;
    }

    public Potential[,] potentials2D;
    public Dictionary<Entity, Dictionary<Entity, Potential>> potentialsDictionary;
    public List<List<Potential>> potentialsList;

    public bool isInitialized = false;
    public int ii = 0;
    public int jj = 0;

    private List<PotentialCalculationTaskData> _taskDataList = new List<PotentialCalculationTaskData>(); // Reusable list for task data

    void Start()
    {
        // Initialization is handled in Update if not initialized.
    }

     public void Initialize()
    {
        isInitialized = true;
        potentialsDictionary = new Dictionary<Entity, Dictionary<Entity, Potential>>();
        potentialsList = new List<List<Potential>>();
        
        var validEntities = EntityMgr.inst.entities.FindAll(e => e != null && e.entityClass != EntityClass.Missile);
        int n = validEntities.Count;
        
        potentials2D = new Potential[n, n];
        
        for (int i_idx = 0; i_idx < n; i_idx++)
        {
            Entity ent1 = validEntities[i_idx];
            
            Dictionary<Entity, Potential> ent1PotDictionary = new Dictionary<Entity, Potential>();
            List<Potential> ent1PotList = new List<Potential>();
            potentialsDictionary.Add(ent1, ent1PotDictionary);
            potentialsList.Add(ent1PotList);
            
            for (int j_idx = 0; j_idx < n; j_idx++)
            {
                Entity ent2 = validEntities[j_idx];
                
                Potential pot = new Potential(ent1, ent2);
                ent1PotDictionary.Add(ent2, pot);
                ent1PotList.Add(pot);
                potentials2D[i_idx, j_idx] = pot;
            }
        }
        this.ii = n; 
        this.jj = n; 
    }

    void Stop() // Consider OnDisable or OnDestroy
    {
        isInitialized = false;
        // potentials2D = null;
        // potentialsDictionary?.Clear();
        // potentialsList?.Clear();
        // _taskDataList?.Clear();
    }

    private int frameCounter = 0; 
    void Update()
    {
        if (!isInitialized)
        {
            if (EntityMgr.inst != null && EntityMgr.inst.entities != null)
            {
                Initialize();
            } else {
                return; 
            }
        }
        
        if (isInitialized)
        {
            UpdatePotentialsMultiThreaded();
        }
        frameCounter++;
    }

    public List<Potential> selectedEntityPotentials; 

    void UpdatePotentialsMultiThreaded()
    {
        if (potentialsList == null) return;
        int n = potentialsList.Count;
        if (n == 0) return;

        int currentFrameMod = frameCounter % 10; // Stagger updates
        _taskDataList.Clear(); // Reuse the list
        int currentFrame = Time.frameCount; // Read once on main thread

        // Phase 1: Data Gathering (Main Thread)
        for (int i = 0; i < n; i++)
        {
            if (i % 10 != currentFrameMod) continue; // Staggering
            if (potentialsList[i] == null || potentialsList[i].Count == 0) continue;

            Entity ent1 = null;
            foreach (var p_check in potentialsList[i]) {
                if (p_check != null && p_check.ownship != null) {
                    ent1 = p_check.ownship;
                    break;
                }
            }
            
            if (ent1 == null || ent1.entityClass == EntityClass.Missile) continue;

            if (SelectionMgr.inst != null && ent1 == SelectionMgr.inst.selectedEntity)
            {
                selectedEntityPotentials = potentialsList[i];
            }

            for (int j = i + 1; j < n; j++) // Process each pair (i, j) once where j > i
            {
                if (potentialsList[i].Count <= j) continue;
                Potential p1 = potentialsList[i][j]; // Potential(ent1, ent_j)
                if (p1 == null || p1.ownship == null || p1.target == null) continue;
                Entity ent2 = p1.target; // This is ent_j
                if (ent2 == null || ent2.entityClass == EntityClass.Missile) continue;

                // Get p2 = Potential(ent_j, ent1)
                if (j >= potentialsList.Count || potentialsList[j].Count <= i) continue;
                Potential p2 = potentialsList[j][i]; 
                if (p2 == null || p2.ownship == null || p2.target == null || p2.ownship != ent2 || p2.target != ent1) {
                     // Data inconsistency or p2 not found as expected
                    Debug.LogWarning($"Could not find or verify symmetric potential for pair ({ent1.name}, {ent2.name})");
                    continue;
                }

                var taskData = new PotentialCalculationTaskData {
                    potentialToUpdateP1 = p1,
                    potentialToUpdateP2 = p2,
                    frameCount = currentFrame,
                    // P1 (ent1 -> ent2)
                    ownshipPositionP1 = ent1.position, ownshipVelocityP1 = ent1.velocity, ownshipHeadingP1 = ent1.heading,
                    targetPositionP1 = ent2.position, targetVelocityP1 = ent2.velocity, targetHeadingP1 = ent2.heading,
                    subPotentialsDataP1 = new List<SubPotentialTaskData>(),
                    // P2 (ent2 -> ent1)
                    ownshipPositionP2 = ent2.position, ownshipVelocityP2 = ent2.velocity, ownshipHeadingP2 = ent2.heading,
                    targetPositionP2 = ent1.position, targetVelocityP2 = ent1.velocity, targetHeadingP2 = ent1.heading,
                    subPotentialsDataP2 = new List<SubPotentialTaskData>()
                };

                // --- Main Thread Computations for P1 (ent1 -> ent2) ---
                taskData.diffP1 = taskData.targetPositionP1 - taskData.ownshipPositionP1;
                taskData.directionP1 = taskData.diffP1.normalized;
                Collider ownshipColliderP1 = ent1.GetComponent<Collider>();
                Collider targetColliderP1 = ent2.GetComponent<Collider>();

                if (ownshipColliderP1 != null && targetColliderP1 != null) {
                    Vector3 closestOnOwnP1 = ownshipColliderP1.ClosestPoint(taskData.targetPositionP1);
                    Vector3 closestOnTgtP1 = targetColliderP1.ClosestPoint(taskData.ownshipPositionP1);
                    taskData.precalculatedDistanceP1 = Vector3.Distance(closestOnOwnP1, closestOnTgtP1);
                    taskData.cpaP1_posDiff = closestOnOwnP1 - closestOnTgtP1;
                } else {
                    taskData.precalculatedDistanceP1 = taskData.diffP1.magnitude;
                    taskData.cpaP1_posDiff = taskData.ownshipPositionP1 - taskData.targetPositionP1;
                }

                // SubPotentials for P1 (ownship: ent1, target: ent2)
                if (p1.subPotentials != null) {
                    foreach (SubPotential sp_p1 in p1.subPotentials) {
                        if (sp_p1 == null || sp_p1.pfTransform == null) continue;
                        var subData = new SubPotentialTaskData { subPotentialToUpdate = sp_p1 };
                        subData.pfPosition = sp_p1.pfTransform.position;
                        subData.targetEntityPosition = taskData.targetPositionP1; // ent2.position
                        subData.diff = subData.targetEntityPosition - subData.pfPosition;
                        subData.direction = subData.diff.normalized;
                        Collider pfCollider = sp_p1.cachedPftCollider;
                        if (pfCollider != null && targetColliderP1 != null) { // targetColliderP1 is ent2's collider
                            Vector3 cOnPf = pfCollider.ClosestPoint(subData.targetEntityPosition);
                            Vector3 cOnTgt = targetColliderP1.ClosestPoint(subData.pfPosition);
                            subData.precalculatedDistance = Vector3.Distance(cOnPf, cOnTgt);
                        } else if (pfCollider != null) {
                            subData.precalculatedDistance = Vector3.Distance(pfCollider.ClosestPoint(subData.targetEntityPosition), subData.targetEntityPosition);
                        } else if (targetColliderP1 != null) {
                            subData.precalculatedDistance = Vector3.Distance(targetColliderP1.ClosestPoint(subData.pfPosition), subData.pfPosition);
                        } else {
                            subData.precalculatedDistance = subData.diff.magnitude;
                        }
                        taskData.subPotentialsDataP1.Add(subData);
                    }
                }

                // --- Main Thread Computations for P2 (ent2 -> ent1) ---
                taskData.diffP2 = taskData.targetPositionP2 - taskData.ownshipPositionP2; // ent1.pos - ent2.pos
                taskData.directionP2 = taskData.diffP2.normalized;
                taskData.precalculatedDistanceP2 = taskData.precalculatedDistanceP1; // Distance is symmetric

                // cpaP2_posDiff: ent2.collider.ClosestPoint(ent1.pos) - ent1.collider.ClosestPoint(ent2.pos)
                if (targetColliderP1 != null && ownshipColliderP1 != null) { // ent2.collider & ent1.collider
                    Vector3 closestOnOwnP2 = targetColliderP1.ClosestPoint(taskData.targetPositionP2); // ent2.collider.ClosestPoint(ent1.pos)
                    Vector3 closestOnTgtP2 = ownshipColliderP1.ClosestPoint(taskData.ownshipPositionP2); // ent1.collider.ClosestPoint(ent2.pos)
                    taskData.cpaP2_posDiff = closestOnOwnP2 - closestOnTgtP2;
                } else {
                    taskData.cpaP2_posDiff = taskData.ownshipPositionP2 - taskData.targetPositionP2; // ent2.pos - ent1.pos
                }
                
                // SubPotentials for P2 (ownship: ent2, target: ent1)
                if (p2.subPotentials != null) {
                    foreach (SubPotential sp_p2 in p2.subPotentials) {
                        if (sp_p2 == null || sp_p2.pfTransform == null) continue;
                        var subData = new SubPotentialTaskData { subPotentialToUpdate = sp_p2 };
                        subData.pfPosition = sp_p2.pfTransform.position;
                        subData.targetEntityPosition = taskData.targetPositionP2; // ent1.position
                        subData.diff = subData.targetEntityPosition - subData.pfPosition;
                        subData.direction = subData.diff.normalized;
                        Collider pfCollider = sp_p2.cachedPftCollider;
                        // ownshipColliderP1 is ent1's collider
                        if (pfCollider != null && ownshipColliderP1 != null) { 
                            Vector3 cOnPf = pfCollider.ClosestPoint(subData.targetEntityPosition);
                            Vector3 cOnTgt = ownshipColliderP1.ClosestPoint(subData.pfPosition);
                            subData.precalculatedDistance = Vector3.Distance(cOnPf, cOnTgt);
                        } else if (pfCollider != null) {
                             subData.precalculatedDistance = Vector3.Distance(pfCollider.ClosestPoint(subData.targetEntityPosition), subData.targetEntityPosition);
                        } else if (ownshipColliderP1 != null) {
                            subData.precalculatedDistance = Vector3.Distance(ownshipColliderP1.ClosestPoint(subData.pfPosition), subData.pfPosition);
                        } else {
                            subData.precalculatedDistance = subData.diff.magnitude;
                        }
                        taskData.subPotentialsDataP2.Add(subData);
                    }
                }
                _taskDataList.Add(taskData);
            }
        }

        // Phase 2: Parallel Computation
        if (_taskDataList.Count > 0) {
            Parallel.ForEach(_taskDataList, ProcessPotentialDataOnWorkerThread);
        }
    }

    // This method is executed on worker threads by Parallel.ForEach
    private void ProcessPotentialDataOnWorkerThread(PotentialCalculationTaskData data)
    {
        // Process P1
        Potential p1 = data.potentialToUpdateP1;
        p1.diff = data.diffP1;
        p1.direction = data.directionP1;
        p1.distance = data.precalculatedDistanceP1;
        p1.framecount = data.frameCount;

        if (p1.cpaInfo == null) {
            // This allocation happens on a worker thread. CPAInfo constructor is simple.
            // It's generally better to ensure these are created on the main thread during init if possible.
            // However, given its simplicity (just assigning references), it's likely fine.
            p1.cpaInfo = new CPAInfo(p1.ownship, p1.target); 
        }
        p1.cpaInfo.ReCompute_ThreadSafe(
            data.ownshipPositionP1, data.ownshipVelocityP1, data.ownshipHeadingP1,
            data.targetPositionP1, data.targetVelocityP1, data.targetHeadingP1,
            data.cpaP1_posDiff
        );
        p1.relativeVelocity = p1.cpaInfo.relativeVelocity;
        p1.targetAngle = p1.cpaInfo.targetAngle;

        foreach (var subData in data.subPotentialsDataP1) {
            SubPotential sp = subData.subPotentialToUpdate;
            sp.diff = subData.diff;
            sp.direction = subData.direction;
            sp.distance = subData.precalculatedDistance;
        }

        // Process P2
        Potential p2 = data.potentialToUpdateP2;
        p2.diff = data.diffP2;
        p2.direction = data.directionP2;
        p2.distance = data.precalculatedDistanceP2;
        p2.framecount = data.frameCount;

        if (p2.cpaInfo == null) {
            p2.cpaInfo = new CPAInfo(p2.ownship, p2.target);
        }
        p2.cpaInfo.ReCompute_ThreadSafe(
            data.ownshipPositionP2, data.ownshipVelocityP2, data.ownshipHeadingP2,
            data.targetPositionP2, data.targetVelocityP2, data.targetHeadingP2,
            data.cpaP2_posDiff
        );
        p2.relativeVelocity = p2.cpaInfo.relativeVelocity;
        p2.targetAngle = p2.cpaInfo.targetAngle;

        foreach (var subData in data.subPotentialsDataP2) {
            SubPotential sp = subData.subPotentialToUpdate;
            sp.diff = subData.diff;
            sp.direction = subData.direction;
            sp.distance = subData.precalculatedDistance;
        }
    }
    
    // The original ComputePotentials and ComputeSubPotentials are now integrated into UpdatePotentialsMultiThreaded and ProcessPotentialDataOnWorkerThread
    // public void ComputePotentials(Entity ent1, int ent1Index, Entity ent2, int ent2Index) { ... }
    // public void ComputeSubPotentials(Potential activePotential, Potential otherPotential) { ... }


    public Potential ComputeEntityPotential(Entity ownship, Entity target) {
        if (ownship == null || target == null) return null;
        Potential pot = new Potential(ownship, target);
        // pot.ReCompute(); // If full computation is needed immediately (will use main thread Unity API calls)
        return pot;
    }

    public Potential GetPotential(Entity e1, Entity e2)
    {
        if (!isInitialized) return null;
        if (e1 == null || e2 == null) return null;
        if (e1 == e2) return null;
        
        if (e1.entityClass == EntityClass.Missile || e2.entityClass == EntityClass.Missile)
            return null; 

        if (potentialsDictionary.TryGetValue(e1, out var e1Potentials))
        {
            if (e1Potentials.TryGetValue(e2, out var potential))
            {
                return potential;
            }
        }
        return null;
    }
}

// Assumed definitions (place these in appropriate files or context)
// public enum EntityClass { Missile, Other }
// public class Entity : MonoBehaviour {
//     public Vector3 position { get { return transform.position; } } // Example
//     public Vector3 velocity; // Example
//     public float heading;    // Example
//     public EntityClass entityClass; // Example
//     public AIComponent ai; // Example, where AIComponent has pfList
// }
// public class AIComponent { public List<Transform> pfList; } // Example
// public static class Utils { // Example
//     public const float EPSILON = 0.0001f;
//     public static float Degrees360(float angle) { /* ... */ return 0; }
//     public static float VectorToHeadingDegrees(Vector3 vec) { /* ... */ return 0; }
//     public static float AngleDiffPosNeg(float angle1, float angle2) { /* ... */ return 0; }
// }
// public static class EntityMgr { public static EntityMgr inst; public List<Entity> entities; } // Example
// public static class SelectionMgr { public static SelectionMgr inst; public Entity selectedEntity; } // Example
