using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
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
        // Ensure the other object has an Entity component
        Entity otherEntity = other.GetComponent<Entity>();
        if (otherEntity == null || WeaponMgr.inst == null || creatorsEntity == null) return;

        // Prevent self-collision or interacting with weapons already tracked by WeaponMgr
        if (WeaponMgr.inst.weapons.Contains(otherEntity) || otherEntity == creatorsEntity) return;

        // Check for weapon aspects on the creator's entity
        WeaponAspect weaponAspect = creatorsEntity.GetComponentInChildren<WeaponAspect>();
        if (weaponAspect == null || weaponAspect.allWeapons == null || weaponAspect.allWeapons.Count == 0) return;

        // Find weapon data matching this entity's type
        Weapon weaponData = weaponAspect.allWeapons.Find(w => w.entityType == entityType);
        if (weaponData == null) return;

        // Calculate damage to apply to the other entity
        float damage = WeaponMgr.inst.GetDamageForTarget(weaponData.weaponType, otherEntity.entityType);
        if (damage <= 0) return;

        // Apply damage and update health
        otherEntity.health = Mathf.Max(otherEntity.health - damage, 0);
        health = 0;

        // Handle destruction of this entity if health is depleted
        if (health <= 0) DestroyCurrentEntity();

        // Handle destruction of the other entity if its health is depleted
        if (otherEntity.health <= 0) DestroyOtherEntity(otherEntity);
    }

    void DestroyCurrentEntity()
    {
        // Remove from managers and destroy this game object
        EntityMgr.inst.entities.Remove(this);
        WeaponMgr.inst.weapons.Remove(this);
        Destroy(gameObject);
    }

    void DestroyOtherEntity(Entity otherEntity)
    {
        // Handle selection and toggle RTS view if necessary
        if (SelectionMgr.inst.selectedEntity == otherEntity && !CameraMgr.inst.isRTSMode)
            CameraMgr.inst.ToggleRTSView();

        // Stop all commands if the other entity has an AI component
        UnitAI otherEntityAI = otherEntity.GetComponentInChildren<UnitAI>();
        if (otherEntityAI != null)
            otherEntityAI.StopAndRemoveAllCommands();
        else
            Debug.LogWarning("OnTriggerEnter: otherEntity does not have a UnitAI component.");

        // Update selections and remove the entity
        SelectionMgr.inst.selectedEntities.Remove(otherEntity);
        if (SelectionMgr.inst.selectedEntity == otherEntity)
            SelectionMgr.inst.selectedEntity = null;

        EntityMgr.inst.entities.Remove(otherEntity);
        DistanceMgr.inst.Initialize();
        Destroy(otherEntity.gameObject);
    }




}
