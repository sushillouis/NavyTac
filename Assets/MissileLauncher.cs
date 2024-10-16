using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponAspect : MonoBehaviour
{



    // Weapon aspect will have all the necessary components to fire a missile
    // It will also have the necessary components to check for targets and fire at them
    // It will also have the necessary components to manage the ammo and cooldown of the weapon
    // It will also have the necessary components to manage the effectiveness of the weapon against different targets
    // it will also contain damage and other attributes of the weapon
    
    public Transform missileStartPoint;
    public float range;
    public float fireCooldown = 5.0f;
    public int ammoLimit = 1000;
    private Entity ownEntity;
    public int currentAmmo;
    private bool isCooldown = false;
    WeaponType weaponType;

    void Start()
    {
        ownEntity = GetComponent<Entity>();
        currentAmmo = ammoLimit;
    }

    void Update()
    {
        CheckAndFireAtTargets();
    }

    private void CheckAndFireAtTargets()
    {
        if (isCooldown || currentAmmo <= 0)
            return;

        Entity[] allEntities = FindObjectsOfType<Entity>();

        Entity bestTarget = null;
        float highestEffectiveness = 0.0f;

        foreach (Entity otherEntity in allEntities)
        {
            if (otherEntity.team == ownEntity.team || otherEntity == ownEntity || otherEntity.entityType == EntityType.Missile)
                continue;

            float distanceFromEnemy = Vector3.Distance(missileStartPoint.position, otherEntity.transform.position);
            if (distanceFromEnemy <= range)
            {
                float effectiveness = EffectivenessMatrixMgr.GetWeaponEffectiveness(weaponType, otherEntity.entityType);

                // Select the entity with the highest effectiveness
                if (effectiveness > highestEffectiveness)
                {
                    highestEffectiveness = effectiveness;
                    bestTarget = otherEntity;
                }
            }
        }

        // Fire at the best target
        if (bestTarget != null && highestEffectiveness > 0)
        {
            FireMissile(bestTarget);
            StartCoroutine(FireCooldown());
        }
        
    }
    
    private void FireMissile(Entity target)
    {
        if (currentAmmo <= 0)
        {
            Debug.Log("Out of ammo!");
            return;
        }
        Weapon missileWeapon = WeaponMgr.inst.CreateWeapon(weaponType, missileStartPoint.position, Vector3.zero, ownEntity.entityType);
        GameObject newMissile = missileWeapon.gameObject;
        newMissile.GetComponentInParent<Entity>().speed = missileWeapon.startSpeed;
        newMissile.name = "Missile_" + ownEntity.name + "_" + currentAmmo;
        Weapon missileEntity = newMissile.GetComponent<Weapon>();
        missileEntity.desiredHeading = ownEntity.desiredHeading;
        missileEntity.heading = ownEntity.heading;
        missileEntity.team = ownEntity.team;
        missileEntity.position = missileStartPoint.position;
        missileEntity.entityType = EntityType.Missile;
        DelayedIntercept(missileEntity, target);
        currentAmmo--;
    }

    private void DelayedIntercept(Weapon missileEntity, Entity target)
    {
        Intercept intercept = new Intercept(missileEntity, target);
        UnitAI missileAI = missileEntity.GetComponent<UnitAI>();
        missileAI.AddCommand(intercept);
    }
    IEnumerator FireCooldown()
    {
        isCooldown = true;
        yield return new WaitForSeconds(fireCooldown);
        isCooldown = false;
    }
}
