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
    private Dictionary<WeaponType, Dictionary<EntityType, float>> damageMatrix;
    private void InitializeDamageMatrix()
    {
        // Initialize the damage matrix
        damageMatrix = new Dictionary<WeaponType, Dictionary<EntityType, float>>();

        // Define damage for each weapon-target combination
        damageMatrix[WeaponType.AA_Guided] = new Dictionary<EntityType, float>
        {
            {EntityType.DDG51, 5f },
            {EntityType.Container,1f},
            {EntityType.CVN75, 2f},
            {EntityType.JARIUSV , 1f},
            {EntityType.MineSweeper, 2f},
            {EntityType.OilServiceVessel,0},
            {EntityType.OrientExplorer,0},
            {EntityType.PilotVessel,0},
            {EntityType.SeaBaby,0},
            {EntityType.SeaHunter,0},
            {EntityType.SmitHouston,0},
            {EntityType.Tanker,0},
            {EntityType.TugBoat,0},
            {EntityType.Mykola,0},
        };

        damageMatrix[WeaponType.Smart_surface_missiles_USV] = new Dictionary<EntityType, float>
        {
            {EntityType.DDG51, 5f },
            {EntityType.Container,99f},
            {EntityType.CVN75, 2f},
            {EntityType.JARIUSV , 1f},
            {EntityType.MineSweeper, 2f},
            {EntityType.OilServiceVessel,0},
            {EntityType.OrientExplorer,0},
            {EntityType.PilotVessel,0},
            {EntityType.SeaBaby,0},
            {EntityType.SeaHunter,0},
            {EntityType.SmitHouston,0},
            {EntityType.Tanker,0},
            {EntityType.TugBoat,0},
            {EntityType.Mykola,0},
        };

        damageMatrix[WeaponType.LA_guided_missiles] = new Dictionary<EntityType, float>
        {
            {EntityType.DDG51, 5f },
            {EntityType.Container,1f},
            {EntityType.CVN75, 2f},
            {EntityType.JARIUSV , 1f},
            {EntityType.MineSweeper, 2f},
            {EntityType.OilServiceVessel,0},
            {EntityType.OrientExplorer,0},
            {EntityType.PilotVessel,0},
            {EntityType.SeaBaby,0},
            {EntityType.SeaHunter,0},
            {EntityType.SmitHouston,0},
            {EntityType.Tanker,0},
            {EntityType.TugBoat,0},
            {EntityType.Mykola,0},
        };

        damageMatrix[WeaponType.Gun] = new Dictionary<EntityType, float>
        {
            {EntityType.DDG51, 5f },
            {EntityType.Container,1f},
            {EntityType.CVN75, 2f},
            {EntityType.JARIUSV , 1f},
            {EntityType.MineSweeper, 2f},
            {EntityType.OilServiceVessel,0},
            {EntityType.OrientExplorer,0},
            {EntityType.PilotVessel,0},
            {EntityType.SeaBaby,0},
            {EntityType.SeaHunter,0},
            {EntityType.SmitHouston,0},
            {EntityType.Tanker,0},
            {EntityType.TugBoat,0},
            {EntityType.Mykola,0},
        };

    }

    private void Awake()
    {
        inst = this;
        weapons = new List<Entity>();

    }

    public Entity CreateWeapon(EntityType weaponType, Vector3 position, Vector3 eulerAngles, GameObject parent)
    {
        Entity weapon = null;
        GameObject weaponPrefab = weaponPrefabs.Find(x => (x.GetComponent<Entity>().entityType == weaponType));
        if (weaponPrefab != null)
        {
            GameObject weaponGo = Instantiate(weaponPrefab, position, Quaternion.Euler(eulerAngles), entitiesRoot.transform);
            // weaponGo.transform.SetParent(parent.transform,true);
            // weaponGo.transform.SetParent(parent.transform,true);


            if (weaponGo != null)
            {
                Entity creator = parent.GetComponent<Entity>();
                weapon = weaponGo.GetComponent<Entity>();
                weaponGo.name = weaponType.ToString() + weaponId++;
                weapon.creatorsEntity = creator;
                weapon.owner = creator.owner;
                EntityMgr.inst.entities.Add(weapon);
                weapons.Add(weapon);
                DistanceMgr.inst.Initialize();
                weaponGo.GetComponent<Entity>().heading = eulerAngles.y;
                weaponGo.GetComponent<Entity>().desiredHeading = eulerAngles.y;
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