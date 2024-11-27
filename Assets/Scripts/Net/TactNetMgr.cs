using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

[Serializable]
public struct NetSyncStruct : INetworkSerializable, IEquatable<NetSyncStruct>
{
    public int entityId;
    public Vector3 pos;
    public float heading;
    //public float dh, ds;

    public bool Equals(NetSyncStruct other) {
        return (entityId == other.entityId && pos == other.pos
            && Mathf.Abs(heading - other.heading) < Utils.EPSILON
            //&& Mathf.Abs(dh - other.dh) < Utils.EPSILON
            //&& Mathf.Abs(ds - other.ds) < Utils.EPSILON
            );
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        serializer.SerializeValue(ref entityId);
        serializer.SerializeValue(ref pos);
        serializer.SerializeValue(ref heading);
        //serializer.SerializeValue(ref dh);
        //serializer.SerializeValue(ref ds);
    }
}


public class TactNetMgr : NetworkBehaviour
{

    public static TactNetMgr inst;

    private NetworkList<NetSyncStruct> syncList;
    private NetworkObject myNetworkObject;

    [SerializeField] private TactPlayer ownPlayer = null;
    [SerializeField] private float heartbeatInterval = 0.2f;

    private void Awake() {
        inst = this;

        syncList = new NetworkList<NetSyncStruct>();
        myNetworkObject = GetComponent<NetworkObject>();

        myNetworkObject.CheckObjectVisibility += CheckObservability;
    }

    private bool CheckObservability(ulong clientId) {
        return true;
    }
    void Start() {
        NetDebugConsole.inst.Log("STARTed TactNetMgr with id: " + OwnerClientId);
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        syncList.OnListChanged += HandleSyncList;
        NetDebugConsole.inst.Log(OwnerClientId + ": SPAWNed TacNetMgr: isHost? " + IsHost + ", " + IsClient + ", " + IsServer + ", " + IsOwner);
    }

    //----Command update from any client is propagated to all clients----------------------
    [ServerRpc]
    public void CommandUpdateServerRpc(TactCommandStruct commandSpec) {
        //NetDebugConsole.inst.Log($"Owner: {OwnerClientId}, cmdSpec: {commandSpec.ToString()}");
        AIMgr.inst.HandleNetCommandSpec(commandSpec);
        UpdateClientsClientRpc(commandSpec);
    }

    [ClientRpc]
    public void UpdateClientsClientRpc(TactCommandStruct commandSpec) {
        AIMgr.inst.HandleNetCommandSpec(commandSpec);
    }
    //-------------------------------------------------------------------------------------
    // Three methods to handle creating and syncing heartbeat data between all players
    //-------------------------------------------------------------------------------------
    /// <summary>
    /// Runs repeatedly to send heartbeat sync data updates to all clients
    /// Invoked from InitSyncList
    /// </summary>
    void SendNetUpdates() {
        if(IsServer) {
            Entity entity;
            for(int i = 0; i < syncList.Count; i++) {
                NetSyncStruct nss = syncList[i];
                entity = entityDictionary[nss.entityId];
                if(entity != null) {
                    nss.pos.x = entity.position.x;
                    nss.pos.y = entity.position.y;
                    nss.pos.z = entity.position.z;
                    //nss.ds = entity.desiredSpeed;
                    //nss.dh = entity.desiredHeading;
                    nss.heading = entity.heading;
                    syncList[i] = nss;
                }
            }
        }
    }

    public void InitSyncList() {
        if(IsServer) {
            syncList.Clear();
            foreach(Entity ent in EntityMgr.inst.entities) {
                NetSyncStruct nss = AddEntity(ent);
            }
            InvokeRepeating("SendNetUpdates", 0, heartbeatInterval); //SendNetUpdates is above this ^^
        }
    }

    Dictionary<int, Entity> entityDictionary = new Dictionary<int, Entity>();
    NetSyncStruct AddEntity(Entity ent) {
        NetSyncStruct nss = new NetSyncStruct
        {
            entityId = ent.entityId,
            pos = new Vector3(ent.position.x, ent.position.y, ent.position.z),
            heading = ent.heading,
            //dh = ent.desiredHeading,
            //ds = ent.desiredSpeed,
        };
        syncList.Add(nss);
        entityDictionary.Add(ent.entityId, ent);
        return nss;
    }
    //-------------------------------------------------------------------------------------

