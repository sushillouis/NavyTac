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
    TugBoat,
    JARIUSV,
    SeaHunter,
    Mykola,
    SeaBaby,
    CVN75,
    Missile
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
    public float health = 100f;
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
        if (health <= 0)
        {
            OnEntityDeath();
        }

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
}
