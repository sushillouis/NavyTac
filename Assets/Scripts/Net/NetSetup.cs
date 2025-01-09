using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetSetup : NetworkBehaviour
{
    public static NetSetup inst;

    private void Awake() {
        inst = this;
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        NetDebugConsole.inst.Log(OwnerClientId + " NetSetup Spawncall");
        //Players created here, one per client -- but players are named OnLoginButton below
        TactPlayer tmp = PlayerMgr.inst.CreateNetClientPlayer(OwnerClientId);
        PlayerMgr.inst.AddPlayer(tmp);
        if(OwnerClientId == NetworkManager.Singleton.LocalClientId)
            PlayerMgr.inst.localPlayer = tmp;
        NetDebugConsole.inst.Log("Local player: " + tmp.ToString());

    }

    // Update is only for debugging
    private void Update() {
        if(Input.GetKeyUp(KeyCode.C)) {
            NetDebugConsole.inst.Log(PlayerMgr.inst.StringAllPlayers());
        }
        if(Input.GetKeyUp(KeyCode.P)) {
            if(IsOwner)
                OnPlayerNamedServerRpc(OpenOceanMain.inst.playerName, NetworkManager.Singleton.LocalClientId);
        }
    }

    /// <summary>
    /// Starts off everything. Player sync. Entity creation with ownership.
    /// </summary>
    public void OnStartButton() {
        if(IsOwner) {
            OnPlayerNamedServerRpc(OpenOceanMain.inst.playerName, NetworkManager.Singleton.LocalClientId);
        }
    }

    //--------------------------------------------------------------------------------------
    // player setup and entity ownership setup
    [ServerRpc]
    public void OnPlayerNamedServerRpc(string playerName, ulong cid) {
        NetDebugConsole.inst.Log($"Owner: {OwnerClientId}, name: {playerName}, CID: {cid}");
        if(IsHost && IsServer) {
            RunStartupOnAllClientsClientRpc(playerName, cid);
        }
    }

    [ClientRpc]
    public void RunStartupOnAllClientsClientRpc(string playerName, ulong cid) {
        NetDebugConsole.inst.Log($"{OwnerClientId}: Starting up on Client with name: {playerName} and id: {cid}");
        PlayerMgr.inst.RenamePlayer(playerName, cid);
        if(IsOwner) {
            //NetDebugConsole.inst.Log("Owner client has players: ");
            //NetDebugConsole.inst.Log(PlayerMgr.inst.StringAllPlayers());
            //GameMgr.inst.MakeMapEntities();
            GameMgr.inst.OpenOcean1x1();
            TactNetMgr.inst.InitSyncList();
        }
            //StartCoroutine(WaitAndStartupOnClient()); //Awaiting proper countdown and game start

    }
    //--------------------------------------------------------------------------------------

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();
    }

    [SerializeField] private float waitTime = 2f;
    private IEnumerator WaitAndStartupOnClient() {
        yield return new WaitForSeconds(waitTime);
        //GameMgr.inst.MakeMapEntities();
        GameMgr.inst.OpenOcean1x1();
    }


}


