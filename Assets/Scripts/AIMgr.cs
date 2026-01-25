using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements.Experimental;
[Serializable]
public struct TactCommandStruct: INetworkSerializable, IEquatable<TactCommandStruct>
{
    public TactCommandTypes commandType;
    public int[] entityIds;
    public int targetEntityId;
    public Vector3 targetOrOffsetPosition;
    public bool add;
    public bool useLowestCruiseSpeed;

    public bool Equals(TactCommandStruct other) {
        return (commandType == other.commandType
            && IntArrayEqual(entityIds, other.entityIds)
            && targetEntityId == other.targetEntityId
            && targetOrOffsetPosition == other.targetOrOffsetPosition
            && add == other.add
            && useLowestCruiseSpeed == other.useLowestCruiseSpeed);
    }

    public bool IntArrayEqual(int[] a, int[] b) {
        if(a.Length != b.Length) return false;

        for(int i = 0; i < a.Length; i++) {
            if(a[i] != b[i])
                return false;
        }
        return true;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        serializer.SerializeValue(ref commandType);
        if(serializer.IsWriter) {//serialize deserialize array
            int length = entityIds.Length;
            serializer.SerializeValue(ref length);
            for(int i = 0; i < length; i++) {
                serializer.SerializeValue(ref entityIds[i]);
            }

        } else {
            int length = 0;
            serializer.SerializeValue(ref length);
            entityIds = new int[length];
            for(int i = 0; i < length; i++) {
                serializer.SerializeValue(ref entityIds[i]);
            }
        }
        serializer.SerializeValue(ref targetEntityId);
        serializer.SerializeValue(ref targetOrOffsetPosition);
        serializer.SerializeValue(ref add);
        serializer.SerializeValue(ref useLowestCruiseSpeed);
    }

    public override string ToString() {
        StringBuilder sb = new StringBuilder();
        sb.Append("[");
        for(int i = 0; i < entityIds.Length; i++) {
            sb.Append(entityIds[i].ToString() + ", ");
        }
        sb.Append("]");
        return $"Eid: {sb.ToString()}, Cmd: {commandType}, TGT: {targetEntityId}, Pos: {targetOrOffsetPosition}, Add?: {add}, UseLowestCruiseSpeed?: {useLowestCruiseSpeed}";
    }
}

public class AIMgr : NetworkBehaviour
{
    public static AIMgr inst;
    private void Awake()
    {
        inst = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        layerMask = LayerMask.GetMask("Ocean", "Terrain");
        if (autoPopulateBoundaryPositions)
        {
            PopulateBoundaryPositions("TerrainBoundary");
        }
    }

    public bool isPotentialFieldsMovement = false;
    public float potentialDistanceThreshold = 1000;
    public float potentialDistanceThresholdSq = 25000000;
    public float attractionCoefficient = 500;
    public float attractiveExponent = -1;
    public float repulsiveCoefficient = 60000;
    public float groupRepulsiveCoefficient = 6000;
    public float repulsiveExponent = -2.0f;
    [Header("Experimatal PF")]
    public float repulsive2Coefficient = 1000;
    public float attraction2Coefficient = 10000;
    [Header("Boundary Avoidance")]
    // List of boundary positions (e.g., markers placed along terrain boundary) to avoid
    [FormerlySerializedAs("boundaryObjects")]
    public List<Vector3> boundaryPositions = new List<Vector3>();
    // maximum distance at which boundary objects will produce repulsion
    public float boundaryRepulsionDistance = 200f;
    // strength multiplier for boundary repulsion
    public float boundaryRepulsionStrength = 1f;
    // If true, populate boundary positions automatically (prefers TerrainBoundaryCreator data)
    [FormerlySerializedAs("autoPopulateBoundaryObjects")]
    public bool autoPopulateBoundaryPositions = true;
    // In AIMgr.cs
    [Header("Terrain Avoidance (Ships)")]
    public float terrainDetectionRadius = 100f; // How far ships detect islands
    public float maxTerrainRepulsion = 3000f;    // Maximum push force
    public float minSafeDistance = 20f;          // Closest allowed to terrain
    public float collisionRepulsionCoefficient = 5.0f;
    [Header("//Debug")]
    public bool showTerrainAvoidance = true;
    public LayerMask terrainLayerMask;

