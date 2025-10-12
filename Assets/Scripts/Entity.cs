
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
    public bool isAI = false;


    [Header("Aspect references")]
    public NetAspect net = null;
    public OrientedPhysics phx = null;
    public UnitAI ai = null;
    public UIAspect ui = null;
    public WeaponsAspect weapons = null;
    private GameObject healthBarObject;
    public bool isGreyed = false; // Used for greyed out entities in the UI

    public float greyOverlayFadeDuration = 10f; // Duration for the grey overlay fade-out effect

    void Start()
    {
        InitializeEntityValues();
        SetEntityColors();
        SetupHealthBar();
        
    }

    void InitializeEntityValues()
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
    }
    public void SetEntityColors()
    {
        if (owner == null || owner == PlayerMgr.inst.neutral)
        {
            return; // Do not set any colors for neutral or undefined owner
        }
        Renderer[] childRenderers = GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in childRenderers)
        {
            if (renderer.CompareTag("Color"))
            {
                renderer.material.color = owner.playerColor;
            }
            else if (renderer.CompareTag("BrightGreenColor"))
            {
                renderer.material.color = new Color(0.2f, 1f, 0.2f, 1f);
            }
        }
    }

    void SetupHealthBar()
    {
        HealthBarMgr healthBarMgr = GetComponentInChildren<HealthBarMgr>(true);
        if (healthBarMgr != null)
        {
            healthBarObject = healthBarMgr.gameObject;
            healthBarObject.SetActive(false);
        }
    }
    private void FixedUpdate()
    {
        ComputeFuelRange();

        if (CheckAndHandleEntityDestruction())
            return;

        UpdateVisibility();
    }

    private bool CheckAndHandleEntityDestruction()
    {
        if (health <= 0)
        {
            WeaponsMgr.inst.DestroyEntity(this);
            return true;
        }

        if (fuel <= 0 && WeaponsMgr.inst.weapons.Contains(this))
        {
            FXMgr.inst.CreateExplosionAt(transform.position, 1);
            WeaponsMgr.inst.DestroyEntity(this);
            return true;
        }

        return false;
    }

    private void UpdateVisibility()
    {
        if (transform.childCount > 0)
            transform.GetChild(0).gameObject.SetActive(isVisible);

        if (healthBarObject != null)
            healthBarObject.SetActive(isVisible);
    }

    void ComputeFuelRange()
    {
        if (speed <= cruiseSpeed)
        {
            fuelBurnRate = 1f - (cruiseSpeed - speed) / (cruiseSpeed + 0.0001f);
        }
        else
        {
            fuelBurnRate = 1f + (speed - cruiseSpeed) / (maxSpeed - cruiseSpeed + 0.0001f);
        }

        fuel -= fuelBurnRate * Time.fixedDeltaTime * Time.timeScale;
        fuel = Mathf.Clamp(fuel, 0, maxFuel);
        range = Mathf.Clamp(fuel * cruiseSpeed, 0, maxRange);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, entityCollisionRadius);
    }
    
}

