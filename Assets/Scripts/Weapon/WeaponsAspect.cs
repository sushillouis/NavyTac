using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


[Serializable]
public class WeaponData
{
    public float cooldown;
    public WeaponBehaviors behaviorType;
    public EntityType weaponEntityType;
    public float range = 200f;
    public List<Entity> currentWeaponEntities; //list of alive weapons
    public Vector3 launchLocation;
    public Vector3 launchDirection;
    public float ammoCount;
    [NonSerialized]
    public float lastShotTime;
}
public class WeaponsAspect : MonoBehaviour
{
    public Entity entity;
    public List<WeaponData> weapons = new List<WeaponData>();

    private void Awake() {
        entity = GetComponentInParent<Entity>();
        entity.weapons = this;

        foreach(WeaponData wd in weapons) {
            wd.currentWeaponEntities = new List<Entity>();
            wd.lastShotTime = -wd.cooldown;
        }

    }
}
