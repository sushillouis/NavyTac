using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using Unity.Netcode;
using UnityEngine;
[Serializable]
public struct TactCommandStruct: INetworkSerializable, IEquatable<TactCommandStruct>
{
    public TactCommandTypes commandType;
    public int[] entityIds;
    public int targetEntityId;
    public Vector3 targetOrOffsetPosition;
    public bool add;

    public bool Equals(TactCommandStruct other) {
        return (commandType == other.commandType
            && IntArrayEqual(entityIds, other.entityIds)
            && targetEntityId == other.targetEntityId
            && targetOrOffsetPosition == other.targetOrOffsetPosition
            && add == other.add);
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
    }

    public override string ToString() {
        StringBuilder sb = new StringBuilder();
        sb.Append("[");
        for(int i = 0; i < entityIds.Length; i++) {
            sb.Append(entityIds[i].ToString() + ", ");
        }
        sb.Append("]");
        return $"Eid: {sb.ToString()}, Cmd: {commandType}, TGT: {targetEntityId}, Pos: {targetOrOffsetPosition}, Add?: {add}";
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
    public List<Entity> selectedEntities = new List<Entity>();


    // Update is called once per frame
    void Update()
    {
        
    }

    //I need to be entity owner to command entities.
    //If I select a number of entities, I will only command the entities I own
    // Does not yet handle AI players
    // In AIMgr.cs
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
            if (ent != null && !ent.isVisible && ent.entityClass == EntityClass.Missile) ent = null;

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

                ReplayCommand cmd = new ReplayCommand
                {
                    timestamp = Time.time,
                    commandType = commandType,
                    entityIds = selectedEntities.Select(e => e.entityId).ToArray(),
                    targetPosition = pos,
                    targetEntityId = targetEntityId,
                    add = add
                };
                ReplayMgr.inst.RecordCommand(cmd);
            }

            if (ent == null)
            {
                if (attackMove)
                    HandleAttackMove(SelectionMgr.inst.selectedEntities, pos, null, add);
                else
                    HandleMove(SelectionMgr.inst.selectedEntities, pos, add);
            }
            else
            {
                if (attackMove)
                    HandleAttackMove(SelectionMgr.inst.selectedEntities, pos, ent, add);
                else
                    HandleMove(SelectionMgr.inst.selectedEntities, pos, add);
            }
        }
    }
}

    // public void HandleMove(List<Entity> entities, Vector3 point, bool add, 
    //                   bool isLocalCommand = true, bool maxSpeedMovement = false , bool useFormation = false, FormationType formationType = FormationType.Circle)
    // Constructor for position-based attack-move
