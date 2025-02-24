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
    public float maxHealth;
    public float minHealth;

    public float maxFuel;
    public float maxRange;

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
    

    void Start()
    {
        isSelected = false;
        //cameraRig = transform.Find("CameraRig").gameObject;
        //selectionCircle = transform.Find("Decorations").Find("SelectionCylinder").gameObject;
        fuel = maxFuel;
        Renderer mainRenderer = GetComponent<Renderer>();
   
    
    // Optionally, update all child renderers if necessary
        Renderer[] childRenderers = GetComponentsInChildren<Renderer>();
        
        for (int i = 0; i < childRenderers.Length; i++)
        {
            if(childRenderers[i].tag =="Color"){
                childRenderers[i].material.color = owner.playerColor;
            }
            
        }
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
    //             DamageMatrix dm = new DamageMatrix();
    //             float damage = dm.GetDamage(this.entityType, otherEntity.entityType);
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
            // Debug.Log(otherEntity.gameObject.layer);
            // Check if otherEntity is not null and not the creator, and has a different owner
            if (otherEntity != null && otherEntity != creatorsEntity && otherEntity.owner != owner)
            {   
                float damage = WeaponsMgr.inst.damageMatrix.GetDamage(this.entityType, otherEntity.entityType);
                FXMgr.inst.CreateExplosionAt(hit.point, 1);
                otherEntity.health = Mathf.Max(otherEntity.health - damage, 0);
                health = 0;
                if (health <= 0) WeaponsMgr.inst.DestroyEntity(this);
            }
        }
    }
}
void OnCollisionEnter(Collision collision)
{
    // Check if the collider belongs to a TerrainCollider
    if (collision.collider is TerrainCollider)
    {
        Debug.Log("Hitting Terrain");
    }
}


    // END
}
