
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;


public class Entity : MonoBehaviour
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
    public float fuel = 1;
    public float range;
    public float fuelBurnRate;
    public EntityRole entityRole;

    [Header("Const values")]
    //------------------------------
    // values that do not change
    //------------------------------
    public float acceleration;
    public float originalAcceleration; // Used to reset acceleration after boost
    public float turnRate;
    public float originalTurnRate; // Used to reset turn rate after boost
    public float maxSpeed;
    public float originalMaxSpeed; // Used to reset max speed after boost
    public float minSpeed;
    public float cruiseSpeed;
    public float mass;
    public float length;
    public float width;
    public float height;
    public float maxHealth;
    public float minHealth;
    public bool isVisible = true;
    public float maxFuel;
    public float maxRange;
    public float entityCollisionRadius = 1.0f; // Added entity collision radius

    public EntityType entityType;
    public EntityClass entityClass;

    public GameObject cameraRig;
    public GameObject selectionCircle;
    public TactPlayer owner;
    public Entity creatorsEntity;


    [Header("Aspect references")]
    public NetAspect net = null;
    public OrientedPhysics phx = null;
    public UnitAI ai = null;
    public UIAspect ui = null;
    public WeaponsAspect weapons = null;
    private GameObject healthBarObject;

    void Start()
    {
        entityCollisionRadius = length;
        fuel = maxFuel;
        health = maxHealth;
        if (entityType != EntityType.AntiShipMissile && entityType != EntityType.Rig_Balder)
        {
            acceleration = maxSpeed * maxSpeed / 100f;
            turnRate = (180f / Mathf.PI) * (maxSpeed / acceleration);

        }
        isSelected = false;
        Renderer mainRenderer = GetComponent<Renderer>();
        Renderer[] childRenderers = GetComponentsInChildren<Renderer>(true);

        
        for (int i = 0; i < childRenderers.Length; i++)
        {
            if(childRenderers[i].tag =="Color"){
                childRenderers[i].material.color = owner.playerColor;
            }
            
        }
        HealthBarMgr healthBarMgr = GetComponentInChildren<HealthBarMgr>(true);
        if(healthBarMgr != null)
        {
            healthBarObject = healthBarMgr.gameObject;
            healthBarObject.SetActive(false);  // Start hidden
        }
    }
    private void FixedUpdate() {
        ComputeFuelRange();
        if(health <= 0 ){
            WeaponsMgr.inst.DestroyEntity(this);          
        }
        if(fuel <= 0){
            if(WeaponsMgr.inst.weapons.Contains(this))
            {
                FXMgr.inst.CreateExplosionAt(transform.position, 1);
                WeaponsMgr.inst.DestroyEntity(this);
            }
        }
        
        transform.GetChild(0).gameObject.SetActive(isVisible);
        if(healthBarObject!= null)
        {
            healthBarObject.SetActive(isVisible);
        }
        
        
        
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

    // Draw Gizmo for entity collision radius
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, entityCollisionRadius);
    }
    
}