    public RaycastHit hit;
    public int layerMask;

    // Gather boundary positions from active TerrainBoundaryCreator components or fall back to a layer search.
    public void PopulateBoundaryPositions(string layerName)
    {
        boundaryPositions.Clear();
        HashSet<Vector3> seen = new HashSet<Vector3>();

        TerrainBoundaryCreator[] creators = FindObjectsOfType<TerrainBoundaryCreator>();
        for (int i = 0; i < creators.Length; i++)
        {
            var positions = creators[i].BoundaryPositions;
            if (positions == null)
            {
                continue;
            }

            for (int j = 0; j < positions.Count; j++)
            {
                Vector3 pos = positions[j];
                pos.y = 0f;
                if (seen.Add(pos))
                {
                    boundaryPositions.Add(pos);
                }
            }
        }

        if (boundaryPositions.Count > 0 || string.IsNullOrEmpty(layerName))
        {
            return;
        }

        int layer = LayerMask.NameToLayer(layerName);
        if (layer == -1)
        {
            return;
        }

        GameObject[] allGOs = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < allGOs.Length; i++)
        {
            var stack = new Stack<GameObject>();
            stack.Push(allGOs[i]);

            while (stack.Count > 0)
            {
                GameObject go = stack.Pop();
                Vector3 pos = go.transform.position;
                pos.y = 0f;
                if (go.layer == layer && seen.Add(pos))
                {
                    boundaryPositions.Add(pos);
                }

                Transform goTransform = go.transform;
                for (int childIndex = 0; childIndex < goTransform.childCount; childIndex++)
                {
                    stack.Push(goTransform.GetChild(childIndex).gameObject);
                }
            }
        }
    }
    public List<Entity> selectedEntities = new List<Entity>();


    // Update is called once per frame
    void Update()
    {

    }

    //I need to be entity owner to command entities.
    //If I select a number of entities, I will only command the entities I own
    // Does not yet handle AI players
    // In AIMgr.cs

    [ContextMenu("Values")]
    public void Values()
    {
        StringBuilder sb = new StringBuilder("AIMgr Values:\n");
        sb.AppendLine($"attractionCoefficient: {attractionCoefficient}");
        sb.AppendLine($"attractiveExponent: {attractiveExponent}");
        sb.AppendLine($"repulsiveCoefficient: {repulsiveCoefficient}");
        sb.AppendLine($"repulsiveExponent: {repulsiveExponent}");
        sb.AppendLine($"repulsive2Coefficient: {repulsive2Coefficient}");
        sb.AppendLine($"attraction2Coefficient: {attraction2Coefficient}");
        sb.AppendLine($"boundaryRepulsionDistance: {boundaryRepulsionDistance}");
        sb.AppendLine($"boundaryRepulsionStrength: {boundaryRepulsionStrength}");
        sb.AppendLine($"potentialDistanceThreshold: {potentialDistanceThreshold}");
        sb.AppendLine($"potentialDistanceThresholdSq: {potentialDistanceThresholdSq}");
        sb.AppendLine($"isPotentialFieldsMovement: {isPotentialFieldsMovement}");
        sb.AppendLine($"terrainDetectionRadius: {terrainDetectionRadius}");
        sb.AppendLine($"maxTerrainRepulsion: {maxTerrainRepulsion}");
        sb.AppendLine($"minSafeDistance: {minSafeDistance}");
        sb.AppendLine($"collisionRepulsionCoefficient: {collisionRepulsionCoefficient}");
        sb.AppendLine($"showTerrainAvoidance: {showTerrainAvoidance}");
        sb.AppendLine($"autoPopulateBoundaryPositions: {autoPopulateBoundaryPositions}");
        Debug.Log(sb.ToString());

    }
