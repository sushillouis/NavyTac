using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum WeaponType
{
    Missile
}

public class Weapon : MonoBehaviour
{
    public Vector3 position = Vector3.zero;
    public Vector3 velocity = Vector3.zero;

    public float speed;
    public float desiredSpeed;
    public float coolDown = 5.0f;  // Cooldown time in seconds
    public float ammoLimits = 10;  // Number of times this weapon can be fired
    public float damage;
    public float heading;
    public float desiredHeading;

    public float acceleration;
    public float turnRate;
    public float maxSpeed;
    public float minSpeed;
    public float mass;
    public WeaponType weaponType;
    public float range;  // The range of the weapon

    public Entity targetEntity;  // The target entity for the missile
    private bool isFired = false;
    private float lastFireTime = -999f;

    void Start()
    {
        // Initial setup for the weapon
        speed = 0;
        desiredSpeed = maxSpeed;
    }

    void Update()
    {
        // Cooldown logic handled in WeaponMgr
    }

    // Fire the missile at the target
    public bool Fire(Entity target)
    {
        if (Time.time - lastFireTime < coolDown)
        {
            Debug.Log("Weapon is still cooling down.");
            return false; // Cooldown not yet complete
        }

        if (ammoLimits <= 0)
        {
            Debug.Log("No more ammo available.");
            return false; // No ammo left
        }

        targetEntity = target;
        isFired = true;
        lastFireTime = Time.time;
        ammoLimits--;  // Reduce ammo after firing

        Debug.Log("Weapon fired at target: " + targetEntity.name);
        return true;  // Indicating weapon has successfully fired
    }
}
