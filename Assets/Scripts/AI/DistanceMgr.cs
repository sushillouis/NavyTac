using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using System.Linq; // Added for ToList()

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
    // Kept for potential direct use or debugging.
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

[BurstCompile]
public static class CPAJobHelper 
{
    public static CPAJobOutputData CalculateCPA(Vector3 osPos, Vector3 osVel, float osHeading,
                                             Vector3 tgtPos, Vector3 tgtVel, float tgtHeading,
                                             Vector3 precomputedPosDiff, float epsilon) 
    {
        CPAJobOutputData output = new();
        output.relativeVelocity = tgtVel - osVel;

        Vector3 _velDiff = osVel - tgtVel;
        float _relSpeedSquared = _velDiff.sqrMagnitude;

        if (_relSpeedSquared < epsilon * 10) 
            output.time = 0;
        else
            output.time = -Vector3.Dot(precomputedPosDiff, _velDiff) / _relSpeedSquared;
        
        if (output.time < 0) output.time = 0; 

        output.ownShipPositionAtCPA = osPos + osVel * output.time;
        output.targetPositionAtCPA = tgtPos + tgtVel * output.time;
        
        Vector3 cpaVector = output.targetPositionAtCPA - output.ownShipPositionAtCPA;
        output.range = cpaVector.magnitude;

        output.targetAbsBearing = Utils.Degrees360(Utils.VectorToHeadingDegrees(cpaVector));
        output.targetRelativeBearing = Utils.Degrees360(Utils.AngleDiffPosNeg(output.targetAbsBearing, osHeading));
        output.targetAngle = Utils.Degrees360(output.targetAbsBearing + 180 - tgtHeading);
        
        return output;
    }
}

public struct PotentialPairJobInput
{
    public int frameCount;
    public Vector3 ownshipPositionP1; public Vector3 ownshipVelocityP1; public float ownshipHeadingP1;
    public Vector3 targetPositionP1; public Vector3 targetVelocityP1; public float targetHeadingP1;
    public Vector3 diffP1_mainThread; public Vector3 directionP1_mainThread; 
    public float precalculatedDistanceP1_mainThread; public Vector3 cpaP1_posDiff_mainThread;
    public Vector3 ownshipPositionP2; public Vector3 ownshipVelocityP2; public float ownshipHeadingP2;
    public Vector3 targetPositionP2; public Vector3 targetVelocityP2; public float targetHeadingP2;
    public Vector3 diffP2_mainThread; public Vector3 directionP2_mainThread; 
    public float precalculatedDistanceP2_mainThread; public Vector3 cpaP2_posDiff_mainThread;
    public int subPotentialsP1_Count;
    public int subPotentialsP1_StartIndex; 
    public int subPotentialsP2_Count;
    public int subPotentialsP2_StartIndex; 
}

public struct SubPotentialJobInput
{
    public Vector3 diff_mainThread;
    public Vector3 direction_mainThread;
    public float precalculatedDistance_mainThread;
}

public struct PotentialPairJobOutput
{
    public Vector3 diffP1; public Vector3 directionP1; public float distanceP1; public int frameCountP1;
    public Vector3 relativeVelocityP1; public float cpaTargetAngleP1;
    public float cpaTimeP1; public float cpaRangeP1;
    public float cpaTargetRelativeBearingP1; public float cpaTargetAbsBearingP1;
    public Vector3 cpaOwnShipPositionP1; public Vector3 cpaTargetPositionP1;
    public Vector3 diffP2; public Vector3 directionP2; public float distanceP2; public int frameCountP2;
    public Vector3 relativeVelocityP2; public float cpaTargetAngleP2;
    public float cpaTimeP2; public float cpaRangeP2;
    public float cpaTargetRelativeBearingP2; public float cpaTargetAbsBearingP2;
    public Vector3 cpaOwnShipPositionP2; public Vector3 cpaTargetPositionP2;
}

public struct SubPotentialJobOutput
{
    public Vector3 diff;
    public Vector3 direction;
    public float distance;
}

[BurstCompile]
public struct BoundaryRepulsionJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<Vector3> EntityPositions;
    [ReadOnly] public NativeArray<Vector3> BoundaryPositions;
    [ReadOnly] public float RepulsiveCoefficient;
    [ReadOnly] public float RepulsiveExponent;
    [ReadOnly] public float BoundaryStrength;
    [ReadOnly] public float MaxDistance;
    [WriteOnly] public NativeArray<Vector3> RepulsionOutputs;

    public void Execute(int index)
    {
        Vector3 entityPos = EntityPositions[index];
        entityPos.y = 0f; // Evaluate repulsion on the horizontal plane only
        Vector3 accumulatedRepulsion = Vector3.zero;

        for (int i = 0; i < BoundaryPositions.Length; i++)
        {
            Vector3 boundaryPos = BoundaryPositions[i];
            boundaryPos.y = 0f;
            Vector3 diff = boundaryPos - entityPos; // Point from entity toward boundary
            float dist = diff.magnitude;
            if (dist <= 0.0001f || dist > MaxDistance) continue;

            Vector3 repDir = diff / dist;
            float safeDist = math.max(dist, 0.001f);
            float magnitude = RepulsiveCoefficient * BoundaryStrength * math.pow(safeDist, RepulsiveExponent);
            accumulatedRepulsion += repDir * magnitude;
        }

        RepulsionOutputs[index] = accumulatedRepulsion;
    }
}

