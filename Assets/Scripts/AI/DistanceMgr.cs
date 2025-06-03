using System;
using System.Collections.Generic;
using UnityEngine;
// using System.Threading.Tasks; // Replaced by Unity.Jobs
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;

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

    public int FrameCount;

    public List<SubPotential> subPotentials;

    
    public Potential(Entity own, Entity tgt)
    {
        ownship = own;
        target = tgt;
        cpaInfo = new CPAInfo(own, target); // CPAInfo must be initialized here
        subPotentials = new();
        if (own != null && own.ai != null && own.ai.pfList != null)
        {
            foreach(Transform t in own.ai.pfList) {
                if (t == null) continue;
                SubPotential subPotential = new() {
                    pfTransform = t,
                    cachedPftCollider = t.GetComponent<Collider>()
                };
                subPotentials.Add(subPotential);
            }
        }
    }

    // This method is not directly used by DistanceMgr's new multi-threaded update.
    public void ReCompute() {
        FrameCount = Time.frameCount;

        if (target == null || ownship == null) return;

        diff = target.position - ownship.position;
        distance = diff.magnitude;
        direction = diff.normalized;
        
        cpaInfo ??= new CPAInfo(ownship, target);
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

[Serializable]
public class CPAInfo
{
    public Entity ownship; 
    public Entity target;  
    public Vector3 ownShipPosition = Vector3.zero; 
    public Vector3 targetPosition = Vector3.zero;  
    public float time = 0; 
    public float range = 0; 
    public float targetRelativeBearing = 0;
    public float targetAbsBearing = 0;
    public float targetAngle;
    public Vector3 relativeVelocity = Vector3.zero; 

    private Vector3 _velDiff = Vector3.zero;   
    private Vector3 _posDiff = Vector3.zero;   
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
    // This method is now effectively replaced by CPAHelper.CalculateCPA for job-based computation
    // public void ReCompute_ThreadSafe(...) { ... }
};

// Helper class for passing data to worker threads for SubPotential calculation (Main Thread side)
class SubPotentialTaskData {
    public SubPotential subPotentialToUpdate; // Reference to the SubPotential object
    public Vector3 pfPosition;
    public Vector3 targetEntityPosition;
    public float precalculatedDistance;
    public Vector3 diff;
    public Vector3 direction;
}

// Helper class for passing data to worker threads for Potential calculation (Main Thread side)
class PotentialCalculationTaskData {
    public Potential potentialToUpdateP1;
    public Potential potentialToUpdateP2;
    public int frameCount;

    // Data for P1 (ent1 -> ent2)
    public Vector3 ownshipPositionP1; public Vector3 ownshipVelocityP1; public float ownshipHeadingP1;
    public Vector3 targetPositionP1; public Vector3 targetVelocityP1; public float targetHeadingP1;
    public float precalculatedDistanceP1; public Vector3 diffP1; public Vector3 directionP1;
    public Vector3 cpaP1_posDiff; 
    public List<SubPotentialTaskData> subPotentialsDataP1;

    // Data for P2 (ent2 -> ent1)
    public Vector3 ownshipPositionP2; public Vector3 ownshipVelocityP2; public float ownshipHeadingP2;
    public Vector3 targetPositionP2; public Vector3 targetVelocityP2; public float targetHeadingP2;
    public float precalculatedDistanceP2; public Vector3 diffP2; public Vector3 directionP2;
    public Vector3 cpaP2_posDiff; 
    public List<SubPotentialTaskData> subPotentialsDataP2;
}


// --- Burst Job Related Structs ---

// Struct for CPA calculation results, used by the job
public struct CPAJobOutputData
{
    public Vector3 ownShipPositionAtCPA;
    public Vector3 targetPositionAtCPA;
    public float time;
    public float range;
    public float targetRelativeBearing;
    public float targetAbsBearing;
    public float targetAngle;
    public Vector3 relativeVelocity;
}

// Helper for CPA calculations within a Burst job
[BurstCompile]
public static class CPAJobHelper // Made static class for helper methods
{
    public static CPAJobOutputData CalculateCPA(Vector3 osPos, Vector3 osVel, float osHeading,
                                             Vector3 tgtPos, Vector3 tgtVel, float tgtHeading,
                                             Vector3 precomputedPosDiff, float epsilon) // Added epsilon parameter
    {
        CPAJobOutputData output = new();
        output.relativeVelocity = tgtVel - osVel;

        Vector3 _velDiff = osVel - tgtVel;
        float _relSpeedSquared = _velDiff.sqrMagnitude;

        if (_relSpeedSquared < epsilon * 10) // Use passed epsilon
            output.time = 0;
        else
            output.time = -Vector3.Dot(precomputedPosDiff, _velDiff) / _relSpeedSquared;
        
        if (output.time < 0) output.time = 0; 

        output.ownShipPositionAtCPA = osPos + osVel * output.time;
        output.targetPositionAtCPA = tgtPos + tgtVel * output.time;
        
        Vector3 cpaVector = output.targetPositionAtCPA - output.ownShipPositionAtCPA;
        output.range = cpaVector.magnitude;

        // Ensure Utils methods are Burst-compatible (static, no managed types, only blittable math)
        output.targetAbsBearing = Utils.Degrees360(Utils.VectorToHeadingDegrees(cpaVector));
        output.targetRelativeBearing = Utils.Degrees360(Utils.AngleDiffPosNeg(output.targetAbsBearing, osHeading));
        output.targetAngle = Utils.Degrees360(output.targetAbsBearing + 180 - tgtHeading);
        
        return output;
    }
}

// Input data for a pair of potentials for the job
public struct PotentialPairJobInput
{
    public int frameCount;
    // P1 data
    public Vector3 ownshipPositionP1; public Vector3 ownshipVelocityP1; public float ownshipHeadingP1;
    public Vector3 targetPositionP1; public Vector3 targetVelocityP1; public float targetHeadingP1;
    public Vector3 diffP1_mainThread; public Vector3 directionP1_mainThread; 
    public float precalculatedDistanceP1_mainThread; public Vector3 cpaP1_posDiff_mainThread;
    // P2 data
    public Vector3 ownshipPositionP2; public Vector3 ownshipVelocityP2; public float ownshipHeadingP2;
    public Vector3 targetPositionP2; public Vector3 targetVelocityP2; public float targetHeadingP2;
    public Vector3 diffP2_mainThread; public Vector3 directionP2_mainThread; 
    public float precalculatedDistanceP2_mainThread; public Vector3 cpaP2_posDiff_mainThread;
    
    public int subPotentialsP1_Count;
    public int subPotentialsP1_StartIndex; // Index into the flat AllSubPotentialInputs array
    public int subPotentialsP2_Count;
    public int subPotentialsP2_StartIndex; // Index into the flat AllSubPotentialInputs array
}

// Input data for a single sub-potential for the job
public struct SubPotentialJobInput
{
    // Values pre-calculated on the main thread
    public Vector3 diff_mainThread;
    public Vector3 direction_mainThread;
    public float precalculatedDistance_mainThread;
}

// Output data for a pair of potentials from the job
public struct PotentialPairJobOutput
{
    // P1 results
    public Vector3 diffP1; public Vector3 directionP1; public float distanceP1; public int frameCountP1;
    public Vector3 relativeVelocityP1; public float cpaTargetAngleP1;
    public float cpaTimeP1; public float cpaRangeP1;
    public float cpaTargetRelativeBearingP1; public float cpaTargetAbsBearingP1;
    public Vector3 cpaOwnShipPositionP1; public Vector3 cpaTargetPositionP1;

    // P2 results
    public Vector3 diffP2; public Vector3 directionP2; public float distanceP2; public int frameCountP2;
    public Vector3 relativeVelocityP2; public float cpaTargetAngleP2;
    public float cpaTimeP2; public float cpaRangeP2;
    public float cpaTargetRelativeBearingP2; public float cpaTargetAbsBearingP2;
    public Vector3 cpaOwnShipPositionP2; public Vector3 cpaTargetPositionP2;
}

// Output data for a single sub-potential from the job
public struct SubPotentialJobOutput
{
    public Vector3 diff;
    public Vector3 direction;
    public float distance;
}

[BurstCompile]
public struct ProcessPotentialsJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<PotentialPairJobInput> PotentialPairInputs;
    [ReadOnly] public NativeArray<SubPotentialJobInput> AllSubPotentialInputs;
    [ReadOnly] public float Epsilon; // Added Epsilon field

    [WriteOnly] public NativeArray<PotentialPairJobOutput> PotentialPairOutputs;
    [WriteOnly, NativeDisableParallelForRestriction] public NativeArray<SubPotentialJobOutput> AllSubPotentialOutputs;

    public void Execute(int index)
    {
        PotentialPairJobInput jobInputData = PotentialPairInputs[index];
        PotentialPairJobOutput jobOutputData = new();

        // --- Process P1 (potentialToUpdateP1) ---
        jobOutputData.frameCountP1 = jobInputData.frameCount;
        jobOutputData.diffP1 = jobInputData.diffP1_mainThread; 
        jobOutputData.directionP1 = jobInputData.directionP1_mainThread;
        jobOutputData.distanceP1 = jobInputData.precalculatedDistanceP1_mainThread;

        CPAJobOutputData cpaP1 = CPAJobHelper.CalculateCPA(
            jobInputData.ownshipPositionP1, jobInputData.ownshipVelocityP1, jobInputData.ownshipHeadingP1,
            jobInputData.targetPositionP1, jobInputData.targetVelocityP1, jobInputData.targetHeadingP1,
            jobInputData.cpaP1_posDiff_mainThread,
            Epsilon // Pass Epsilon to CPAJobHelper
        );
        jobOutputData.relativeVelocityP1 = cpaP1.relativeVelocity;
        jobOutputData.cpaTimeP1 = cpaP1.time;
        jobOutputData.cpaRangeP1 = cpaP1.range;
        jobOutputData.cpaTargetRelativeBearingP1 = cpaP1.targetRelativeBearing;
        jobOutputData.cpaTargetAbsBearingP1 = cpaP1.targetAbsBearing;
        jobOutputData.cpaTargetAngleP1 = cpaP1.targetAngle;
        jobOutputData.cpaOwnShipPositionP1 = cpaP1.ownShipPositionAtCPA;
        jobOutputData.cpaTargetPositionP1 = cpaP1.targetPositionAtCPA;

        for (int i = 0; i < jobInputData.subPotentialsP1_Count; i++)
        {
            int subInputIndex = jobInputData.subPotentialsP1_StartIndex + i;
            if (subInputIndex < 0 || subInputIndex >= AllSubPotentialInputs.Length) continue; 

            SubPotentialJobInput subInput = AllSubPotentialInputs[subInputIndex];
            SubPotentialJobOutput subOutput = new()
            {
                diff = subInput.diff_mainThread,
                direction = subInput.direction_mainThread,
                distance = subInput.precalculatedDistance_mainThread 
            };
            if (subInputIndex < 0 || subInputIndex >= AllSubPotentialOutputs.Length) continue; 
            AllSubPotentialOutputs[subInputIndex] = subOutput;
        }

        // --- Process P2 (potentialToUpdateP2) ---
        jobOutputData.frameCountP2 = jobInputData.frameCount;
        jobOutputData.diffP2 = jobInputData.diffP2_mainThread;
        jobOutputData.directionP2 = jobInputData.directionP2_mainThread;
        jobOutputData.distanceP2 = jobInputData.precalculatedDistanceP2_mainThread;

        CPAJobOutputData cpaP2 = CPAJobHelper.CalculateCPA(
            jobInputData.ownshipPositionP2, jobInputData.ownshipVelocityP2, jobInputData.ownshipHeadingP2,
            jobInputData.targetPositionP2, jobInputData.targetVelocityP2, jobInputData.targetHeadingP2,
            jobInputData.cpaP2_posDiff_mainThread,
            Epsilon // Pass Epsilon to CPAJobHelper
        );
        jobOutputData.relativeVelocityP2 = cpaP2.relativeVelocity;
        jobOutputData.cpaTimeP2 = cpaP2.time;
        jobOutputData.cpaRangeP2 = cpaP2.range;
        jobOutputData.cpaTargetRelativeBearingP2 = cpaP2.targetRelativeBearing;
        jobOutputData.cpaTargetAbsBearingP2 = cpaP2.targetAbsBearing;
        jobOutputData.cpaTargetAngleP2 = cpaP2.targetAngle;
        jobOutputData.cpaOwnShipPositionP2 = cpaP2.ownShipPositionAtCPA;
        jobOutputData.cpaTargetPositionP2 = cpaP2.targetPositionAtCPA;
        
        for (int i = 0; i < jobInputData.subPotentialsP2_Count; i++)
        {
            int subInputIndex = jobInputData.subPotentialsP2_StartIndex + i;
            if (subInputIndex < 0 || subInputIndex >= AllSubPotentialInputs.Length) continue; 

            SubPotentialJobInput subInput = AllSubPotentialInputs[subInputIndex];
            SubPotentialJobOutput subOutput = new()
            {
                diff = subInput.diff_mainThread,
                direction = subInput.direction_mainThread,
                distance = subInput.precalculatedDistance_mainThread
            };
            if (subInputIndex < 0 || subInputIndex >= AllSubPotentialOutputs.Length) continue; 
            AllSubPotentialOutputs[subInputIndex] = subOutput;
        }
        
        PotentialPairOutputs[index] = jobOutputData;
    }
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


    // Main thread lists to hold task data and references for mapping job results back
    private readonly List<PotentialCalculationTaskData> _mainThreadTaskDataList = new();
    private readonly List<SubPotential> _flatSubPotentialReferences = new(); // For direct mapping of sub-potential results

    // Temporary lists for populating NativeArrays
    private readonly List<PotentialPairJobInput> _tempPotentialPairJobInputs = new();
    private readonly List<SubPotentialJobInput> _tempAllSubPotentialJobInputs = new();


     public void Initialize()
    {
        isInitialized = true;
        potentialsDictionary = new();
        potentialsList = new();
        
        var validEntities = EntityMgr.inst.entities.FindAll(e => e != null && e.entityClass != EntityClass.Missile);
        int n = validEntities.Count;
        
        potentials2D = new Potential[n, n];
        
        for (int i_idx = 0; i_idx < n; i_idx++)
        {
            Entity ent1 = validEntities[i_idx];
            
            Dictionary<Entity, Potential> ent1PotDictionary = new();
            List<Potential> ent1PotList = new();
            potentialsDictionary.Add(ent1, ent1PotDictionary);
            potentialsList.Add(ent1PotList);
            
            for (int j_idx = 0; j_idx < n; j_idx++)
            {
                Entity ent2 = validEntities[j_idx];
                
                Potential pot = new(ent1, ent2);
                ent1PotDictionary.Add(ent2, pot);
                ent1PotList.Add(pot);
                potentials2D[i_idx, j_idx] = pot;
            }
        }
        ii = n; 
        jj = n; 
    } // End of Initialize method

    void OnDestroy() // Changed from Stop() to OnDestroy for typical Unity lifecycle
    {
        // Clear lists that might hold references or large data
        _mainThreadTaskDataList.Clear();
        _flatSubPotentialReferences.Clear();
        _tempPotentialPairJobInputs.Clear();
        _tempAllSubPotentialJobInputs.Clear();
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
            UpdatePotentialsWithJob();
        }
        frameCounter++;
    }

    public List<Potential> selectedEntityPotentials; 

    void UpdatePotentialsWithJob()
    {
        if (potentialsList == null) return;
        int n = potentialsList.Count;
        if (n == 0) return;

        int currentFrameMod = frameCounter % 10; 
        _mainThreadTaskDataList.Clear(); 
        _flatSubPotentialReferences.Clear();
        _tempPotentialPairJobInputs.Clear();
        _tempAllSubPotentialJobInputs.Clear();

        int currentFrame = Time.frameCount; 

        // Phase 1: Data Gathering (Main Thread) - Populates _mainThreadTaskDataList and temporary job input lists
        for (int i_loop = 0; i_loop < n; i_loop++) // Renamed loop variable to avoid conflict
        {
            if (i_loop % 10 != currentFrameMod) continue; 
            if (potentialsList[i_loop] == null || potentialsList[i_loop].Count == 0) continue;

            Entity ent1 = null;
            foreach (var p_check in potentialsList[i_loop]) { // Find ent1 for this row
                if (p_check != null && p_check.ownship != null) {
                    ent1 = p_check.ownship;
                    break;
                }
            }
            
            if (ent1 == null || ent1.entityClass == EntityClass.Missile) continue;

            if (SelectionMgr.inst != null && ent1 == SelectionMgr.inst.selectedEntity)
            {
                selectedEntityPotentials = potentialsList[i_loop];
            }

            for (int j_loop = i_loop + 1; j_loop < n; j_loop++)  // Renamed loop variable
            {
                if (potentialsList[i_loop].Count <= j_loop) continue;
                Potential p1 = potentialsList[i_loop][j_loop]; 
                if (p1 == null || p1.ownship == null || p1.target == null) continue;
                Entity ent2 = p1.target; 
                if (ent2 == null || ent2.entityClass == EntityClass.Missile) continue;

                if (j_loop >= potentialsList.Count || potentialsList[j_loop].Count <= i_loop) continue;
                Potential p2 = potentialsList[j_loop][i_loop]; 
                if (p2 == null || p2.ownship == null || p2.target == null || p2.ownship != ent2 || p2.target != ent1) {
                    Debug.LogWarning($"Could not find or verify symmetric potential for pair ({ent1.name}, {ent2.name})");
                    continue;
                }

                // Create main thread task data (holds references to Potential objects)
                var mtTaskData = new PotentialCalculationTaskData() {
                    potentialToUpdateP1 = p1, potentialToUpdateP2 = p2, frameCount = currentFrame,
                    ownshipPositionP1 = ent1.position, ownshipVelocityP1 = ent1.velocity, ownshipHeadingP1 = ent1.heading,
                    targetPositionP1 = ent2.position, targetVelocityP1 = ent2.velocity, targetHeadingP1 = ent2.heading,
                    subPotentialsDataP1 = new(),
                    ownshipPositionP2 = ent2.position, ownshipVelocityP2 = ent2.velocity, ownshipHeadingP2 = ent2.heading,
                    targetPositionP2 = ent1.position, targetVelocityP2 = ent1.velocity, targetHeadingP2 = ent1.heading,
                    subPotentialsDataP2 = new()
                };

                PotentialPairJobInput jobInputItem = new() { 
                    frameCount = currentFrame,
                    ownshipPositionP1 = mtTaskData.ownshipPositionP1, ownshipVelocityP1 = mtTaskData.ownshipVelocityP1, ownshipHeadingP1 = mtTaskData.ownshipHeadingP1,
                    targetPositionP1 = mtTaskData.targetPositionP1, targetVelocityP1 = mtTaskData.targetVelocityP1, targetHeadingP1 = mtTaskData.targetHeadingP1,
                    ownshipPositionP2 = mtTaskData.ownshipPositionP2, ownshipVelocityP2 = mtTaskData.ownshipVelocityP2, ownshipHeadingP2 = mtTaskData.ownshipHeadingP2,
                    targetPositionP2 = mtTaskData.targetPositionP2, targetVelocityP2 = mtTaskData.targetVelocityP2, targetHeadingP2 = mtTaskData.targetHeadingP2
                };
                
                Collider ownshipColliderP1 = ent1.GetComponent<Collider>();
                Collider targetColliderP1 = ent2.GetComponent<Collider>();

                // --- Main Thread Computations for P1 (ent1 -> ent2) ---
                mtTaskData.diffP1 = mtTaskData.targetPositionP1 - mtTaskData.ownshipPositionP1;
                jobInputItem.diffP1_mainThread = mtTaskData.diffP1;
                mtTaskData.directionP1 = mtTaskData.diffP1.normalized;
                jobInputItem.directionP1_mainThread = mtTaskData.directionP1;

                if (ownshipColliderP1 != null && targetColliderP1 != null) {
                    Vector3 closestOnOwnP1 = ownshipColliderP1.ClosestPoint(mtTaskData.targetPositionP1);
                    Vector3 closestOnTgtP1 = targetColliderP1.ClosestPoint(mtTaskData.ownshipPositionP1);
                    mtTaskData.precalculatedDistanceP1 = Vector3.Distance(closestOnOwnP1, closestOnTgtP1);
                    mtTaskData.cpaP1_posDiff = closestOnOwnP1 - closestOnTgtP1;
                } else {
                    mtTaskData.precalculatedDistanceP1 = mtTaskData.diffP1.magnitude;
                    mtTaskData.cpaP1_posDiff = mtTaskData.ownshipPositionP1 - mtTaskData.targetPositionP1;
                }
                jobInputItem.precalculatedDistanceP1_mainThread = mtTaskData.precalculatedDistanceP1;
                jobInputItem.cpaP1_posDiff_mainThread = mtTaskData.cpaP1_posDiff;
                
                jobInputItem.subPotentialsP1_StartIndex = _tempAllSubPotentialJobInputs.Count;
                if (p1.subPotentials != null) {
                    foreach (SubPotential sp_p1 in p1.subPotentials) {
                        if (sp_p1 == null || sp_p1.pfTransform == null) continue;
                        var subMTData = new SubPotentialTaskData() { subPotentialToUpdate = sp_p1 }; 
                        subMTData.pfPosition = sp_p1.pfTransform.position;
                        subMTData.targetEntityPosition = mtTaskData.targetPositionP1;
                        subMTData.diff = subMTData.targetEntityPosition - subMTData.pfPosition;
                        subMTData.direction = subMTData.diff.normalized;

                        Collider pfSpCollider = sp_p1.cachedPftCollider;
                        if (pfSpCollider != null && targetColliderP1 != null) {
                            Vector3 cOnPf = pfSpCollider.ClosestPoint(subMTData.targetEntityPosition);
                            Vector3 cOnTgt = targetColliderP1.ClosestPoint(subMTData.pfPosition);
                            subMTData.precalculatedDistance = Vector3.Distance(cOnPf, cOnTgt);
                        } else if (pfSpCollider != null) {
                            subMTData.precalculatedDistance = Vector3.Distance(pfSpCollider.ClosestPoint(subMTData.targetEntityPosition), subMTData.targetEntityPosition);
                        } else if (targetColliderP1 != null) {
                            subMTData.precalculatedDistance = Vector3.Distance(targetColliderP1.ClosestPoint(subMTData.pfPosition), subMTData.pfPosition);
                        } else {
                            subMTData.precalculatedDistance = subMTData.diff.magnitude;
                        }
                        mtTaskData.subPotentialsDataP1.Add(subMTData); 
                        _flatSubPotentialReferences.Add(sp_p1);
                        _tempAllSubPotentialJobInputs.Add(new() {
                            diff_mainThread = subMTData.diff,
                            direction_mainThread = subMTData.direction,
                            precalculatedDistance_mainThread = subMTData.precalculatedDistance
                        });
                    }
                }
                jobInputItem.subPotentialsP1_Count = _tempAllSubPotentialJobInputs.Count - jobInputItem.subPotentialsP1_StartIndex;

                // --- Main Thread Computations for P2 (ent2 -> ent1) ---
                mtTaskData.diffP2 = mtTaskData.targetPositionP2 - mtTaskData.ownshipPositionP2;
                jobInputItem.diffP2_mainThread = mtTaskData.diffP2;
                mtTaskData.directionP2 = mtTaskData.diffP2.normalized;
                jobInputItem.directionP2_mainThread = mtTaskData.directionP2;
                mtTaskData.precalculatedDistanceP2 = mtTaskData.precalculatedDistanceP1; // Symmetric
                jobInputItem.precalculatedDistanceP2_mainThread = mtTaskData.precalculatedDistanceP2;

                // Note: ownshipColliderP1 is ent1's collider, targetColliderP1 is ent2's collider
                if (targetColliderP1 != null && ownshipColliderP1 != null) { 
                    Vector3 closestOnOwnP2 = targetColliderP1.ClosestPoint(mtTaskData.targetPositionP2); // ent2's collider, closest to ent1
                    Vector3 closestOnTgtP2 = ownshipColliderP1.ClosestPoint(mtTaskData.ownshipPositionP2); // ent1's collider, closest to ent2
                    mtTaskData.cpaP2_posDiff = closestOnOwnP2 - closestOnTgtP2;
                } else {
                    mtTaskData.cpaP2_posDiff = mtTaskData.ownshipPositionP2 - mtTaskData.targetPositionP2; 
                }
                jobInputItem.cpaP2_posDiff_mainThread = mtTaskData.cpaP2_posDiff;

                jobInputItem.subPotentialsP2_StartIndex = _tempAllSubPotentialJobInputs.Count;
                 if (p2.subPotentials != null) {
                    foreach (SubPotential sp_p2 in p2.subPotentials) {
                        if (sp_p2 == null || sp_p2.pfTransform == null) continue;
                        var subMTData = new SubPotentialTaskData() { subPotentialToUpdate = sp_p2 };
                        subMTData.pfPosition = sp_p2.pfTransform.position;
                        subMTData.targetEntityPosition = mtTaskData.targetPositionP2; // This is ent1's position
                        subMTData.diff = subMTData.targetEntityPosition - subMTData.pfPosition;
                        subMTData.direction = subMTData.diff.normalized;

                        Collider pfSpCollider = sp_p2.cachedPftCollider;
                        // ownshipColliderP1 is ent1's collider, which is the target for p2's subpotentials
                        if (pfSpCollider != null && ownshipColliderP1 != null) {
                            Vector3 cOnPf = pfSpCollider.ClosestPoint(subMTData.targetEntityPosition);
                            Vector3 cOnTgt = ownshipColliderP1.ClosestPoint(subMTData.pfPosition);
                            subMTData.precalculatedDistance = Vector3.Distance(cOnPf, cOnTgt);
                        } else if (pfSpCollider != null) {
                             subMTData.precalculatedDistance = Vector3.Distance(pfSpCollider.ClosestPoint(subMTData.targetEntityPosition), subMTData.targetEntityPosition);
                        } else if (ownshipColliderP1 != null) { // Collider of ent1
                            subMTData.precalculatedDistance = Vector3.Distance(ownshipColliderP1.ClosestPoint(subMTData.pfPosition), subMTData.pfPosition);
                        } else {
                            subMTData.precalculatedDistance = subMTData.diff.magnitude;
                        }
                        mtTaskData.subPotentialsDataP2.Add(subMTData);
                        _flatSubPotentialReferences.Add(sp_p2);
                         _tempAllSubPotentialJobInputs.Add(new() {
                            diff_mainThread = subMTData.diff,
                            direction_mainThread = subMTData.direction,
                            precalculatedDistance_mainThread = subMTData.precalculatedDistance
                        });
                    }
                }
                jobInputItem.subPotentialsP2_Count = _tempAllSubPotentialJobInputs.Count - jobInputItem.subPotentialsP2_StartIndex;
                _mainThreadTaskDataList.Add(mtTaskData);
                _tempPotentialPairJobInputs.Add(jobInputItem);
            }
        }
        
        if (_tempPotentialPairJobInputs.Count == 0) return;

        // Phase 2: Allocate NativeArrays and Schedule Job
        NativeArray<PotentialPairJobInput> potentialPairInputsNat = new(_tempPotentialPairJobInputs.ToArray(), Allocator.TempJob);
        NativeArray<SubPotentialJobInput> allSubPotentialInputsNat = new(_tempAllSubPotentialJobInputs.ToArray(), Allocator.TempJob);
        
        NativeArray<PotentialPairJobOutput> potentialPairOutputsNat = new(_tempPotentialPairJobInputs.Count, Allocator.TempJob);
        NativeArray<SubPotentialJobOutput> allSubPotentialOutputsNat = new(_tempAllSubPotentialJobInputs.Count > 0 ? _tempAllSubPotentialJobInputs.Count : 1, Allocator.TempJob); // Ensure non-zero length if no subpotentials

        var job = new ProcessPotentialsJob
        {
            PotentialPairInputs = potentialPairInputsNat,
            AllSubPotentialInputs = allSubPotentialInputsNat,
            PotentialPairOutputs = potentialPairOutputsNat,
            AllSubPotentialOutputs = allSubPotentialOutputsNat,
            Epsilon = Utils.EPSILON // Set Epsilon for the job
        };
        
        JobHandle jobHandle = job.Schedule(_tempPotentialPairJobInputs.Count, 32); // Adjust batch count as needed
        jobHandle.Complete();

        // Phase 3: Apply Results (Main Thread)
        for (int k = 0; k < _mainThreadTaskDataList.Count; k++) // Renamed loop variable
        {
            PotentialCalculationTaskData mtTaskData = _mainThreadTaskDataList[k];
            PotentialPairJobOutput jobOutput = potentialPairOutputsNat[k];

            // Apply to P1
            Potential p1_res = mtTaskData.potentialToUpdateP1;
            p1_res.diff = jobOutput.diffP1;
            p1_res.direction = jobOutput.directionP1;
            p1_res.distance = jobOutput.distanceP1;
            p1_res.FrameCount = jobOutput.frameCountP1;
            p1_res.relativeVelocity = jobOutput.relativeVelocityP1;
            p1_res.targetAngle = jobOutput.cpaTargetAngleP1;
            if (p1_res.cpaInfo != null) 
            {
                p1_res.cpaInfo.time = jobOutput.cpaTimeP1;
                p1_res.cpaInfo.range = jobOutput.cpaRangeP1;
                p1_res.cpaInfo.targetRelativeBearing = jobOutput.cpaTargetRelativeBearingP1;
                p1_res.cpaInfo.targetAbsBearing = jobOutput.cpaTargetAbsBearingP1;
                p1_res.cpaInfo.targetAngle = jobOutput.cpaTargetAngleP1; // CPA's targetAngle
                p1_res.cpaInfo.relativeVelocity = jobOutput.relativeVelocityP1; 
                p1_res.cpaInfo.ownShipPosition = jobOutput.cpaOwnShipPositionP1;
                p1_res.cpaInfo.targetPosition = jobOutput.cpaTargetPositionP1;
            }
            
            // Apply to P2
            Potential p2_res = mtTaskData.potentialToUpdateP2;
            p2_res.diff = jobOutput.diffP2;
            p2_res.direction = jobOutput.directionP2;
            p2_res.distance = jobOutput.distanceP2;
            p2_res.FrameCount = jobOutput.frameCountP2;
            p2_res.relativeVelocity = jobOutput.relativeVelocityP2;
            p2_res.targetAngle = jobOutput.cpaTargetAngleP2;
             if (p2_res.cpaInfo != null)
             {
                p2_res.cpaInfo.time = jobOutput.cpaTimeP2;
                p2_res.cpaInfo.range = jobOutput.cpaRangeP2;
                p2_res.cpaInfo.targetRelativeBearing = jobOutput.cpaTargetRelativeBearingP2;
                p2_res.cpaInfo.targetAbsBearing = jobOutput.cpaTargetAbsBearingP2;
                p2_res.cpaInfo.targetAngle = jobOutput.cpaTargetAngleP2; // CPA's targetAngle
                p2_res.cpaInfo.relativeVelocity = jobOutput.relativeVelocityP2;
                p2_res.cpaInfo.ownShipPosition = jobOutput.cpaOwnShipPositionP2;
                p2_res.cpaInfo.targetPosition = jobOutput.cpaTargetPositionP2;
            }
        }
        
        // Apply SubPotential results
        for(int l=0; l < _flatSubPotentialReferences.Count; ++l) // Renamed loop variable
        {
            if (l >= allSubPotentialOutputsNat.Length) continue; // Bounds check
            SubPotential sp = _flatSubPotentialReferences[l];
            SubPotentialJobOutput spOutput = allSubPotentialOutputsNat[l];
            sp.diff = spOutput.diff;
            sp.direction = spOutput.direction;
            sp.distance = spOutput.distance;
        }

        // Dispose NativeArrays
        potentialPairInputsNat.Dispose();
        allSubPotentialInputsNat.Dispose();
        potentialPairOutputsNat.Dispose();
        allSubPotentialOutputsNat.Dispose();
    }
    
    public Potential ComputeEntityPotential(Entity ownshipParam, Entity targetParam) {
        if (ownshipParam == null || targetParam == null) return null;
        Potential pot = new(ownshipParam, targetParam);
        // pot.ReCompute(); // This would be a main-thread only, non-job computation
        return pot;
    }

    public Potential GetPotential(Entity e1, Entity e2)
    {
        if (e1 == null || e2 == null) return null;
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