using System.Collections;
using System.Collections.Generic;
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
    CVN75
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

    // Start is called before the first frame update
    void Start()
    {
        isSelected = false;
        //cameraRig = transform.Find("CameraRig").gameObject;
        //selectionCircle = transform.Find("Decorations").Find("SelectionCylinder").gameObject;
    }

    // Update is called once per frame
    void Update()
    {
    }

    void OnTriggerEnter(Collider other)
    {
        if (other != this.GetComponentInParent<Collider>())
        {
            // Check if the collider belongs to a weapon
            Entity weaponEntity = other.GetComponent<Entity>();
            if (weaponEntity != null && WeaponMgr.inst.weapons.Contains(weaponEntity))
            {
                WeaponAspect weaponAspect = other.GetComponent<WeaponAspect>();
                if (weaponAspect != null)
                {
                    // Retrieve the weapon's damage value
                    Weapon weaponData = weaponAspect.allWeapons.Find(w => w.entityType == weaponEntity.entityType);
                    if (weaponData != null)
                    {
                        // Apply damage to this entity
                        health -= weaponData.damage;

                        // Log the damage application
                        Debug.Log($"{gameObject.name} took {weaponData.damage} damage from {weaponEntity.name}");

                        // Check if the entity is destroyed
                        if (health <= 0)
                        {
                            Debug.Log("DEAD");
                        }
                    }
                }
            }
        }
    }
}
