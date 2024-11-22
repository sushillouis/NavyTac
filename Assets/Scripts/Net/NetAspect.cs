using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetAspect : MonoBehaviour
{
    void Start() {
        entity = GetComponentInParent<Entity>();
        entity.net = this;
    }

    public Entity entity;

    // Update is called once per frame
    void Update() {

    }
    public Vector3 posDiff = Vector3.zero;
    public float rotDiff = 0;

}
