
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
}

public class WeaponAspect : MonoBehaviour
{
    // Start is called before the first frame update
    public float rClickRadiusSq = 10000;
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
    public void HandleFire_AA_Guided(Vector3 mousePos){
        weaponIndex = 0;
        Debug.Log("Fire AA Guided");
    }
    public void HandleFire_LA_GuidedMissiles(Vector3 mousePos){
        weaponIndex = 1;
        Debug.Log("Fire LA Guided Missiles");
    }
    public void HandleFire_SmartSurfaceMissiles(Vector2 mousePos)
    {
        weaponIndex = 0;
        if (Physics.Raycast(Camera.main.ScreenPointToRay(mousePos), out hit ,float.MaxValue ,layerMask))
        {
            Vector3 pos = hit.point;
            pos.y = 0;
            Entity ent = AIMgr.inst.FindClosestEntInRadius(pos, rClickRadiusSq);
            Weapon selectedWeapon =   allWeapons[weaponIndex];
            
            if(SelectionMgr.inst.selectedEntity == this.GetComponentInParent<Entity>())
            {
                if(selectedWeapon.ammoCount > 0)
                {
                    Entity newMissile = WeaponMgr.inst.CreateWeapon(selectedWeapon.entityType,  selectedWeapon.source.position,selectedWeapon.source.rotation.eulerAngles);
                    UnitAI missileAI = newMissile.GetComponentInChildren<UnitAI>();
                    if (ent != null)
                    {
                        Intercept intercept = new Intercept(newMissile, ent);
                        missileAI.AddCommand(intercept);
                    }
                    selectedWeapon.ammoCount--;
                }
            }
        }
    }   
}
