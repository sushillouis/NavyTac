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
    public Entity creatorsEntity;

    void Start()
    {
        isSelected = false;
        //cameraRig = transform.Find("CameraRig").gameObject;
        //selectionCircle = transform.Find("Decorations").Find("SelectionCylinder").gameObject;
    }
    void Update()
    {
    }

   void OnTriggerEnter(Collider other)
{
    Entity otherEntity = other.GetComponent<Entity>();
    if (otherEntity == null || WeaponMgr.inst.weapons.Contains(otherEntity) || otherEntity == this.creatorsEntity)
        return; 

    WeaponAspect weaponAspect = creatorsEntity.GetComponentInChildren<WeaponAspect>();
    if (weaponAspect == null || weaponAspect.allWeapons.Count == 0)
        return;

    Weapon weaponData = weaponAspect.allWeapons.Find(w => w.entityType == this.entityType);
    if (weaponData == null)
        return;

    float damage = WeaponMgr.inst.GetDamageForTarget(weaponData.weaponType, otherEntity.entityType);
    if (damage <= 0)
        return; 

    otherEntity.health = Mathf.Max(otherEntity.health - damage, 0);
    Debug.Log($"{otherEntity.name} took {damage} damage from {this.name}");

    if (otherEntity.health == 0)
    {
        Debug.Log($"{otherEntity.name} is destroyed.");
    }
}


}