    //End Server only------------------------------------------------------------------------

    private void Update() {
        if(Input.GetKeyUp(KeyCode.T)) {
            if(IsServer)
                InitSyncList();
        }
        if(Input.GetKeyUp(KeyCode.U)) {
            if(IsOwner) {
                PrintEntityOwners();
                //SendClientUpdatesToServer();
            }

        }
        if(Input.GetKeyUp(KeyCode.V)) {
            if(IsServer) NetDebugConsole.inst.Log(OwnerClientId + " am Server");
            if(IsHost)   NetDebugConsole.inst.Log(OwnerClientId + " am HOST");
            if(IsClient) NetDebugConsole.inst.Log(OwnerClientId + " am Client");
            if(IsOwner)  NetDebugConsole.inst.Log(OwnerClientId + " am OWNER");
        }

    }

    public void PrintEntityOwners() {
        NetDebugConsole.inst.Log("Start Entities-------");
        foreach(Entity ent in EntityMgr.inst.entities) {
            NetDebugConsole.inst.Log($"Ent:{ent.name}, owner: {ent.owner.ToString()}");
        }
        NetDebugConsole.inst.Log("End   Entities-------");
    }

      public void HandleSyncList(NetworkListEvent<NetSyncStruct> changeEvent) {
        if(IsClient && !IsServer) {
            ClientHandleSyncList(changeEvent);
        }
    }


    //Client sync------------------------------------------
    void ClientHandleSyncList(NetworkListEvent<NetSyncStruct> changeEvent) {
        switch(changeEvent.Type) {
            case NetworkListEvent<NetSyncStruct>.EventType.Add:
                break;
            case NetworkListEvent<NetSyncStruct>.EventType.Value:
                HandleChangesFromServer(changeEvent.Value);
                break;
            default:
                NetDebugConsole.inst.Log(OwnerClientId + ": Client: Default: " + changeEvent.Value.entityId);
                break;
        }
    }

    void HandleChangesFromServer(NetSyncStruct changedValue) {

        Entity ent;
        if(!entityDictionary.ContainsKey(changedValue.entityId)) {
            ent = EntityMgr.inst.entities.Find(x => x.entityId == changedValue.entityId);
            entityDictionary.Add(ent.entityId, ent);
        }
        ent = entityDictionary[changedValue.entityId];

        if(ent != null) { //Tell net aspect to handle net update
            ent.net.NetUpdate(changedValue);
        }
    }
    //end Client sync------------------------------------------

    public override void OnNetworkDespawn() {
        TactNetShutdown();
        base.OnNetworkDespawn();
    }

    public void TactNetShutdown() {
        syncList = null;
    }

}


/*
 void SendClientUpdatesToServer() {
     if(IsOwner) {
         foreach(Entity entity in EntityMgr.inst.entities) {
             NetDebugConsole.inst.Log(OwnerClientId +  $": Ent: {entity.owner.playerId}");
             if(entity.owner.playerId == OwnerClientId) {
                 UpdateOwnersDSDHClientRpc(entity.entityId, entity.desiredSpeed, entity.desiredHeading);
             }
         }
     }
 }

 [ClientRpc]
 void UpdateOwnersDSDHClientRpc(int entityId, float ds, float dh) {
     NetDebugConsole.inst.Log($"{OwnerClientId}: updating {entityId} + ({ds}, {dh}) ");
     Entity ent = entityDictionary[entityId];
     if(ent != null) {
         ent.desiredSpeed = ds;
         ent.desiredHeading = dh;
     }

 }

 //Begin Server only------------------------------------------------------------------------
 public string ConnectedClientsToString() {
     StringBuilder stringBuilder = new StringBuilder();
     stringBuilder.Append("Connected Clients:\n");
     foreach(ulong key in NetworkManager.Singleton.ConnectedClients.Keys) {
         stringBuilder.Append(OwnerClientId + ": clientID: " + key + ", val: ");
         stringBuilder.Append(NetworkManager.Singleton.ConnectedClients[key]);
         stringBuilder.Append('\n');
     }
     return stringBuilder.ToString();
 }
 */