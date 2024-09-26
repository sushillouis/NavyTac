using System.ComponentModel;
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
    TugBoat
}
public enum Team
{
    Team1,
    Team2
}

public class Entity : MonoBehaviour
{
    //------------------------------
    // values that change while running
    //------------------------------
    public bool isSelected = false;
    public Vector3 position = Vector3.zero;
    public Vector3 velocity = Vector3.zero;

    public float speed;
    public float desiredSpeed;
    public float heading; //degrees
    public float desiredHeading; //degrees
    //------------------------------
    // values that do not change
    //------------------------------
    public float acceleration;
    public float turnRate;
    public float maxSpeed;
    public float minSpeed;
    public float mass;
    public float health;
    public Transform missileStartPoint;
    public EntityType entityType;

    public Team team;
    public List<Weapon> weapons; 

    public GameObject cameraRig;
    public GameObject selectionCircle;

    // Start is called before the first frame update
    void Start()
    {
        isSelected = false;
        cameraRig = transform.Find("CameraRig").gameObject;
        selectionCircle = transform.Find("Decorations").Find("SelectionCylinder").gameObject;
    }

    // Update is called once per frame
    void Update()
    {
        FindAndEngageEnemy();  // Continuously scan for enemies to engage
    }

    // Method to find and engage enemies in range
    void FindAndEngageEnemy()
    {
        Entity[] allEntities = FindObjectsOfType<Entity>();

        foreach (Entity otherEntity in allEntities)
        {
            // Skip friendly entities
            if (otherEntity.team == this.team)
                continue;

            // Iterate through weapons
            if (weapons != null && weapons.Count > 0)
            {
                foreach (Weapon weapon in weapons)
                {
                    // Check if enemy is within range
                    float distanceFromEnemy = Vector3.Distance(transform.position, otherEntity.transform.position);
                    if (distanceFromEnemy < weapon.range)
                    {
                        // Fire the missile using WeaponMgr, if within range
                        if (weapon.weaponType == WeaponType.Missile)
                        {
                            WeaponMgr.inst.FireWeapon(this, otherEntity, weapon, missileStartPoint);
                        }
                    }
                }
            }
        }
    }
}
