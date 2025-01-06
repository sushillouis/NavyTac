using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Entity : MonoBehaviour, IComparable<Entity>
{
    public int entityId;
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
    public EntityRole entityRole;

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
    public EntityClass entityClass;

    public GameObject cameraRig;
    public GameObject selectionCircle;

    public TactPlayer owner;


    [Header("Aspect references")]
    public NetAspect net = null;
    public OrientedPhysics phx = null;
    public UnitAI ai = null;
    public UIAspect ui = null;
    public WeaponsAspect weapons = null;

    // Start is called before the first frame update
    void Start()
    {
        isSelected = false;
        //cameraRig = transform.Find("CameraRig").gameObject;
        //selectionCircle = transform.Find("Decorations").Find("SelectionCylinder").gameObject;
        fuel = maxFuel;

    }

    // Update is called once per frame
    void Update()
    {

    }

    private void FixedUpdate() {
        ComputeFuelRange();
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

    public int CompareTo(Entity other)
    {   
        if(other == null) return -1;


        if(other.entityType == this.entityType) {
            return 0;
        }

        return Utils.costDict[this.entityType] > Utils.costDict[other.entityType] ? -1 : 1;
    }

    
}

public class EntityStrengthCompararer: IComparer<Entity> {

    public int Compare(Entity left, Entity right)
    {
        if(left != null && right != null) {
            return Utils.strengthDict[left.entityType] > Utils.strengthDict[right.entityType] ? -1 : 1;
        }

        if(right == null && left ==null) {
            return 0;
        }
        if(left!=null) {
            return -1;
        }
        return 1;
    }
}
