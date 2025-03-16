using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.WSA;


[Serializable]
public class WeaponData
{
    public Transform launchPoint; 
    public float cooldown;
    public WeaponBehaviors behaviorType;
    public EntityType weaponEntityType;
    public float range = 200f;
    public List<Entity> currentWeaponEntities; //list of alive weapons
    public float defaultDamage;
    // public Vector3 launchLocation;
    // public Vector3 launchDirection;
    public float ammoCount;
    [NonSerialized]
    public float lastShotTime;
}
public class WeaponsAspect : MonoBehaviour
{
    public Entity entity;
    // public List<WeaponData> weapons = new List<WeaponData>();
    public WeaponData weapon;

    private void Awake() {
        
        entity = GetComponentInParent<Entity>();
    
        entity.weapons = this;
        weapon.currentWeaponEntities = new List<Entity>();
        weapon.lastShotTime = -weapon.cooldown;
    }
}
