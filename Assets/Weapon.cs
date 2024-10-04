using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[System.Serializable]
public enum WeaponType
{
    Missile
}

public class Weapon : Entity
{


    public float startSpeed;
    public float damage;
    public WeaponType weaponType;
    void Start()
    {
        speed = startSpeed;
        desiredSpeed = startSpeed;
    }

    void Update()
    {

    }
    private void OnTriggerEnter(Collider other)
    {

        Entity targetEntity = other.GetComponent<Entity>();

        if (targetEntity != null && targetEntity.team != this.team && targetEntity.entityType != this.entityType)
        {
            targetEntity.TakeDamage(damage);
            WeaponMgr.inst.RemoveWeapon(this);
        }
    }
}