[BurstCompile]
public struct BoundaryRepulsionContribsJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<Vector3> EntityPositions;
    [ReadOnly] public NativeArray<Vector3> BoundaryPositions;
    [ReadOnly] public float RepulsiveCoefficient;
    [ReadOnly] public float RepulsiveExponent;
    [ReadOnly] public float BoundaryStrength;
    [ReadOnly] public float MaxDistance;
    [ReadOnly] public int MaxContributionsPerEntity;

    [WriteOnly] public NativeArray<Vector3> Contributions; // Length = EntityCount * MaxContributionsPerEntity
    [WriteOnly] public NativeArray<int> ContributionCounts; // Length = EntityCount
    [WriteOnly] public NativeArray<int> BoundaryIndices;    // Length = EntityCount * MaxContributionsPerEntity

    public void Execute(int index)
    {
        Vector3 entityPos = EntityPositions[index];
        entityPos.y = 0f; // XZ plane

        int baseOffset = index * MaxContributionsPerEntity;
        int count = 0;

        for (int i = 0; i < BoundaryPositions.Length; i++)
        {
            Vector3 boundaryPos = BoundaryPositions[i];
            boundaryPos.y = 0f;
            Vector3 diff = boundaryPos - entityPos; // Point from entity toward boundary
            float dist = diff.magnitude;
            if (dist <= 0.0001f || dist > MaxDistance) continue;
            if (count >= MaxContributionsPerEntity) break;

            Vector3 repDir = diff / dist;
            float safeDist = math.max(dist, 0.001f);
            float magnitude = RepulsiveCoefficient * BoundaryStrength * math.pow(safeDist, RepulsiveExponent);

            Contributions[baseOffset + count] = repDir * magnitude;
            BoundaryIndices[baseOffset + count] = i;
            count++;
        }

        ContributionCounts[index] = count;
    }
}

