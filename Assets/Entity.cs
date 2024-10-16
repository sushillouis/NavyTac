using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

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
    Missile,
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
    public float health = 100f;
    public float fuel = 100f;
    public Transform missileStartPoint;
    public EntityType entityType;

    public Team team;
    public List<Weapon> weapons;

    public GameObject cameraRig;
    public GameObject selectionCircle;

    // Track previous position to calculate distance traveled
    private Vector3 previousPosition;

    // Start is called before the first frame update
    void Start()
    {
        isSelected = false;
        cameraRig = transform.Find("CameraRig").gameObject;
        selectionCircle = transform.Find("Decorations").Find("SelectionCylinder").gameObject;

        // Initialize the previous position
        previousPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (health <= 0)
        {
            OnEntityDeath();
        }

        FuelConsumption();  // Call fuel consumption based on distance

        // Update the previous position after moving
        previousPosition = transform.position;
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        if (health <= 0)
        {
            OnEntityDeath();
        }
    }

    private void OnEntityDeath()
    {
        EntityMgr.inst.RemoveEntity(this);
    }

    private void FuelConsumption()
    {

        float distanceTraveled = Vector3.Distance(previousPosition, transform.position);
        fuel -= (distanceTraveled / 100f);
        fuel = Mathf.Max(fuel, 0);
        if (fuel <= 0)
        {
            maxSpeed = 0;
            minSpeed = 0;
            desiredSpeed = 0;
            acceleration = 0;
            turnRate = 0;
        }
    }
}
