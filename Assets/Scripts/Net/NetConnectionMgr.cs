using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetConnectionMgr : MonoBehaviour
{
   public static NetConnectionMgr inst;
    private void Awake() {
        inst = this;
    }

    public enum ConnectionState
    {
        Connected = 0,
        Disconnected = 1,
    }

    public event Action<ulong, ConnectionState> OnClientConnectionNotification;

    private void Start() {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectedCallback;
    }

    public void OnClientConnectedCallback(ulong cid) {
        NetDebugConsole.inst.Log("Connected to: " + cid);
    }
    public void OnClientDisconnectedCallback(ulong cid) {
        NetDebugConsole.inst.Log("OnClientDisconnectedCallback: " + cid);
        TactNetMgr.inst.TactNetShutdown();
        NetworkManager.Singleton.Shutdown();

    }



}
