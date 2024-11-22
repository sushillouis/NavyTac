using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;
using UnityEngine.InputSystem;
using System.Text;

public struct NetSyncStruct : INetworkSerializable, IEquatable<NetSyncStruct>
{
    public int entityId;
    public Vector3 pos;
    public float heading;
    public float dh, ds;

    public bool Equals(NetSyncStruct other) {
        return (entityId == other.entityId && pos == other.pos
            && Mathf.Abs(heading - other.heading) < Utils.EPSILON
            && Mathf.Abs(dh - other.dh) < Utils.EPSILON
            && Mathf.Abs(ds - other.ds) < Utils.EPSILON);
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        serializer.SerializeValue(ref entityId);
        serializer.SerializeValue(ref pos);
        serializer.SerializeValue(ref heading);
        serializer.SerializeValue(ref dh);
        serializer.SerializeValue(ref ds);
    }
}


public class TactNetMgr : NetworkBehaviour
{


    private NetworkList<NetSyncStruct> syncList;
    private NetworkObject myNetworkObject;
    private void Awake() {
        syncList = new NetworkList<NetSyncStruct>();
        myNetworkObject = GetComponent<NetworkObject>();

        myNetworkObject.CheckObjectVisibility += CheckObservability;

    }

    private bool CheckObservability(ulong clientId) {
        return true;
    }
    void Start() {
        //Debug.Log("Started TactNetMgr with id: " + OwnerClientId);
        NetDebugConsole.inst.Log("Started TactNetMgr with id: " + OwnerClientId);
        if(IsServer) {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetDebugConsole.inst.Log(ConnectedClientsToString());
        }
        PlayerMgr.inst.AddNetClientPlayer(OwnerClientId);
    }

    string ConnectedClientsToString() {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append("Connected Clients:\n");
        foreach(ulong key in NetworkManager.Singleton.ConnectedClients.Keys) {
            stringBuilder.Append(OwnerClientId + ": clientID: " + key + ", val: ");
            stringBuilder.Append(NetworkManager.Singleton.ConnectedClients[key]);
            stringBuilder.Append('\n');
        }
        return stringBuilder.ToString();
    }

    private void OnClientConnected(ulong clientID) {
        NetDebugConsole.inst.Log(OwnerClientId + ": Client connected, Id: " + clientID);
        NetDebugConsole.inst.Log(ConnectedClientsToString());

    }

    private void Update() {
        if(Input.GetKeyUp(KeyCode.T)) {
            if(IsServer)
                InitSyncList();
        }
        if(Input.GetKeyUp(KeyCode.U)) {
            if(IsServer)
                SendNetUpdates();
        }
        if(Input.GetKeyUp(KeyCode.V)) {
            if(IsServer) {
                NetDebugConsole.inst.Log(OwnerClientId + " am Server");
            }
            if(IsHost) {
                NetDebugConsole.inst.Log(OwnerClientId + " am HOST");
            }
            if(IsClient) {
                NetDebugConsole.inst.Log(OwnerClientId + " am Client");
            }

        }
    }

    float interval = 0.5f;
    public float passedTime = 0;
    bool NetUpdateInterval() {
        passedTime += Time.deltaTime * Time.timeScale;
        if(passedTime > interval) {
            passedTime = 0;
            return true;
        } else {
            return false;
        }
    }

    /// <summary>
    /// Only called if isServer true
    /// </summary>
    void SendNetUpdates() {
        if(IsServer) {
            Entity entity;
            //NetSyncStruct syncStruct;
            for(int i = 0; i < syncList.Count; i++) {
                NetSyncStruct nss = syncList[i];
                entity = entityDictionary[nss.entityId];
                if(entity != null) {

                    nss.pos.x = entity.position.x;
                    nss.pos.y = entity.position.y;
                    nss.pos.z = entity.position.z;
                    nss.ds = entity.desiredSpeed;
                    nss.dh = entity.desiredHeading;
                    nss.heading = entity.heading;
                    syncList[i] = nss;
                }
            }
        }
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        syncList.OnListChanged += HandleSyncList;
        NetDebugConsole.inst.Log(OwnerClientId + ": Spawned: isHost? " + IsHost + ", " + IsClient + ", " + IsServer);

        if(IsServer)
            InitSyncList();
    }

    public void HandleSyncList(NetworkListEvent<NetSyncStruct> changeEvent) {
        if(IsClient && !IsServer) {
            ClientHandleSyncList(changeEvent);
        }

    }
    void ServerHandleSyncList(NetworkListEvent<NetSyncStruct> changeEvent) {
        //NetDebugConsole.inst.Log(OwnerClientId + ": ServerHandleSyncList: " + changeEvent.Type);
    }

    void ClientHandleSyncList(NetworkListEvent<NetSyncStruct> changeEvent) {
        switch(changeEvent.Type) {
            case NetworkListEvent<NetSyncStruct>.EventType.Add:
                //NetDebugConsole.inst.Log(OwnerClientId + ": Client: Added: " + changeEvent.Value.entityId
                //    + " pos: " + changeEvent.Value.pos);
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
            //NetDebugConsole.inst.Log(OwnerClientId + ": Client: Added to dictionary: " + ent.entityId);
        }
        ent = entityDictionary[changedValue.entityId];

        if(ent != null) {
            ent.net.posDiff = changedValue.pos - ent.position;
            ent.net.rotDiff = changedValue.heading - ent.heading;

            ent.position.x = changedValue.pos.x;
            ent.position.y = changedValue.pos.y;
            ent.position.z = changedValue.pos.z;
            ent.heading = changedValue.heading;


            ent.desiredHeading = changedValue.dh;
            ent.desiredSpeed = changedValue.ds;
        }
    }

    void InitSyncList() {
        syncList.Clear();
        foreach(Entity ent in EntityMgr.inst.entities) {
            NetSyncStruct nss = AddEntity(ent);
        }
        InvokeRepeating("SendNetUpdates", 0, 0.2f);
    }

    Dictionary<int, Entity> entityDictionary = new Dictionary<int, Entity>();
    NetSyncStruct AddEntity(Entity ent) {
        NetSyncStruct nss = new NetSyncStruct
        {
            entityId = ent.entityId,
            pos = new Vector3(ent.position.x, ent.position.y, ent.position.z),
            heading = ent.heading,
            dh = ent.desiredHeading,
            ds = ent.desiredSpeed,
        };
        syncList.Add(nss);
        entityDictionary.Add(ent.entityId, ent);
        return nss;
    }

    public override void OnNetworkDespawn() {
        syncList.Clear();
        syncList.OnListChanged -= HandleSyncList;
        syncList.Dispose();
        base.OnNetworkDespawn();
    }



}