public void HandleCommand(Vector2 mousePos, bool intercept, bool attackMove, bool add)
{
    selectedEntities = SelectionMgr.inst.selectedEntities;
    if (selectedEntities.Count > 0)
    {
        foreach (Entity ent in selectedEntities)
        {
            if (ent.entityType == EntityType.Rig_Balder || ent.entityClass == EntityClass.Missile) return;
        }
        if (Physics.Raycast(Camera.main.ScreenPointToRay(mousePos), out hit, float.MaxValue, layerMask))
        {
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Terrain"))
            {
                return;
            }

            Vector3 pos = hit.point;
            pos.y = 0;
            Entity ent = UIMgr.inst.FindClosestEntInRadius(pos);
            if (ent != null && !ent.isVisible && ent.entityClass == EntityClass.Missile && !ent.isGreyed) ent = null;

            // Record command
            if (ReplayMgr.inst != null)
            {
                string commandType;
                int targetEntityId = -1;
                if (attackMove)
                {
                    commandType = ent != null ? "AttackMoveToEntity" : "AttackMoveToPosition";
                    if (ent != null) targetEntityId = ent.entityId;
                }
                else
                {
                    commandType = "Move";
                }

                string targetEntityName = null;
                string targetOwnerName = null;
                if (targetEntityId != -1 && EntityMgr.inst.entitiesDict.TryGetValue(targetEntityId, out Entity targetEntity) && targetEntity != null)
                {
                    targetEntityName = targetEntity.name;
                    if (targetEntity.owner != null)
                    {
                        targetOwnerName = targetEntity.owner.name;
                    }
                }

                ReplayCommand cmd = new()
                {
                    timestamp = Time.time ,
                    timeScale = Time.timeScale,
                    commandType = commandType,
                    entityIds = selectedEntities.Select(e => e.entityId).ToArray(),
                    targetPosition = pos,
                    targetEntityId = targetEntityId,
                    targetEntityName = targetEntityName,
                    targetOwnerName = targetOwnerName,
                    add = add
                };
                Debug.Log($"Recording command: {cmd.commandType} at {cmd.targetPosition} for entities: {string.Join(", ", cmd.entityIds)}");
                
                ReplayMgr.inst.RecordCommand(cmd);
            }

            if (ent == null)
            {
                if (attackMove)
                    HandleAttackMove(SelectionMgr.inst.selectedEntities, pos, null, add, useLowestCruiseSpeed: true);
                else
                    HandleMove(SelectionMgr.inst.selectedEntities, pos, add, useLowestCruiseSpeed: true);
            }
            else
            {
                if (attackMove)
                    HandleAttackMove(SelectionMgr.inst.selectedEntities, pos, ent, add, useLowestCruiseSpeed: true);
                else
                    HandleMove(SelectionMgr.inst.selectedEntities, pos, add, useLowestCruiseSpeed: true);
            }
        }
    }
}

    // public void HandleMove(List<Entity> entities, Vector3 point, bool add, 
    //                   bool isLocalCommand = true, bool maxSpeedMovement = false, bool useFormation = false, FormationType formationType = FormationType.Circle)
    // Constructor for position-based attack-move
