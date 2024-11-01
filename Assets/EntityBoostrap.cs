using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This is Debug Code
// This is Debug Code
// This is Debug Code
public class EntityBoostrap : MonoBehaviour {
    [Header("This is a Debug Script")]
    [SerializeField] PlayerOwnerOfShip playerOwner;
    void Start()
    {
        Entity ent = this.gameObject.GetComponent<Entity>();
        switch (playerOwner) {
            case PlayerOwnerOfShip.Player1:
                ent.owner = PlayerMgr.inst.player1;
                break;
            case PlayerOwnerOfShip.Player2:
                ent.owner = PlayerMgr.inst.player2;
                break;
            case PlayerOwnerOfShip.Admin:
                ent.owner = PlayerMgr.inst.adminPlayer;
                break;
            case PlayerOwnerOfShip.Observer:
                ent.owner = PlayerMgr.inst.observer;
                break;
            default:
                ent.owner = PlayerMgr.inst.adminPlayer;
                break;
        }
        
        EntityMgr.inst.entities.Add(ent);
        this.enabled = false;
    }
}

enum PlayerOwnerOfShip {
    Admin,
    Observer,
    Player1,
    Player2,
}