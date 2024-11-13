using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;

[System.Serializable]
public enum EntityType
{
    DDG51,
    Container,
    MineSweeper,
    OilServiceVessel,
    OrientExplorer,
    PilotVessel,
    SmitHouston,
    Tanker,
    TugBoat,
    JARIUSV,
    SeaHunter,
    Mykola,
    SeaBaby,
    CVN75,
    Submarine,
    AntiShipMissile,
}


public class Entity : MonoBehaviour
{
    //------------------------------
    // values that change while running
    //------------------------------
    [Header("Dynamic values")]
    public bool isSelected = false;
    public Vector3 position = Vector3.zero;
    public Vector3 velocity = Vector3.zero;

    public float speed;
    public float desiredSpeed;
    public float heading; //degrees
    public float desiredHeading; //degrees
    public float health;
    public float fuel;

    [Header("Const values")]
    //------------------------------
    // values that do not change
    //------------------------------
    public float acceleration;
    public float turnRate;
    public float maxSpeed;
    public float minSpeed;
    public float mass;
    public float length;
    public float width;
    public float height;
    public EntityType entityType;
    public GameObject cameraRig;
    public GameObject selectionCircle;
    public Player owner;
    public Entity creatorsEntity;

    void Start()
    {
        isSelected = false;
        //cameraRig = transform.Find("CameraRig").gameObject;
        //selectionCircle = transform.Find("Decorations").Find("SelectionCylinder").gameObject;
    }
    void Update()
    {
    }
    // This is On Trigger Enter is just for testing purposes.
    // START
    void OnTriggerEnter(Collider other)
    {
        if (WeaponsMgr.inst.weapons.Contains(this))
            {
            Entity otherEntity = other.GetComponent<Entity>();
            if (otherEntity != creatorsEntity)
            {
                float damage = DamageMatrix.GetDamage(entityType, otherEntity.entityType);
                // Debug.Log(damage);
                otherEntity.health = Mathf.Max(otherEntity.health - damage, 0);
                health = 0;
                if (health <= 0) WeaponsMgr.inst.DestroyEntity(this);
                if (otherEntity.health <= 0) WeaponsMgr.inst.DestroyEntity(otherEntity);
            }
        }
    }

    // END
}
