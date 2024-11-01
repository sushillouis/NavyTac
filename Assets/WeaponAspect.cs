
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
        foreach (Weapon weapon in allWeapons)
        {
            weapon.lastFireTime = -weapon.cooldown;
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    private Weapon GetWeaponByType(WeaponType type)
    {
        return allWeapons.Find(weapon => weapon.weaponType == type);
    }
    public void HandleFire_AA_Guided(Vector3 mousePos)
    {
        Weapon selectedWeapon = GetWeaponByType(WeaponType.AA_Guided);
        Debug.Log("Fire AA Guided");
    }
    public void HandleFire_LA_GuidedMissiles(Vector3 mousePos)
    {
        Weapon selectedWeapon = GetWeaponByType(WeaponType.LA_guided_missiles);
        Debug.Log("Fire LA Guided Missiles");
    }
    
   public void HandleFire_SmartSurfaceMissiles(Vector2 mousePos)
    {
        Weapon selectedWeapon = GetWeaponByType(WeaponType.Smart_surface_missiles_USV);
        if (!CanFireWeapon(selectedWeapon)) return;

        if (IsHitWithinLayerMask(mousePos, out Vector3 targetPosition))
        {
            Entity targetEntity = GetTargetEntity(targetPosition);
            if (targetEntity != null && selectedWeapon.ammoCount > 0)
            {
                CreateAndLaunchMissile(selectedWeapon, targetEntity, targetPosition);
                selectedWeapon.ammoCount--;
                selectedWeapon.lastFireTime = Time.time;
            }
        }
    }
    public void HandleFire_Gun(Vector2 mousePos)
        {
            Weapon SelectedWeapon = GetWeaponByType(WeaponType.Gun);
            Debug.Log("Fire LA Guided Missiles");
        }
    private bool CanFireWeapon(Weapon weapon)
    {
        return weapon != null && !IsWeaponOnCooldown(weapon) && IsWeaponSelected();
    }

    private bool IsWeaponOnCooldown(Weapon weapon){
        return Time.time - weapon.lastFireTime < weapon.cooldown;
    }
    private bool IsWeaponSelected()
    {
        return SelectionMgr.inst.selectedEntity == this.GetComponentInParent<Entity>();
    }

    private bool IsHitWithinLayerMask(Vector2 mousePos, out Vector3 targetPosition)
    {
        targetPosition = Vector3.zero;
        if (Physics.Raycast(Camera.main.ScreenPointToRay(mousePos), out hit, float.MaxValue, layerMask))
        {
            targetPosition = hit.point;
            targetPosition.y = 0;
            return true;
        }
        return false;
    }

    private Entity GetTargetEntity(Vector3 targetPosition)
    {
        if (!IsTargetInRange(targetPosition)) return null;
        return AIMgr.inst.FindClosestEntInRadius(targetPosition, rClickRadiusSq);
    }

    private bool IsTargetInRange(Vector3 targetPosition)
    {
        Weapon selectedWeapon = GetWeaponByType(WeaponType.Smart_surface_missiles_USV);
        return Vector3.Distance(selectedWeapon.source.position, targetPosition) <= selectedWeapon.range;
    }

    private void CreateAndLaunchMissile(Weapon weapon, Entity targetEntity, Vector3 targetPosition)
    {
        Entity newMissile = WeaponMgr.inst.CreateWeapon(
            weapon.entityType,
            weapon.source.position,
            weapon.source.rotation.eulerAngles,
            this.gameObject.transform.parent.parent.gameObject
        );

        if (newMissile != null)
        {
            UnitAI missileAI = newMissile.GetComponentInChildren<UnitAI>();
            Intercept intercept = new Intercept(newMissile, targetEntity);
            missileAI.AddCommand(intercept);
        }
    }
    
}
