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
    public Team team;
    public float speed;
    public float fuel = 100f;
    public float desiredSpeed;
    public float heading; //degrees
    public float desiredHeading; //degrees
    //------------------------------
    // values that do not change
    //------------------------------
    public float acceleration;
    private Vector3 lastPosition;
    public float turnRate;
    public float maxSpeed;
    public float minSpeed;
    public float mass;

    public EntityType entityType;

    public GameObject cameraRig;
    public GameObject selectionCircle;

    // Start is called before the first frame update
    void Start()
    {
        isSelected = false;
        cameraRig = transform.Find("CameraRig").gameObject;
        selectionCircle = transform.Find("Decorations").Find("SelectionCylinder").gameObject;
        lastPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        FuelConsumption();
        lastPosition = transform.position;
    }

    
    // fuel consumption is added on the basis of distance traveled
    private void FuelConsumption(){
        float distanceTraveled = Vector3.Distance(lastPosition, transform.position);
        fuel -= (distanceTraveled / 100f);
        fuel = Mathf.Max(fuel, 0);
        if (fuel <= 0)
        {
            isSelected = false;
            speed = 0;
            this.GetComponent<UnitAI>().StopAndRemoveAllCommands();
        }
    }
}