public void HandleAttackMove(List<Entity> entities, Vector3 point, Entity target, bool add = false, bool isLocalCommand = true, bool maxSpeedMovement = false, bool acquireTarget = false, float doneDistanceSq = 0f, bool useLowestCruiseSpeed = false)
{
    if (isLocalCommand)
    {
        NetTellAllClients(TactCommandTypes.AttackMove, entities, point, target, add, useLowestCruiseSpeed);
    }

    float groupSpeed = -1f;
    if (useLowestCruiseSpeed && entities.Count > 1)
    {
        float lowestCruiseSpeed = float.MaxValue;
        foreach (Entity entity in entities)
        {
            if (entity.maxSpeed < lowestCruiseSpeed)
            {
                lowestCruiseSpeed = entity.maxSpeed;
            }
        }
        groupSpeed = lowestCruiseSpeed;
    }

    foreach (Entity entity in entities)
    {
        if (entity == null || entity.gameObject == null || entity.isGreyed)
            continue;

        UnitAI uai = entity.GetComponentInChildren<UnitAI>();
        var startPos = (add && uai != null) ? uai.GetQueueTailPosition() : entity.position;

        Pathfinding.inst.StartFindPath(startPos, point, (waypoints, success) => {
            if (success && waypoints.Length > 0)
            {
                if (uai == null) uai = entity.GetComponentInChildren<UnitAI>();
                if (uai != null)
                {
                    if (!add)
                    {
                        uai.StopAndRemoveAllCommands();
                    }

                    for (int i = 0; i < waypoints.Length; i++)
                    {
                        Vector3 waypoint = waypoints[i];
                        bool isLastWaypoint = (i == waypoints.Length - 1);

                        float currentDoneDistanceSq = isLastWaypoint
                            ? StoppingDistanceSq(entity, entities.Count)
                            : 1000f * 1000f;

                        AttackMove am = isLastWaypoint && target != null
                            ? new AttackMove(entity, target, acquireTargetsOnWay: acquireTarget, maxSpeedMovement, currentDoneDistanceSq)
                            : new AttackMove(entity, waypoint, maxSpeedMovement, currentDoneDistanceSq, !isLastWaypoint);
                        
                        uai.AddCommand(am);
                    }
                }
            }
            else
            {
                // Fallback to direct attack-move if pathfinding fails
                HandleDirectAttackMove(entity, point, target, add, maxSpeedMovement, acquireTarget, doneDistanceSq, entities.Count);
            }
        });
    }
}

