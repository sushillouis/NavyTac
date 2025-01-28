using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BasicMissile : MonoBehaviour, GenericMissile
{
    public float thrust = 200f;
    public float turnStrength = 90f;
    public float highSpeed = 1000f;
    public float highRotSpeed = 55f;
    public float cruseAltitude = 500f;
    public float terminalRadius;
    public Entity target = null;
    [SerializeField] protected PIDController pitchPID;
    [SerializeField] protected PIDController rollPID;
    [SerializeField] protected PIDController yawPID;
    protected Vector3 _velocity = Vector3.zero;
    public Vector3 velocity {get { return _velocity; } set { _velocity = value; } }
    public Vector3 position {get { return transform.position; }}
    [SerializeField] protected Vector3 angularVelocity = Vector3.zero;
    public int phase = 0;
    public const float LIFT_COFFEFICENT = 0.85f;
    [SerializeField] protected TactPlayer _team;
    public TactPlayer team {get {return _team;} set { _team = value;}}
    [SerializeField] protected List<GenericMissile.MissileDieEventMethod> _OnDieEvent = new();
    public event GenericMissile.MissileDieEventMethod OnDieEvent;

    public virtual void Init(TactPlayer player) {
        target.ai.OnDieEvent +=TargetDead;
        team = player;
    }

    public virtual void TargetDead(Entity target) {
        //TODO Find New Target
        Explode();
    }

    public virtual void Explode() {
        FXMgr.inst.CreateExplosionAt(transform.position);
        if(target!=null) {
            target.ai.OnDieEvent -=TargetDead;
        }
        WeaponsMgr.inst.activeMissiles.Remove(this);
        if(this.OnDieEvent!=null) {this.OnDieEvent(this);}
        Destroy(gameObject);
    }
}
