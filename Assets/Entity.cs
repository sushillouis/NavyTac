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
    CVN75,
    Submarine,
    AntiShipMissile,
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

    // void OnTriggerEnter(Collider other)
    // {
    //     Entity otherEntity = other.GetComponent<Entity>();
    //     if (otherEntity == null || WeaponMgr.inst == null || creatorsEntity == null) return;
    //     if (WeaponMgr.inst.weapons.Contains(otherEntity) || otherEntity == creatorsEntity) return;
    //     WeaponAspect weaponAspect = creatorsEntity.GetComponentInChildren<WeaponAspect>();
    //     if (weaponAspect == null || weaponAspect.allWeapons == null || weaponAspect.allWeapons.Count == 0) return;
    //     Weapon weaponData = weaponAspect.allWeapons.Find(w => w.entityType == entityType);
    //     if (weaponData == null) return;
    //     float damage = WeaponMgr.inst.GetDamageForTarget(weaponData.weaponType, otherEntity.entityType);
    //     if (damage <= 0) return;
    //     otherEntity.health = Mathf.Max(otherEntity.health - damage, 0);
    //     health = 0;
    //     if (health <= 0) DestroyEntity(this);
    //     if (otherEntity.health <= 0) DestroyEntity(otherEntity);
    // }

    // void DestroyEntity(Entity entity)
    // {   
    //     if (!CameraMgr.inst.isRTSMode && CameraMgr.inst.YawNode.transform.parent.parent.name == entity.name )
    //         CameraMgr.inst.ToggleRTSView();
    //     UnitAI otherEntityAI = entity.GetComponentInChildren<UnitAI>();
    //     if (otherEntityAI != null)
    //         otherEntityAI.StopAndRemoveAllCommands();
    //     if (SelectionMgr.inst.selectedEntities.Contains(entity))
    //     {
    //         SelectionMgr.inst.selectedEntities.Remove(entity);
    //         if (SelectionMgr.inst.selectedEntities.Count > 0){
    //             Debug.Log(SelectionMgr.inst.selectedEntities[0]);
    //             SelectionMgr.inst.selectedEntity = SelectionMgr.inst.selectedEntities[0];}
    //         else    
    //             SelectionMgr.inst.selectedEntity = null;
    //     }
    //     EntityMgr.inst.entities.Remove(entity);
    //     if (WeaponMgr.inst.weapons.Contains(entity))
    //         WeaponMgr.inst.weapons.Remove(entity);
    //     DistanceMgr.inst.Initialize();
    //     Destroy(entity.gameObject);
    // }
}
