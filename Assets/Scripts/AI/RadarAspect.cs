using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RadarAspect : MonoBehaviour
{
    public Entity entity;
    public bool isEnabled;
    public float scanrate;
    public float range;
    float _scanratetimer;
    public List<GenericMissile> inboundMissiles = new();
    [SerializeField] int targetIterator = 0;
    // Start is called before the first frame update
    void Start()
    {
        _scanratetimer = scanrate;
        entity = GetComponentInParent<Entity>();
    }

    // Update is called once per frame
    void Update()
    {
        if(!isEnabled) {
            return;
        }
        _scanratetimer-=Time.deltaTime;
        if(_scanratetimer<=0f) {
            
            _scanratetimer=scanrate;
            RadarSearch();
        }
    }

    public void RadarSearch() {
        foreach (GenericMissile missile in inboundMissiles) {
            missile.OnDieEvent +=TargetDead;
        }
        inboundMissiles.Clear();
        foreach (GenericMissile missile in WeaponsMgr.inst.activeMissiles) {
            if(missile.team==entity.owner && Vector3.Distance(entity.position,missile.position)<range) {
                inboundMissiles.Add(missile);
                missile.OnDieEvent -=TargetDead;
            }
        }
        inboundMissiles.Sort(new MissileProximityComparer(entity.position));
    }

    
    public void TargetDead(GenericMissile missile) {
        inboundMissiles.Remove(missile);
    }

    // public List<GenericMissile> GetMissiles() {
    //     List<GenericMissile> tempMissiles = new();
    //     foreach (GenericMissile missile in inboundMissiles) {
    //         if(missile!=null) {
    //             tempMissiles.Add(missile);
    //         } else {
    //             print("Missile Destroyed!");
    //         }
    //     }
    //     return tempMissiles;
    // }

    public GenericMissile GetClosestMissile(int index = 0) {
        GenericMissile tempMissile = null;
        if(inboundMissiles.Count>index) {
            tempMissile = inboundMissiles[index];
        }
        return tempMissile;
    }

    public GenericMissile GetClosestMissileIterate() {
        GenericMissile tempMissile = null;

        if(inboundMissiles.Count>targetIterator) {
            tempMissile = inboundMissiles[targetIterator];
            targetIterator++;
        } else {
            targetIterator=0;
        }
        return tempMissile;
    }

    
}

public class MissileProximityComparer: IComparer<GenericMissile> {

    Vector3 location;
    public MissileProximityComparer(Vector3 location) {
        this.location = location;
        
    }   

    public int Compare(GenericMissile left, GenericMissile right)
    {
        if(left != null && right != null) {
            float mag1 = Vector3.Distance(location, left.position);
            float mag2 = Vector3.Distance(location, right.position);
            return mag1 > mag2 ? -1 : 1;
        }

        if(right == null && left ==null) {
            return 0;
        }
        if(left!=null) {
            return -1;
        }
        return 1;
    }
}