[BurstCompile]
public struct ProcessPotentialsJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<PotentialPairJobInput> PotentialPairInputs;
    [ReadOnly] public NativeArray<SubPotentialJobInput> AllSubPotentialInputs;
    [ReadOnly] public float Epsilon; 

    [WriteOnly] public NativeArray<PotentialPairJobOutput> PotentialPairOutputs;
    [WriteOnly, NativeDisableParallelForRestriction] public NativeArray<SubPotentialJobOutput> AllSubPotentialOutputs;

    public void Execute(int index)
    {
        PotentialPairJobInput jobInputData = PotentialPairInputs[index];
        PotentialPairJobOutput jobOutputData = new();

        jobOutputData.frameCountP1 = jobInputData.frameCount;
        jobOutputData.diffP1 = jobInputData.diffP1_mainThread; 
        jobOutputData.directionP1 = jobInputData.directionP1_mainThread;
        jobOutputData.distanceP1 = jobInputData.precalculatedDistanceP1_mainThread;

        CPAJobOutputData cpaP1 = CPAJobHelper.CalculateCPA(
            jobInputData.ownshipPositionP1, jobInputData.ownshipVelocityP1, jobInputData.ownshipHeadingP1,
            jobInputData.targetPositionP1, jobInputData.targetVelocityP1, jobInputData.targetHeadingP1,
            jobInputData.cpaP1_posDiff_mainThread, Epsilon );
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

        jobOutputData.frameCountP2 = jobInputData.frameCount;
        jobOutputData.diffP2 = jobInputData.diffP2_mainThread;
        jobOutputData.directionP2 = jobInputData.directionP2_mainThread;
        jobOutputData.distanceP2 = jobInputData.precalculatedDistanceP2_mainThread;

        CPAJobOutputData cpaP2 = CPAJobHelper.CalculateCPA(
            jobInputData.ownshipPositionP2, jobInputData.ownshipVelocityP2, jobInputData.ownshipHeadingP2,
            jobInputData.targetPositionP2, jobInputData.targetVelocityP2, jobInputData.targetHeadingP2,
            jobInputData.cpaP2_posDiff_mainThread, Epsilon );
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

// Helper container for a pair of potentials (e1->e2 and e2->e1)
public class PotentialPairContainer
{
    public Potential P1; // Potential from e1 to e2
    public Potential P2; // Potential from e2 to e1
}

public class DistanceMgr : MonoBehaviour
{
    public static DistanceMgr inst;
    private void Awake()
    {
        inst = this;
    }

    public bool isInitialized = false;
    
    // New members for spatial grid and on-demand potentials
    private Dictionary<Vector2Int, List<Entity>> _spatialGrid;
    private Dictionary<Tuple<int, int>, PotentialPairContainer> _activePotentials; // Key: (minEntityId, maxEntityId)
    private List<Tuple<int, int>> _activePotentialKeysToProcess; // List of keys from _activePotentials for iteration
    private int _currentProcessingStartIndex = 0; // For round-robin processing of active potentials

    public float cellSize = 500f; // Tune this based on typical engagement ranges / entity density
    public float interactionRange = 2000f; // Entities within this range will have potentials calculated
    private float _interactionRangeSqr;

    public int maxPairsToProcessPerFrame = 50; // Tune this to balance workload per frame
    [Range(0f, 1f)] public float boundaryRepulsionSmoothing = 0.25f;
    public bool computePerPointBoundaryContributions = false; // Enable to compute per-point boundary repulsions per entity
    [Range(1, 64)] public int maxBoundaryContributionsPerEntity = 8; // Up to K contributions per entity

    // Main thread lists to hold task data and references for mapping job results back
    private readonly List<PotentialCalculationTaskData> _mainThreadTaskDataList = new();
    private readonly List<SubPotential> _flatSubPotentialReferences = new(); 

    // Temporary lists for populating NativeArrays
    private readonly List<PotentialPairJobInput> _tempPotentialPairJobInputs = new();
    private readonly List<SubPotentialJobInput> _tempAllSubPotentialJobInputs = new();
    private readonly List<Entity> _boundaryJobEntities = new();
    private readonly List<Entity> _boundaryJobValidEntities = new();
    private readonly Dictionary<int, Vector3> _entityBoundaryRepulsions = new();
    private readonly Dictionary<int, Vector3> _previousBoundaryRepulsions = new();
    // Per-point boundary repulsions (precomputed each frame if enabled)
    private readonly Dictionary<int, Vector3[]> _entityBoundaryPerPointRepulsions = new();
    private readonly Dictionary<int, int[]> _entityBoundaryPerPointIndices = new();
    private readonly Dictionary<int, int> _entityBoundaryPerPointCounts = new();
    
    public List<Potential> selectedEntityPotentials; // For UI or other systems needing potentials for a selected entity

     public void Initialize()
    {
        _spatialGrid = new Dictionary<Vector2Int, List<Entity>>();
        _activePotentials = new Dictionary<Tuple<int, int>, PotentialPairContainer>();
        _activePotentialKeysToProcess = new List<Tuple<int, int>>();
        _interactionRangeSqr = interactionRange * interactionRange;
        
        selectedEntityPotentials = new List<Potential>(); // Initialize the list
        _entityBoundaryRepulsions.Clear();
        _boundaryJobEntities.Clear();
        _previousBoundaryRepulsions.Clear();

        isInitialized = true;
        // No longer pre-calculating all potentials
    }

    void OnDestroy()
    {
        _mainThreadTaskDataList.Clear();
        _flatSubPotentialReferences.Clear();
        _tempPotentialPairJobInputs.Clear();
        _tempAllSubPotentialJobInputs.Clear();

        if (_spatialGrid != null) _spatialGrid.Clear();
        if (_activePotentials != null) _activePotentials.Clear();
        if (_activePotentialKeysToProcess != null) _activePotentialKeysToProcess.Clear();
        if (selectedEntityPotentials != null) selectedEntityPotentials.Clear();
        _boundaryJobEntities.Clear();
        _entityBoundaryRepulsions.Clear();
        _previousBoundaryRepulsions.Clear();
    _entityBoundaryPerPointRepulsions.Clear();
    _entityBoundaryPerPointIndices.Clear();
    _entityBoundaryPerPointCounts.Clear();
    }
    
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
            UpdateSpatialGridAndActivePotentials();
            ProcessActivePotentialsWithJob();
            ComputeBoundaryRepulsions();
            UpdateSelectedEntityPotentials(); // Update after job processing
        }
    }

    Vector2Int GetGridCell(Vector3 position) {
        // Assuming XZ plane for grid
        return new Vector2Int(Mathf.FloorToInt(position.x / cellSize), Mathf.FloorToInt(position.z / cellSize));
    }

    IEnumerable<Vector2Int> GetNeighborCellsIncludingSelf(Vector2Int cell) {
        for (int dx = -1; dx <= 1; dx++) {
            for (int dy = -1; dy <= 1; dy++) {
                yield return new Vector2Int(cell.x + dx, cell.y + dy);
            }
        }
    }
    
    void UpdateSpatialGridAndActivePotentials()
    {
        if (EntityMgr.inst == null || EntityMgr.inst.entities == null) return;

    var relevantEntities = EntityMgr.inst.entities.FindAll(e => e != null && e.entityClass != EntityClass.Missile && e.isActiveAndEnabled);
    _boundaryJobEntities.Clear();
    _boundaryJobEntities.AddRange(relevantEntities);

        _spatialGrid.Clear();
        foreach (Entity entity in relevantEntities)
        {
            if(entity == null) continue; 
            Vector2Int cell = GetGridCell(entity.position);
            if (!_spatialGrid.TryGetValue(cell, out var entitiesInCell))
            {
                entitiesInCell = new List<Entity>();
                _spatialGrid[cell] = entitiesInCell;
            }
            entitiesInCell.Add(entity);
        }

        var newActivePotentials = new Dictionary<Tuple<int, int>, PotentialPairContainer>();
        var consideredPairsThisUpdate = new HashSet<Tuple<int, int>>(); // To avoid duplicate processing in this method

        foreach (Entity e1 in relevantEntities)
        {
            if(e1 == null) continue;
            Vector2Int e1Cell = GetGridCell(e1.position);
            
            foreach (Vector2Int cellToSearch in GetNeighborCellsIncludingSelf(e1Cell))
            {
                if (_spatialGrid.TryGetValue(cellToSearch, out var entitiesInCell))
                {
                    foreach (Entity e2 in entitiesInCell)
                    {
                        if (e2 == null || e1 == e2) continue;

                        int id1 = e1.GetInstanceID();
                        int id2 = e2.GetInstanceID();
                        Entity firstEntity = (id1 < id2) ? e1 : e2; // Canonical order for key
                        Entity secondEntity = (id1 < id2) ? e2 : e1;
                        Tuple<int, int> pairKey = Tuple.Create(firstEntity.GetInstanceID(), secondEntity.GetInstanceID());

                        if (consideredPairsThisUpdate.Contains(pairKey)) continue;
                        consideredPairsThisUpdate.Add(pairKey);

                        if ((firstEntity.position - secondEntity.position).sqrMagnitude < _interactionRangeSqr)
                        {
                            if (_activePotentials.TryGetValue(pairKey, out var existingPairContainer))
                            {
                                // Ensure entities are still valid and match
                                if (existingPairContainer.P1.ownship == firstEntity && existingPairContainer.P1.target == secondEntity &&
                                    firstEntity.isActiveAndEnabled && secondEntity.isActiveAndEnabled)
                                {
                                    newActivePotentials.Add(pairKey, existingPairContainer);
                                }
                                else // Stale references or entities changed, recreate
                                {
                                    Potential newP1 = new Potential(firstEntity, secondEntity);
                                    Potential newP2 = new Potential(secondEntity, firstEntity);
                                    newActivePotentials.Add(pairKey, new PotentialPairContainer { P1 = newP1, P2 = newP2 });
                                }
                            }
                            else
                            {
                                Potential newP1 = new Potential(firstEntity, secondEntity);
                                Potential newP2 = new Potential(secondEntity, firstEntity);
                                newActivePotentials.Add(pairKey, new PotentialPairContainer { P1 = newP1, P2 = newP2 });
                            }
                        }
                    }
                }
            }
        }
        _activePotentials = newActivePotentials;
        _activePotentialKeysToProcess = _activePotentials.Keys.ToList(); // Update the list of keys to iterate over
    }


    void ProcessActivePotentialsWithJob()
    {
        if (_activePotentialKeysToProcess == null || _activePotentialKeysToProcess.Count == 0) return;

        _mainThreadTaskDataList.Clear();
        _flatSubPotentialReferences.Clear();
        _tempPotentialPairJobInputs.Clear();
        _tempAllSubPotentialJobInputs.Clear();
        
        int currentFrame = Time.frameCount;
        List<PotentialPairContainer> pairsForThisJobRun = new List<PotentialPairContainer>();
        int processedCountInLoop = 0; // How many pairs we've decided to process in this call
        int initialKeyCount = _activePotentialKeysToProcess.Count; // Count before potential removals within the loop

        for (int i = 0; i < initialKeyCount && processedCountInLoop < maxPairsToProcessPerFrame; ++i)
        {
            if (_currentProcessingStartIndex >= _activePotentialKeysToProcess.Count)
            {
                _currentProcessingStartIndex = 0; // Wrap around
            }
            // If list becomes empty after wrap-around (e.g., all entities destroyed)
            if (_activePotentialKeysToProcess.Count == 0) break;

            Tuple<int, int> currentKey = _activePotentialKeysToProcess[_currentProcessingStartIndex];
            
            if (!_activePotentials.TryGetValue(currentKey, out var potentialPairContainer))
            {
                // Key in list but not in dictionary (should be rare, indicates inconsistency)
                _activePotentialKeysToProcess.RemoveAt(_currentProcessingStartIndex);
                // Do not increment _currentProcessingStartIndex, as list shifted. Loop continues.
                continue; 
            }

            Potential p1 = potentialPairContainer.P1;
            Potential p2 = potentialPairContainer.P2;

            // Check if entities are still valid (e.g., not destroyed since last grid update)
            if (p1.ownship == null || p1.target == null || !p1.ownship.isActiveAndEnabled || !p1.target.isActiveAndEnabled)
            {
                _activePotentials.Remove(currentKey); // Remove from main dictionary
                _activePotentialKeysToProcess.RemoveAt(_currentProcessingStartIndex); // Remove from iteration list
                // Do not increment _currentProcessingStartIndex, as list shifted.
                continue;
            }
            
            pairsForThisJobRun.Add(potentialPairContainer);
            processedCountInLoop++;
            _currentProcessingStartIndex++; // Move to next key for the *next* frame/batch
        }


        if (pairsForThisJobRun.Count == 0) return;

        // --- Phase 1: Data Gathering for selected pairs (Main Thread) ---
        foreach (var pairContainer in pairsForThisJobRun)
        {
            Potential p1 = pairContainer.P1;
            Potential p2 = pairContainer.P2;
            Entity ent1 = p1.ownship;
            Entity ent2 = p1.target;

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

            mtTaskData.diffP2 = mtTaskData.targetPositionP2 - mtTaskData.ownshipPositionP2;
            jobInputItem.diffP2_mainThread = mtTaskData.diffP2;
            mtTaskData.directionP2 = mtTaskData.diffP2.normalized;
            jobInputItem.directionP2_mainThread = mtTaskData.directionP2;
            mtTaskData.precalculatedDistanceP2 = mtTaskData.precalculatedDistanceP1; 
            jobInputItem.precalculatedDistanceP2_mainThread = mtTaskData.precalculatedDistanceP2;

            if (targetColliderP1 != null && ownshipColliderP1 != null) { 
                Vector3 closestOnOwnP2 = targetColliderP1.ClosestPoint(mtTaskData.targetPositionP2); 
                Vector3 closestOnTgtP2 = ownshipColliderP1.ClosestPoint(mtTaskData.ownshipPositionP2); 
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
                    subMTData.targetEntityPosition = mtTaskData.targetPositionP2; 
                    subMTData.diff = subMTData.targetEntityPosition - subMTData.pfPosition;
                    subMTData.direction = subMTData.diff.normalized;

                    Collider pfSpCollider = sp_p2.cachedPftCollider;
                    if (pfSpCollider != null && ownshipColliderP1 != null) {
                        Vector3 cOnPf = pfSpCollider.ClosestPoint(subMTData.targetEntityPosition);
                        Vector3 cOnTgt = ownshipColliderP1.ClosestPoint(subMTData.pfPosition);
                        subMTData.precalculatedDistance = Vector3.Distance(cOnPf, cOnTgt);
                    } else if (pfSpCollider != null) {
                         subMTData.precalculatedDistance = Vector3.Distance(pfSpCollider.ClosestPoint(subMTData.targetEntityPosition), subMTData.targetEntityPosition);
                    } else if (ownshipColliderP1 != null) { 
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
        
        if (_tempPotentialPairJobInputs.Count == 0) return;

        // Phase 2: Allocate NativeArrays and Schedule Job
        NativeArray<PotentialPairJobInput> potentialPairInputsNat = new(_tempPotentialPairJobInputs.ToArray(), Allocator.TempJob);
        NativeArray<SubPotentialJobInput> allSubPotentialInputsNat = new(_tempAllSubPotentialJobInputs.Count > 0 ? _tempAllSubPotentialJobInputs.ToArray() : new SubPotentialJobInput[1], Allocator.TempJob); // Ensure non-empty if using count
        
        NativeArray<PotentialPairJobOutput> potentialPairOutputsNat = new(_tempPotentialPairJobInputs.Count, Allocator.TempJob);
        NativeArray<SubPotentialJobOutput> allSubPotentialOutputsNat = new(_tempAllSubPotentialJobInputs.Count > 0 ? _tempAllSubPotentialJobInputs.Count : 1, Allocator.TempJob);

        var job = new ProcessPotentialsJob
        {
            PotentialPairInputs = potentialPairInputsNat,
            AllSubPotentialInputs = allSubPotentialInputsNat,
            PotentialPairOutputs = potentialPairOutputsNat,
            AllSubPotentialOutputs = allSubPotentialOutputsNat,
            Epsilon = Utils.EPSILON 
        };
        
        JobHandle jobHandle = job.Schedule(_tempPotentialPairJobInputs.Count, 32); 
        jobHandle.Complete();

        // Phase 3: Apply Results (Main Thread)
        for (int k = 0; k < _mainThreadTaskDataList.Count; k++) 
        {
            PotentialCalculationTaskData mtTaskData = _mainThreadTaskDataList[k];
            PotentialPairJobOutput jobOutput = potentialPairOutputsNat[k];

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
                p1_res.cpaInfo.targetAngle = jobOutput.cpaTargetAngleP1; 
                p1_res.cpaInfo.relativeVelocity = jobOutput.relativeVelocityP1; 
                p1_res.cpaInfo.ownShipPosition = jobOutput.cpaOwnShipPositionP1;
                p1_res.cpaInfo.targetPosition = jobOutput.cpaTargetPositionP1;
            }
            
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
                p2_res.cpaInfo.targetAngle = jobOutput.cpaTargetAngleP2; 
                p2_res.cpaInfo.relativeVelocity = jobOutput.relativeVelocityP2;
                p2_res.cpaInfo.ownShipPosition = jobOutput.cpaOwnShipPositionP2;
                p2_res.cpaInfo.targetPosition = jobOutput.cpaTargetPositionP2;
            }
        }
        
        for(int l=0; l < _flatSubPotentialReferences.Count; ++l) 
        {
            if (l >= allSubPotentialOutputsNat.Length) continue; 
            SubPotential sp = _flatSubPotentialReferences[l];
            SubPotentialJobOutput spOutput = allSubPotentialOutputsNat[l];
            sp.diff = spOutput.diff;
            sp.direction = spOutput.direction;
            sp.distance = spOutput.distance;
        }

        potentialPairInputsNat.Dispose();
        allSubPotentialInputsNat.Dispose();
        potentialPairOutputsNat.Dispose();
        allSubPotentialOutputsNat.Dispose();
    }

    void ComputeBoundaryRepulsions()
    {
        var aimgr = AIMgr.inst;
        if (aimgr == null || aimgr.boundaryPositions == null || aimgr.boundaryPositions.Count == 0 || aimgr.boundaryRepulsionDistance <= 0f)
        {
            _entityBoundaryRepulsions.Clear();
            _entityBoundaryPerPointRepulsions.Clear();
            _entityBoundaryPerPointIndices.Clear();
            _entityBoundaryPerPointCounts.Clear();
            return;
        }

        if (_boundaryJobEntities.Count == 0)
        {
            _entityBoundaryRepulsions.Clear();
            _entityBoundaryPerPointRepulsions.Clear();
            _entityBoundaryPerPointIndices.Clear();
            _entityBoundaryPerPointCounts.Clear();
            return;
        }

        _boundaryJobValidEntities.Clear();
        var validEntities = _boundaryJobValidEntities;
        for (int i = 0; i < _boundaryJobEntities.Count; i++)
        {
            Entity ent = _boundaryJobEntities[i];
            if (ent == null || !ent.isActiveAndEnabled) continue;
            validEntities.Add(ent);
        }

        if (validEntities.Count == 0)
        {
            _entityBoundaryRepulsions.Clear();
            _entityBoundaryPerPointRepulsions.Clear();
            _entityBoundaryPerPointIndices.Clear();
            _entityBoundaryPerPointCounts.Clear();
            return;
        }

        NativeArray<Vector3> entityPositionsNat = new(validEntities.Count, Allocator.TempJob);
        NativeArray<Vector3> boundaryPositionsNat = new(aimgr.boundaryPositions.Count, Allocator.TempJob);
        NativeArray<Vector3> repulsionOutputsNat = new(validEntities.Count, Allocator.TempJob);

        _previousBoundaryRepulsions.Clear();
        foreach (var kvp in _entityBoundaryRepulsions)
        {
            _previousBoundaryRepulsions[kvp.Key] = kvp.Value;
        }

        for (int i = 0; i < validEntities.Count; i++)
        {
            entityPositionsNat[i] = validEntities[i].position;
        }

        for (int i = 0; i < aimgr.boundaryPositions.Count; i++)
        {
            Vector3 pos = aimgr.boundaryPositions[i];
            pos.y = 0f; // enforce XZ plane repulsion
            boundaryPositionsNat[i] = pos;
        }

        var job = new BoundaryRepulsionJob
        {
            EntityPositions = entityPositionsNat,
            BoundaryPositions = boundaryPositionsNat,
            RepulsiveCoefficient = aimgr.repulsive2Coefficient,
            RepulsiveExponent = aimgr.repulsiveExponent,
            BoundaryStrength = math.max(aimgr.boundaryRepulsionStrength, 0.0001f),
            MaxDistance = aimgr.boundaryRepulsionDistance,
            RepulsionOutputs = repulsionOutputsNat
        };

        JobHandle handle = job.Schedule(validEntities.Count, 32);
        handle.Complete();

        _entityBoundaryRepulsions.Clear();
        float smoothing = Mathf.Clamp01(boundaryRepulsionSmoothing);
        for (int i = 0; i < validEntities.Count; i++)
        {
            int id = validEntities[i].GetInstanceID();
            Vector3 current = repulsionOutputsNat[i];
            if (smoothing > 0f && _previousBoundaryRepulsions.TryGetValue(id, out var previous))
            {
                current = Vector3.Lerp(previous, current, smoothing);
            }
            _entityBoundaryRepulsions[id] = current;
        }

        // Optionally compute per-point contributions per entity
        if (computePerPointBoundaryContributions && maxBoundaryContributionsPerEntity > 0)
        {
            int N = validEntities.Count;
            int K = Mathf.Clamp(maxBoundaryContributionsPerEntity, 1, 64);
            NativeArray<Vector3> contribsNat = new(N * K, Allocator.TempJob);
            NativeArray<int> countsNat = new(N, Allocator.TempJob);
            NativeArray<int> indicesNat = new(N * K, Allocator.TempJob);

            var contribJob = new BoundaryRepulsionContribsJob
            {
                EntityPositions = entityPositionsNat,
                BoundaryPositions = boundaryPositionsNat,
                RepulsiveCoefficient = aimgr.repulsive2Coefficient,
                RepulsiveExponent = aimgr.repulsiveExponent,
                BoundaryStrength = math.max(aimgr.boundaryRepulsionStrength, 0.0001f),
                MaxDistance = aimgr.boundaryRepulsionDistance,
                MaxContributionsPerEntity = K,
                Contributions = contribsNat,
                ContributionCounts = countsNat,
                BoundaryIndices = indicesNat
            };

            JobHandle contribHandle = contribJob.Schedule(N, 32);
            contribHandle.Complete();

            // Copy results to managed containers for easy access
            _entityBoundaryPerPointRepulsions.Clear();
            _entityBoundaryPerPointIndices.Clear();
            _entityBoundaryPerPointCounts.Clear();

            for (int i = 0; i < N; i++)
            {
                int id = validEntities[i].GetInstanceID();
                int count = Mathf.Clamp(countsNat[i], 0, K);
                _entityBoundaryPerPointCounts[id] = count;

                // Reuse arrays if possible
                Vector3[] repArray;
                if (!_entityBoundaryPerPointRepulsions.TryGetValue(id, out repArray) || repArray == null || repArray.Length != K)
                {
                    repArray = new Vector3[K];
                }

                int[] idxArray;
                if (!_entityBoundaryPerPointIndices.TryGetValue(id, out idxArray) || idxArray == null || idxArray.Length != K)
                {
                    idxArray = new int[K];
                }

                int baseOffset = i * K;
                for (int c = 0; c < count; c++)
                {
                    repArray[c] = contribsNat[baseOffset + c];
                    idxArray[c] = indicesNat[baseOffset + c];
                }

                _entityBoundaryPerPointRepulsions[id] = repArray;
                _entityBoundaryPerPointIndices[id] = idxArray;
            }

            contribsNat.Dispose();
            countsNat.Dispose();
            indicesNat.Dispose();
        }
        else
        {
            _entityBoundaryPerPointRepulsions.Clear();
            _entityBoundaryPerPointIndices.Clear();
            _entityBoundaryPerPointCounts.Clear();
        }

        entityPositionsNat.Dispose();
        boundaryPositionsNat.Dispose();
        repulsionOutputsNat.Dispose();
    }

    void UpdateSelectedEntityPotentials()
    {
        if (SelectionMgr.inst != null && SelectionMgr.inst.selectedEntity != null)
        {
            if (selectedEntityPotentials == null) selectedEntityPotentials = new List<Potential>();
            selectedEntityPotentials.Clear();
            Entity selected = SelectionMgr.inst.selectedEntity;

            foreach (var kvp in _activePotentials)
            {
                Potential p1 = kvp.Value.P1; // e.g., ownship -> target
                if (p1.ownship == selected)
                {
                    selectedEntityPotentials.Add(p1);
                }
                // No need to check P2.ownship, as P1.ownship covers one side of the pair.
                // If selected is P1.target, then P2.ownship would be selected.
                // We only want potentials *from* the selected entity.
            }
        }
        else if (selectedEntityPotentials != null)
        {
            selectedEntityPotentials.Clear();
        }
    }

    public Vector3 GetBoundaryRepulsion(Entity entity)
    {
        if (entity == null) return Vector3.zero;
        return _entityBoundaryRepulsions.TryGetValue(entity.GetInstanceID(), out var repulsion) ? repulsion : Vector3.zero;
    }
    
    // Get precomputed per-point boundary repulsion contributions (if computePerPointBoundaryContributions is enabled).
    // Returns the count written to outVectors/indices (up to K per entity). If disabled or none available, returns 0.
    public int GetPrecomputedBoundaryRepulsionContributions(
        Entity entity,
        List<Vector3> outVectors,
        List<int> outBoundaryIndices = null)
    {
        outVectors?.Clear();
        outBoundaryIndices?.Clear();
        if (entity == null) return 0;

        int id = entity.GetInstanceID();
        if (_entityBoundaryPerPointCounts.TryGetValue(id, out var count)
            && _entityBoundaryPerPointRepulsions.TryGetValue(id, out var vecs))
        {
            int n = Mathf.Min(count, vecs.Length);
            if (outVectors != null)
            {
                for (int i = 0; i < n; i++) outVectors.Add(vecs[i]);
            }
            if (outBoundaryIndices != null && _entityBoundaryPerPointIndices.TryGetValue(id, out var inds))
            {
                for (int i = 0; i < n && i < inds.Length; i++) outBoundaryIndices.Add(inds[i]);
            }
            return n;
        }
        return 0;
    }
    
    // Returns the individual repulsion vectors contributed by each boundary point within the provided radius.
    // This does NOT sum them; it gives you one vector per contributing boundary point so you can process/visualize separately.
    // The calculation mirrors BoundaryRepulsionJob (XZ plane only), but runs on the main thread for ad-hoc queries.
    public int GetBoundaryRepulsionContributionsWithinRadius(
        Entity entity,
        List<Vector3> outRepulsionVectors,
        float radius = 200f,
        List<int> outBoundaryIndices = null,
        bool planarXZ = true)
    {
        outRepulsionVectors?.Clear();
        outBoundaryIndices?.Clear();

        if (entity == null || AIMgr.inst == null) return 0;
        var aimgr = AIMgr.inst;
        var boundaryPts = aimgr.boundaryPositions;
        if (boundaryPts == null || boundaryPts.Count == 0 || radius <= 0f) return 0;

        Vector3 entPos = entity.position;
        if (planarXZ) entPos.y = 0f;

        float maxRadius = Mathf.Min(radius, Mathf.Max(aimgr.boundaryRepulsionDistance, 0.0001f));
        float coef = aimgr.repulsive2Coefficient;
        float exp = aimgr.repulsiveExponent;
        float strength = Mathf.Max(aimgr.boundaryRepulsionStrength, 0.0001f);

        int count = 0;
        for (int i = 0; i < boundaryPts.Count; i++)
        {
            Vector3 b = boundaryPts[i];
            if (planarXZ) b.y = 0f;
            Vector3 diff = b - entPos; // from entity toward boundary point
            float dist = diff.magnitude;
            if (dist <= 0.0001f || dist > maxRadius) continue;

            Vector3 dir = diff / dist;
            float safeDist = math.max(dist, 0.001f);
            float magnitude = coef * strength * math.pow(safeDist, exp);
            Vector3 repulsion = dir * magnitude;

            if (outRepulsionVectors != null) outRepulsionVectors.Add(repulsion);
            if (outBoundaryIndices != null) outBoundaryIndices.Add(i);
            count++;
        }

        return count;
    }
    
    public Potential ComputeEntityPotential(Entity ownshipParam, Entity targetParam) {
        if (ownshipParam == null || targetParam == null) return null;
        // This method is for one-off calculations, not part of the main job loop.
        // It will not benefit from the job system directly unless adapted.
        Potential pot = new(ownshipParam, targetParam);
        pot.ReCompute(); // Perform immediate, single-threaded computation.
        return pot;
    }

    public Potential GetPotential(Entity e1, Entity e2)
    {
        if (e1 == null || e2 == null || e1.entityClass == EntityClass.Missile || e2.entityClass == EntityClass.Missile)
            return null; 

        int id1 = e1.GetInstanceID();
        int id2 = e2.GetInstanceID();
        
        Tuple<int, int> pairKey = (id1 < id2) ? Tuple.Create(id1, id2) : Tuple.Create(id2, id1);

        if (_activePotentials.TryGetValue(pairKey, out var container))
        {
            // Return the potential where e1 is the ownship
            if (container.P1.ownship == e1 && container.P1.target == e2) return container.P1;
            if (container.P2.ownship == e1 && container.P2.target == e2) return container.P2; // Should be this if P1.ownship != e1
        }
        return null; // Pair is not active or not found
    }
}