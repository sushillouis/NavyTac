using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponAspect : MonoBehaviour
{
    
    public float range;
    public float damage;
    public Entity ownEntity;
    
    public float ammo;
    void Start()
    {
        ammo = WeaponMgr.inst.weapons.Count;
        ownEntity = GetComponentInParent<Entity>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
            {
                CheckAndFireAtTargets();
            }
    }
    
    
    private void CheckAndFireAtTargets()
    {
        if (ammo <= 0)
            return;
        
        Entity[] allEntities = FindObjectsOfType<Entity>();
        foreach (Entity otherEntity in allEntities)
        {
            if (otherEntity.team == ownEntity.team || otherEntity == ownEntity)
                return;
            float distanceFromEnemy = Vector3.Distance(ownEntity.transform.position, otherEntity.transform.position);
            if (distanceFromEnemy <= range)
            {
                Debug.Log(otherEntity.name +" " + ownEntity.name);
                FireMissile(otherEntity);

            }
        }
        
        
    }
    private void FireMissile(Entity target){
        foreach(Entity weapon in WeaponMgr.inst.weapons)
        {
            
            Intercept intercept = new Intercept(weapon, target);
            UnitAI usv = weapon.GetComponent<UnitAI>();
            usv.AddCommand(intercept);
            ammo--;
        }
    }
    
}
