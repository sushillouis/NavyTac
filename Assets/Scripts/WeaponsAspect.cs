using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class WeaponData
{
    public float cooldown;
    public WeaponBehaviors behaviorType;
    public EntityType weaponEntityType;
    public List<Entity> currentWeaponEntities; //list of alive weapons
    public Vector3 launchLocation;
    public Vector3 launchDirection;

    public float ammoCount;

}

public class WeaponsAspect : MonoBehaviour
{
    public Entity entity;
    public List<WeaponData> weapons = new List<WeaponData>();

    // Start is called before the first frame update
    void Start() {
        entity = GetComponentInParent<Entity>();
        entity.weapons = this;

        foreach(WeaponData wd in weapons) {
            wd.currentWeaponEntities = new List<Entity>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
