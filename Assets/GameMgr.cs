﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameMgr : MonoBehaviour
{
    public static GameMgr inst;
    private GameInputs input;
    private void Awake()
    {
        inst = this;
        input = new GameInputs();
        input.Enable();
    }

    // Start is called before the first frame update
    void Start()
    {
        Vector3 position = Vector3.zero;
        foreach (GameObject go in EntityMgr.inst.entityPrefabs)
        {
            Entity ent = EntityMgr.inst.CreateEntity(go.GetComponent<Entity>().entityType, position, Vector3.zero);
            ent.isSelected = false;
            ent.team = Team.Team1;
            position.x += 200;
        }
        position = Vector3.zero;
        position.z = 5000;
        foreach (GameObject go in EntityMgr.inst.entityPrefabs)
        {
            Entity ent = EntityMgr.inst.CreateEntity(go.GetComponent<Entity>().entityType, position, Vector3.zero);
            ent.isSelected = false;
            ent.team = Team.Team2;
            ent.desiredHeading = 180;
            ent.heading = 180;
            position.x += 200;
        }
    }

    public Vector3 position;
    public float spread = 20;
    public float colNum = 10;
    public float initZ;
    // Update is called once per frame
    void Update()
    {
        if (input.Entities.Create100.triggered) {
            initZ = position.z;
            for (int i = 0; i < 10; i++) {
                for (int j = 0; j < 10; j++) {
                    Entity ent = EntityMgr.inst.CreateEntity(EntityType.PilotVessel, position, Vector3.zero);
                    position.z += spread;
                }
                position.x += spread;
                position.z = initZ;
            }
            DistanceMgr.inst.Initialize();
        }
    }
}
