using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissileLauncher : MonoBehaviour
{
    public Transform missileStartPoint;
    public float range;
    public float fireCooldown = 5.0f;
    public int ammoLimit = 10;
    private Entity ownEntity;
    private int currentAmmo;
    private bool isCooldown = false;
    WeaponType weaponType;


    private static Dictionary<Entity, List<GameObject>> targetMissilesMap = new Dictionary<Entity, List<GameObject>>();

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

        foreach (Entity otherEntity in allEntities)
        {
            if (otherEntity.team == ownEntity.team || otherEntity == ownEntity || otherEntity.entityType == EntityType.Missile)
                continue;

            float distanceFromEnemy = Vector3.Distance(missileStartPoint.position, otherEntity.transform.position);
            if (distanceFromEnemy <= range)
            {
                FireMissile(otherEntity);
                StartCoroutine(FireCooldown());
                break;
            }
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
        if (missileEntity == null)
        {
            Debug.LogError("Entity component not found on the instantiated missile prefab.");
            return;
        }
        missileEntity.team = ownEntity.team;
        missileEntity.position = missileStartPoint.position;
        missileEntity.entityType = EntityType.Missile;
        if (!targetMissilesMap.ContainsKey(target))
        {
            targetMissilesMap[target] = new List<GameObject>();
        }
        targetMissilesMap[target].Add(newMissile);
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
