using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponMgr : MonoBehaviour
{
    public static WeaponMgr inst;
    public GameObject entitiesRoot;
    public List<GameObject> weaponPrefabs;
    public List<Entity> weapons;

    public static int weaponId = 0;
    private void Awake()
    {
        inst = this;
        weapons = new List<Entity>();
       
    }

    public Entity CreateWeapon(EntityType weaponType, Vector3 position, Vector3 eulerAngles)
    {
        Entity weapon = null;
        GameObject weaponPrefab = weaponPrefabs.Find(x => (x.GetComponent<Entity>().entityType == weaponType));
        if (weaponPrefab != null)
        {
            GameObject weaponGo = Instantiate(weaponPrefab, position, Quaternion.Euler(eulerAngles), entitiesRoot.transform);
            if (weaponGo != null)
            {
                weapon = weaponGo.GetComponent<Entity>();
                weaponGo.name = weaponType.ToString() + weaponId++;
                weapons.Add(weapon);
                weaponGo.GetComponent<Entity>().heading = eulerAngles.y;
                weaponGo.GetComponent<Entity>().desiredHeading= eulerAngles.y;
            }
        }
        return weapon;
    }
    void Start()
    {
    }

    void Update()
    {
    }
}