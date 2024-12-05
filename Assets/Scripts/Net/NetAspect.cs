using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public class NetAspect : MonoBehaviour
{
    private void Awake() {
        entity = GetComponentInParent<Entity>();
        entity.net = this;
    }
    void Start() {
        oldNetUpdateTime = Time.realtimeSinceStartup;
    }

    public Entity entity;
    public NetSyncStruct sync;

    public Vector3 posDiff = Vector3.zero;
    public Vector3 posDelta = Vector3.zero;
    public float rotDiff = 0;
    public float rotDelta = 0;

    // Update is called once per frame
    [SerializeField] private float oldNetUpdateTime = 0;
    [SerializeField] private float diffNetUpdateTime = 0;


    [SerializeField] private float diffUpdateTime = 0;
    void FixedUpdate() {
        diffUpdateTime = Time.fixedDeltaTime;
        entity.position += posDelta;
        entity.heading += rotDelta;
    }

    public float nSteps;
    public void NetUpdate(NetSyncStruct syncData) {
        sync = syncData;
        diffNetUpdateTime = Time.realtimeSinceStartup - oldNetUpdateTime;
        oldNetUpdateTime = Time.realtimeSinceStartup;

        posDiff = syncData.pos - entity.position;
        rotDiff = Utils.AngleDiffPosNeg(syncData.heading, entity.heading);

        nSteps = diffNetUpdateTime / diffUpdateTime;
        posDelta = posDiff/nSteps;
        rotDelta = rotDiff/nSteps;

        //entity.position = syncData.pos;
        //entity.heading = syncData.heading;


        //entity.desiredHeading = syncData.dh;
        //entity.desiredSpeed = syncData.ds;



    }
}
