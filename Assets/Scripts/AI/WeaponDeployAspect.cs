using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WeaponDeployAspect : MonoBehaviour
{
    public UnitAI unitAI;
    float _range;
    public float range;
    public int ammo;
    public float cooldown;
    float cooldownTimer = 0;
    public bool isFreeToFire = false;
    [SerializeField] protected Entity target = null;
    public virtual void SetTarget(Entity newTarget) {
        if(target!=null && target!=newTarget) {
            target.ai.dieEvents.Remove(TargetDead);
        }
        target=null;
        if(newTarget!=null && target!=newTarget  && newTarget.entityClass != EntityClass.Airplane) {
            newTarget.ai.dieEvents.Add(TargetDead);
            target=newTarget;
        }
    }

    public Entity GetTarget() {
        return target;
    }

    public void TargetDead(Entity target) {
        target=null;
    }
    // Start is called before the first frame update
    void Start()
    {
        _range = range*Utils.FromNauticalMiles;
        unitAI.weaponDeployAspects.Add(this);
    }

    // Update is called once per frame
    void Update()
    {
        if(!isFreeToFire || ammo==0 || target==null || Vector3.Distance(target.position,unitAI.entity.position)>_range) {
            // Debug.Log("NoFIre!");
            return;
        }
        cooldownTimer-=Time.deltaTime;
        if(cooldownTimer<=0f) {
            FireWeapon();
            // Debug.Log("FIRE!");
            cooldownTimer=cooldown;
            ammo--;
        }
    }

    public virtual void FireWeapon() {
        JudeMissile temp = WeaponsMgr.inst.LaunchCruseMissile(unitAI.entity.position,target,new(0,unitAI.entity.heading,0)).GetComponent<JudeMissile>();
        temp.velocity = Quaternion.Euler(0,90,0)*unitAI.entity.velocity;
        temp.phase = 1;
    }
}
