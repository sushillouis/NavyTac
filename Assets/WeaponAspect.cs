
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;
public enum WeaponType
{
    AA_Guided,
    LA_guided_missiles,
    Smart_surface_missiles_USV,
    Gun
}

[Serializable]
public class Weapon
{
    public Transform source;
    public WeaponType weaponType;
    public EntityType entityType;
    public int cooldown;
    public int ammoCount;
    public float range;
    public GameObject weaponPrefab;
    public float damage;

    [NonSerialized]
    public float lastFireTime;
}

public class WeaponAspect : MonoBehaviour
{
    // Start is called before the first frame update
    public float rClickRadiusSq = 5000;
    public int layerMask;
    public RaycastHit hit;
    public List<Weapon> allWeapons;
    public static WeaponAspect inst;
    private void Awake()
    {
        inst = this;
    }

    void Start()
    {
        layerMask = 1 << 9;
    }

    // Update is called once per frame
    void Update()
    {

    }
    private int weaponIndex;
    public void HandleFire_AA_Guided(Vector3 mousePos)
    {
        weaponIndex = 0;
        Debug.Log("Fire AA Guided");
    }
    public void HandleFire_LA_GuidedMissiles(Vector3 mousePos)
    {
        weaponIndex = 1;
        Debug.Log("Fire LA Guided Missiles");
    }
    public void HandleFire_SmartSurfaceMissiles(Vector2 mousePos)
    {
        weaponIndex = 0;
        Weapon selectedWeapon = allWeapons[weaponIndex];

        // Check if the weapon is ready to fire (cooldown complete)
        if (Time.time - selectedWeapon.lastFireTime < selectedWeapon.cooldown)
        {
            Debug.Log("Weapon on cooldown");
            return;
        }

        // Perform a raycast to find target position
        if (Physics.Raycast(Camera.main.ScreenPointToRay(mousePos), out hit, float.MaxValue, layerMask))
        {
            Vector3 targetPosition = hit.point;
            targetPosition.y = 0;

            // Check if target is within range
            if (Vector3.Distance(selectedWeapon.source.position, targetPosition) > selectedWeapon.range)
            {
                Debug.Log("Target out of range");
                return;
            }

            // Find the closest entity within the click radius
            Entity targetEntity = AIMgr.inst.FindClosestEntInRadius(targetPosition, rClickRadiusSq);

            // Verify that we have ammo and a valid target
            if (SelectionMgr.inst.selectedEntity == this.GetComponentInParent<Entity>() &&
                WeaponMgr.inst != null && selectedWeapon.ammoCount > 0 && targetEntity != null)
            {
                // Fire the weapon
                Entity newMissile = WeaponMgr.inst.CreateWeapon(
                    selectedWeapon.entityType,
                    selectedWeapon.source.position,
                    selectedWeapon.source.rotation.eulerAngles,
                    this.gameObject
                );

                // Assign missile AI and target
                UnitAI missileAI = newMissile.GetComponentInChildren<UnitAI>();
                Intercept intercept = new Intercept(newMissile, targetEntity);
                missileAI.AddCommand(intercept);

                // Decrease ammo count and set last fire time for cooldown
                selectedWeapon.ammoCount--;
                selectedWeapon.lastFireTime = Time.time;
            }
        }
    }


    public void HandleFire_Gun(Vector2 mousePos)
    {
        weaponIndex = 3;
        Debug.Log("Fire LA Guided Missiles");
    }
}
