using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AntiMissileWeaponDeployAspect : MonoBehaviour
{
    public UnitAI unitAI;
    float _range;
    public float range;
    public int ammo;
    public float cooldown;
    float cooldownTimer = 0;
    public bool isFreeToFire = false;
    [SerializeField] protected GenericMissile target = null;
    public virtual void SetTarget(GenericMissile newTarget) {
        if(target!=null && target!=newTarget) {
            target.OnDieEvent -=TargetDead;
        }
        if(newTarget!=null && target!=newTarget) {
            newTarget.OnDieEvent +=TargetDead;
            target=newTarget;
        }
    }

    public GenericMissile GetTarget() {
        return target;
    }

    public void TargetDead(GenericMissile target) {
        target=null;
    }
    // Start is called before the first frame update
    void Start()
    {
        _range = range*Utils.FromNauticalMiles;
        unitAI.antiWeaponDeployAspects.Add(this);
    }

    // Update is called once per frame
    void Update()
    {
        if(!isFreeToFire || ammo==0 || target==null || Vector3.Distance(target.position,unitAI.entity.position)>_range) {
            // Debug.Log("NoFIre!");
            return;
        }
        cooldownTimer-=Time.deltaTime;
        if(cooldownTimer<=0f && ammo>0) {
            FireWeapon();
            // Debug.Log("FIRE!");
            cooldownTimer=cooldown;
            ammo--;
        }
    }

    public virtual void FireWeapon() {
        AntiMissileMissile temp = WeaponsMgr.inst.LaunchAntiMissile(unitAI.entity.position,target,new(0,90,0),unitAI.entity.owner).GetComponent<AntiMissileMissile>();
        temp.velocity = Quaternion.Euler(0,90,0)*unitAI.entity.velocity;
        temp.phase = 0;
    }
}
