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
    public float range;
    public float fuelBurnRate;

    [Header("Const values")]
    //------------------------------
    // values that do not change
    //------------------------------
    public float acceleration;
    public float turnRate;
    public float maxSpeed;
    public float minSpeed;
    public float cruiseSpeed;
    public float mass;
    public float length;
    public float width;
    public float height;

    public float maxFuel;
    public float maxRange;

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
        fuel = maxFuel;

    }
    void Update()
    {
        if(health <= 0){
            EntityMgr.inst.DestroyEntity(this);           
        }
    }

    private void FixedUpdate() {
        ComputeFuelRange();
        RaycastCollisonCheck();
    }

    void ComputeFuelRange() {
        if(speed <= cruiseSpeed) {
            fuelBurnRate = 1f - (cruiseSpeed - speed) / (cruiseSpeed + 0.0001f);
        } else {
            fuelBurnRate = 1f + (speed - cruiseSpeed) / (maxSpeed - cruiseSpeed + 0.0001f) ;
        }
        fuel -= fuelBurnRate * Time.fixedDeltaTime * Time.timeScale;
        fuel = Mathf.Clamp(fuel, 0, maxFuel);
        range = Mathf.Clamp(fuel * cruiseSpeed, 0, maxRange);

    }
    // This function On Trigger Enter is just for testing purposes.
    // START
    // void OnTriggerEnter(Collider other)
    // {

    //     //this method checks if the weapon is collided by a ship and if yes then it damages the ship based on the damage matrix
    //     Debug.Log("hit");
    //     if (WeaponsMgr.inst.weapons.Contains(this))
    //         {
    //         Entity otherEntity = other.GetComponent<Entity>();
    //         if (otherEntity != creatorsEntity)
    //         {
    //             float damage = DamageMatrix.GetDamage(this.entityType, otherEntity.entityType);
    //             // Debug.Log(damage);
    //             otherEntity.health = Mathf.Max(otherEntity.health - damage, 0);
    //             health = 0;
    //             if (health <= 0) WeaponsMgr.inst.DestroyEntity(this);
    //         }
    //     }
    // }
    // This function  use raycast to check if the weapon is collided by a ship and if yes then it damages the ship based on the damage matrix
    void RaycastCollisonCheck()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, 20))
        {
            if (WeaponsMgr.inst.weapons.Contains(this))
            {
                Entity otherEntity = hit.collider.GetComponent<Entity>();
                if (otherEntity != creatorsEntity)
                {
                    float damage = DamageMatrix.GetDamage(this.entityType, otherEntity.entityType);
                    // Debug.Log(damage);
                    otherEntity.health = Mathf.Max(otherEntity.health - damage, 0);
                    health = 0;
                    if (health <= 0) WeaponsMgr.inst.DestroyEntity(this);
                }
            }
        }
    }

    // END
}
