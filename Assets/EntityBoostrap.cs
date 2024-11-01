using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityBoostrap : MonoBehaviour {
    /// <summary>
    /// Start is called on the frame when a script is enabled just before
    /// any of the Update methods is called the first time.
    /// </summary>
    [SerializeField] Player player;
    void Start()
    {
        Entity ent = this.gameObject.GetComponent<Entity>();
        ent.owner = player;
        EntityMgr.inst.entities.Add(ent);
        Destroy(this);
    }
}