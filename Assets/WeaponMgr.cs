using UnityEngine;

public class WeaponMgr : MonoBehaviour
{
    public static WeaponMgr inst;

    private void Awake()
    {
        inst = this;
    }

    // Fire a weapon (missile) from the assigned missile start point
    public void FireWeapon(Entity firingEntity, Entity targetEntity, Weapon weapon, Transform missileStartPoint)
    {
        if (missileStartPoint == null)
        {
            Debug.LogError("Missile start point is null for entity: " + firingEntity.gameObject.name);
            return;
        }

        // Check if the weapon can fire based on cooldown and ammo limits
        bool canFire = weapon.Fire(targetEntity);
        if (!canFire)
        {
            return;  // Weapon is either cooling down or out of ammo
        }

        // Instantiate the missile at the missile start point
        GameObject weaponObject = Instantiate(weapon.gameObject, missileStartPoint.position, missileStartPoint.rotation);

        // Set up the weapon
        Weapon newWeapon = weaponObject.GetComponent<Weapon>();
        newWeapon.maxSpeed = weapon.maxSpeed;
        newWeapon.range = weapon.range;
        newWeapon.weaponType = weapon.weaponType;
        newWeapon.damage = weapon.damage;

        // Assign the target and run intercept logic
        AssignInterceptCommand(newWeapon, targetEntity);
    }

    // Method to assign the intercept command
    private void AssignInterceptCommand(Weapon missile, Entity target)
    {
        // Create the intercept command for the missile
        Intercept intercept = new Intercept(missile, target);

        // Assign the intercept command to the missile's UnitAI
        UnitAI missileAI = missile.GetComponent<UnitAI>();
        missileAI.SetCommand(intercept);
    }
}