private void HandleDirectAttackMove(Entity entity, Vector3 point, Entity target, bool add, bool maxSpeedMovement, bool acquireTarget, float doneDistanceSq, int entitiesCount, float groupSpeed = -1f)
{
    float currentDoneDistanceSq = StoppingDistanceSq(entity, entitiesCount);
    AttackMove am = target != null
        ? new AttackMove(entity, target, acquireTargetsOnWay: acquireTarget, maxSpeedMovement, currentDoneDistanceSq)
        : new AttackMove(entity, point, maxSpeedMovement, currentDoneDistanceSq);

    UnitAI uai = entity.GetComponentInChildren<UnitAI>();
    if (uai != null)
    {
        AddOrSet(am, uai, add);
    }
}

    public void HandleMove(List<Entity> entities, Vector3 point,
                      bool add = false, bool isLocalCommand = true, bool maxSpeedMovement = false, float doneDistanceSq = 0f, bool useLowestCruiseSpeed = false)
    {
        if (isLocalCommand)
        {
            NetTellAllClients(TactCommandTypes.Move, entities, point, null, add, useLowestCruiseSpeed);
        }

        float groupSpeed = -1f;
        if (useLowestCruiseSpeed && entities.Count > 1)
        {
            float lowestCruiseSpeed = float.MaxValue;
            foreach (Entity entity in entities)
            {
                if (entity.maxSpeed < lowestCruiseSpeed)
                {
                    lowestCruiseSpeed = entity.maxSpeed;
                }
            }
            groupSpeed = lowestCruiseSpeed;
        }

        foreach (Entity entity in entities)
        {
            if(entity.isGreyed)
                continue; 

            UnitAI uai = entity.GetComponentInChildren<UnitAI>();
            var startPos = (add && uai != null) ? uai.GetQueueTailPosition() : entity.position;

            Pathfinding.inst.StartFindPath(startPos, point, (waypoints, success) => {
                if (success && waypoints.Length > 0)
                {
                    if (uai == null) uai = entity.GetComponentInChildren<UnitAI>();
                    if (uai != null)
                    {
                        if (!add)
                        {
                            uai.StopAndRemoveAllCommands();
                        }

                        for (int i = 0; i < waypoints.Length; i++)
                        {
                            Vector3 waypoint = waypoints[i];
                            float currentDoneDistanceSq;
                            if (i < waypoints.Length - 1)
                            {
                                currentDoneDistanceSq = 1000f * 1000f;
                            }
                            else 
                            {
                               currentDoneDistanceSq = StoppingDistanceSq(entity, entities.Count);
                            }
                            // Debug.Log($"AIMgr: Creating Move command to waypoint {waypoint} with doneDistanceSq {currentDoneDistanceSq}");
                            Move m = new Move(entity, waypoint, maxSpeedMovement, currentDoneDistanceSq, i < waypoints.Length - 1, groupSpeed);
                            uai.AddCommand(m);
                        }
                    }
                }
                else
                {
                    HandleDirectMove(entity, point, add, maxSpeedMovement, doneDistanceSq, entities.Count, groupSpeed);
                }
            });
        }
    }

    private void HandleDirectMove(Entity entity, Vector3 point, bool add, bool maxSpeedMovement, float doneDistanceSq, int entitiesCount, float groupSpeed = -1f)
    {
        float currentDoneDistanceSq = StoppingDistanceSq(entity, entitiesCount);
        
        Move m = new Move(entity, point, maxSpeedMovement, currentDoneDistanceSq, groupSpeed: groupSpeed);
        UnitAI uai = entity.GetComponentInChildren<UnitAI>();
        if (uai != null)
        {
            AddOrSet(m, uai, add);
        }
    }

    public float StoppingDistanceSq(Entity entity, int entitiesCount = 1)
    {
        if (entitiesCount == 1)
        {
            return 200f * 200f;
        }
        else if (entitiesCount < 5)
        {
            return 500f * 500f;
        }
        else if (entitiesCount >= 5)
        {
            WeaponsAspect weaponsAspect = entity.GetComponentInChildren<WeaponsAspect>();
            return weaponsAspect.weapon.range * weaponsAspect.weapon.range;
        }
        else
        {
            return 1000f * 1000f;
        }
    }
    void AddOrSet(Command c, UnitAI uai, bool add)
    {
        //if I can command ent, execute command
        if(add)
            uai.AddCommand(c);
        else
            uai.SetCommand(c);
    }

    public void HandleFollow(List<Entity> entities, Entity ent, Vector3 offset, bool add, bool isLocalCommand = true)
    {
        if(isLocalCommand) {
            NetTellAllClients(TactCommandTypes.Follow, entities, offset, ent, add);
        }
        foreach(Entity entity in entities) {
            if(ent != entity) {
                Follow f = new Follow(entity, ent, offset);
                UnitAI uai = entity.GetComponentInChildren<UnitAI>();
                AddOrSet(f, uai, add);
            }
        }
    }

    public void HandleIntercept(List<Entity> entities, Entity ent, bool add, bool isLocalCommand = true)
    {
        if(isLocalCommand) {
            NetTellAllClients(TactCommandTypes.Intercept, entities, Vector3.zero, ent, add);
        }
        foreach(Entity entity in entities) {
            if(ent != entity) {
                Intercept intercept = new Intercept(entity, ent);
                UnitAI uai = entity.GetComponentInChildren<UnitAI>();
                AddOrSet(intercept, uai, add);
            }
        }

    }

    public void Handle3dIntercept(List<Entity> entities, Entity ent, bool add, bool isLocalCommand = true)
    {
        if(isLocalCommand) {
            NetTellAllClients(TactCommandTypes.Intercept3d, entities, Vector3.zero, ent, add);
        } 
        foreach(Entity entity in entities){
            if(ent != entity) {
                Intercept3d intercept3d = new Intercept3d(entity, ent);
                UnitAI uai = entity.GetComponentInChildren<UnitAI>();
                AddOrSet(intercept3d, uai, add);
            }
        }

    }
    public void HandleSmartIntercept(List<Entity> entities, Entity ent, bool add)
    {
        foreach(Entity entity in entities)
        {
            if(ent != entity)
            {
                SmartIntercept smartIntercept = new SmartIntercept(entity, ent);
                UnitAI uai = entity.GetComponentInChildren<UnitAI>();
                AddOrSet(smartIntercept, uai, add);
            }
        }

    }

    //Networking -----------------------------------------------------------------
    void NetTellAllClients(TactCommandTypes cmdType, List<Entity> entities, Vector3 pos, Entity target, bool add, bool useLowestCruiseSpeed = false) {
        if(!OpenOceanMain.inst.isSinglePlayer) {
            TactCommandStruct netCommand = MakeNetCommandStruct(cmdType, entities, pos, target, add, useLowestCruiseSpeed);
            OpenOceanMain.inst.localTactNetMgr.CommandUpdateServerRpc(netCommand);
        }
    }
    

    TactCommandStruct MakeNetCommandStruct(TactCommandTypes cmdType, List<Entity> entities, Vector3 pos, Entity target, bool add, bool useLowestCruiseSpeed = false) {
        TactCommandStruct netCommand = new TactCommandStruct();

        netCommand.commandType = cmdType;
        netCommand.add = add;
        netCommand.useLowestCruiseSpeed = useLowestCruiseSpeed;
        if(target != null)
            netCommand.targetEntityId = target.entityId;
        netCommand.targetOrOffsetPosition = pos;

        netCommand.entityIds = new int[entities.Count];
        int i = 0;
        foreach(Entity ent in entities) {
            netCommand.entityIds[i] = ent.entityId;
            i++;
        }

        return netCommand;
    }


    public void HandleNetCommandSpec(TactCommandStruct command) {
        ////Debug.Log(OwnerClientId + " recvd Command: " + command.ToString());

        List<Entity> entities = new List<Entity>();
        Entity entTmp;
        for(int i = 0; i < command.entityIds.Length; i++) {
            entTmp = EntityMgr.inst.entitiesDict[command.entityIds[i]];
            if(entTmp != null) {
                entities.Add(entTmp);
            }
        }

        switch(command.commandType) {
            case TactCommandTypes.Move:
                ////Debug.Log("NetCmd: MoveTo pos:" + command.targetOrOffsetPosition);
                HandleMove(entities, command.targetOrOffsetPosition, command.add, false, useLowestCruiseSpeed: command.useLowestCruiseSpeed);
                break;
            case TactCommandTypes.AttackMove:
                Entity attackMoveTarget = command.targetEntityId != -1 ? EntityMgr.inst.entitiesDict[command.targetEntityId] : null;
                HandleAttackMove(entities, command.targetOrOffsetPosition, attackMoveTarget, command.add, false, useLowestCruiseSpeed: command.useLowestCruiseSpeed);
                break;
            case TactCommandTypes.Follow:
                Entity target = EntityMgr.inst.entitiesDict[command.targetEntityId];
                if(target != null) {
                    HandleFollow(entities, target, command.targetOrOffsetPosition, command.add, false);
                }
                break;
            case TactCommandTypes.Intercept:
                Entity interceptTarget = EntityMgr.inst.entitiesDict[command.targetEntityId];
                if(interceptTarget != null) {
                    HandleIntercept(entities, interceptTarget, command.add, false);
                }
                break;
            case TactCommandTypes.Intercept3d:
                Entity intercept3dTarget = EntityMgr.inst.entitiesDict[command.targetEntityId];
                if(intercept3dTarget != null) {
                    Handle3dIntercept(entities, intercept3dTarget, command.add, false);
                }
                break;
            default:
                //Debug.Log("Unknown Command: " + command.ToString());
                HandleMove(entities, command.targetOrOffsetPosition, command.add, false);
                break;
        }
        
    }

   
    //Networking -----------------------------------------------------------------
}


/*
 * 
    public float rClickRadiusSq = 10000;
    public Entity FindClosestEntInRadius(Vector3 point, float rsq)
    {
        Entity minEnt = null;
        float min = float.MaxValue;
        foreach (Entity ent in EntityMgr.inst.entities) {
            float distanceSq = (ent.transform.position - point).sqrMagnitude;
            if (distanceSq < rsq) {
                if (distanceSq < min) {
                    minEnt = ent;
                    min = distanceSq;
                }
            }    
        }
        return minEnt;
    }



/*
 * 
    public float rClickRadiusSq = 10000;
rsq        Entity minEntmin = float.MaxValue;
        foreach (Entity ent in EntityMgr.inst.entities) {
rsq) {if (distanceSq < min) minEntentmindistanceSq    minEnt*/