public void HandleAttackMove(List<Entity> entities, Vector3 point, Entity target, bool add = false, bool isLocalCommand = true, bool maxSpeedMovement = false, bool acquireTarget = false)
{
    if (isLocalCommand)
    {
        NetTellAllClients(TactCommandTypes.Move, entities, point, null, add);
    }

    foreach (Entity entity in entities)
    {
        float doneDistanceSq;

        if (target != null)
        {
            // Use weapon range squared if available
            WeaponsAspect weaponsAspect = entity.GetComponentInChildren<WeaponsAspect>();
            doneDistanceSq = (weaponsAspect != null && weaponsAspect.weapon != null) 
                ? weaponsAspect.weapon.range * weaponsAspect.weapon.range 
                : 100000f; // Fallback
        }
        else
        {
            // Use Move's logic based on entity count
            if (entities.Count == 1)
            {
                doneDistanceSq = 500f;
            }
            else if (entities.Count < 5)
            {
                doneDistanceSq = 500f * 500f;
            }
            else if (entities.Count >= 10)
            {
                WeaponsAspect weaponsAspect = entity.GetComponentInChildren<WeaponsAspect>();
                doneDistanceSq = (weaponsAspect != null && weaponsAspect.weapon != null) 
                    ? weaponsAspect.weapon.range * weaponsAspect.weapon.range 
                    : 100000f;
            }
            else
            {
                doneDistanceSq = StoppingDistanceSq(entity.entityType);
            }
        }

        AttackMove am = target != null 
            ? new AttackMove(entity, target,acquireTargetsOnWay:acquireTarget, maxSpeedMovement, doneDistanceSq) 
            : new AttackMove(entity, point, maxSpeedMovement, doneDistanceSq);

        UnitAI uai = entity.GetComponentInChildren<UnitAI>();
        AddOrSet(am, uai, add);
    }
}
    public void HandleMove(List<Entity> entities, Vector3 point,
                      bool add = false, bool isLocalCommand = true, bool maxSpeedMovement = false, float doneDistanceSq = 100000)
    {
        if (isLocalCommand)
        {
            NetTellAllClients(TactCommandTypes.Move, entities, point, null, add);
        }
        foreach (Entity entity in entities)
        {
            // Original log, kept inside the loop. If intended once per call, move outside.
            //Debug.Log("EntityCount: " + entities.Count); 

            float currentDoneDistanceSq;

            if (entities.Count == 1)
            {
                currentDoneDistanceSq = 500f;
            }
            else if (entities.Count < 5) // Covers 2, 3, 4 entities
            {
                currentDoneDistanceSq = 500f * 500f;
            }
            else if (entities.Count >= 10)
            {
                //Debug.Log("Entities: " + entities.Count); // Specific log for this case
                WeaponsAspect weaponsAspect = entity.GetComponentInChildren<WeaponsAspect>();
                if (weaponsAspect != null && weaponsAspect.weapon != null)
                {
                    currentDoneDistanceSq = weaponsAspect.weapon.range * weaponsAspect.weapon.range;
                }
                else
                {
                    // Fallback if WeaponsAspect or weapon is null
                    currentDoneDistanceSq = doneDistanceSq; 
                    //Debug.LogWarning($"Entity {entity.entityId} missing WeaponsAspect or weapon. Using fallback doneDistanceSq: {currentDoneDistanceSq}");
                }
            }
            else // Covers 5, 6, 7, 8, 9 entities
            {
                currentDoneDistanceSq = StoppingDistanceSq(entity.entityType);
            }
            
            Move m = new Move(entity, point, maxSpeedMovement, currentDoneDistanceSq);
            UnitAI uai = entity.GetComponentInChildren<UnitAI>();
            if (uai != null)
            {
                AddOrSet(m, uai, add);
            }
            else
            {
                //Debug.LogWarning($"Entity {entity.entityId} does not have a UnitAI component. Command not issued.");
            }
        }
    }

    public float StoppingDistanceSq(EntityType entityType)
    {
        // Using float literals for consistency
        if (entityType == EntityType.DDG51)
        {
            return 800f * 800f;
        }
        else if (entityType == EntityType.SeaHunter)
        {
            return 500f * 500f;
        }
        else if (entityType == EntityType.JARIUSV)
        {
            return 400f * 400f;
        }
        else
        {
            return 100f * 100f;
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
    void NetTellAllClients(TactCommandTypes cmdType, List<Entity> entities, Vector3 pos, Entity target, bool add) {
        if(!OpenOceanMain.inst.isSinglePlayer) {
            TactCommandStruct netCommand = MakeNetCommandStruct(cmdType, entities, pos, target, add);
            OpenOceanMain.inst.localTactNetMgr.CommandUpdateServerRpc(netCommand);
        }
    }
    

    TactCommandStruct MakeNetCommandStruct(TactCommandTypes cmdType, List<Entity> entities, Vector3 pos, Entity target, bool add) {
        TactCommandStruct netCommand = new TactCommandStruct();

        netCommand.commandType = cmdType;
        netCommand.add = add;
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
                HandleMove(entities, command.targetOrOffsetPosition, command.add, false);
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