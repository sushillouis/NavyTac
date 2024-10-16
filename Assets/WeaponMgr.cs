using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponMgr : MonoBehaviour
{
    public static WeaponMgr inst;
    public GameObject weaponsRoot;
    public GameObject blastPrefab;
    public List<GameObject> weaponPrefabs;
    public List<Weapon> weapons;
    public static int weaponId = 0;
    private void Awake()
    {
        inst = this;
        weapons = new List<Weapon>();
       
    }

    public Weapon CreateWeapon(WeaponType weaponType, Vector3 position, Vector3 eulerAngles, EntityType entityType)
    {
        Weapon weapon = null;
        GameObject weaponPrefab = weaponPrefabs.Find(x => (x.GetComponent<Weapon>().weaponType == weaponType));
        if (weaponPrefab != null)
        {
            GameObject weaponGo = Instantiate(weaponPrefab, position, Quaternion.Euler(eulerAngles), weaponsRoot.transform);
            if (weaponGo != null)
            {
                weapon = weaponGo.GetComponent<Weapon>();
                weaponGo.name = entityType.ToString() + weaponType.ToString() + weaponId++;
                weapons.Add(weapon);
            }
        }
        else
        {
            Debug.LogError("Weapon prefab not found for type: " + weaponType);
        }
        return weapon;
    }
    public void RemoveWeapon(Weapon weapon)
    {
        if (weapons.Contains(weapon))
        {
            GameObject blastInstance = Instantiate(blastPrefab, weapon.transform.position, weapon.transform.rotation);
            UnitAI cmd = weapon.GetComponent<UnitAI>();
            if (cmd != null)
            {
                cmd.StopAndRemoveAllCommands();
            }
            weapons.Remove(weapon);
            Destroy(weapon.gameObject);
            StartCoroutine(DestroyBlastAfterDelay(blastInstance, .05f));
        }
    }

    private IEnumerator DestroyBlastAfterDelay(GameObject blast, float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(blast);
    }

    void Start()
    {
    }

    void Update()
    {
    }
}
