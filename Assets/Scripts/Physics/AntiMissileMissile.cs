using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AntiMissileMissile : MonoBehaviour, GenericMissile
{
    public float thrust = 200f;
    public float turnStrength = 90f;
    public float highSpeed = 1000f;
    public float highRotSpeed = 55f;
    public float cruseAltitude = 500f;
    public float terminalRadius;
    public GenericMissile target = null;
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

    public void Init(TactPlayer player) {
        // target.ai.OnDieEvent.Add(TargetDead);
        team = player;
        target.OnDieEvent +=TargetDead;
    }

    public void TargetDead(GenericMissile target) {
        //TODO Find New Target
        Explode();
    }

    public void Explode() {
        FXMgr.inst.CreateExplosionAt(transform.position);
        // target.ai.OnDieEvent.Remove(TargetDead);
        WeaponsMgr.inst.activeMissiles.Remove(this);
        if(target!=null) {
            target.OnDieEvent -=TargetDead;
        }
        if(this.OnDieEvent!=null) {this.OnDieEvent(this);}
        Destroy(gameObject);
    }

    

    

    void FixedUpdate()
    {
        if(target==null) {
            Explode();
        }
        // if(target==null) {
        //     return;
        // }
        if(Vector3.Distance(transform.position,target.position)<15f ) {
            Explode();
            target.Explode();
            return;
        }
        float throttle = 1f;
        Vector3 targetVector = Vector3.up;
                
        targetVector=target.position-transform.position;

        float velocityMag = _velocity.magnitude;

        if(velocityMag>0) {
            float timeOnTarget = targetVector.magnitude/_velocity.magnitude;
            if(timeOnTarget>.25f) {
                targetVector+=target.velocity*timeOnTarget;
            }
        }
        
        Vector3 targetAngleEuler = Quaternion.LookRotation(targetVector,Vector3.up).eulerAngles;
        float pitchThrottle = pitchPID.UpdateAngle(Time.fixedDeltaTime, transform.eulerAngles.x, targetAngleEuler.x);
        float yawThrottle = rollPID.UpdateAngle(Time.fixedDeltaTime, transform.eulerAngles.y, targetAngleEuler.y);
        float rollThrottle = yawPID.UpdateAngle(Time.fixedDeltaTime, transform.eulerAngles.z, targetAngleEuler.z);

        // print("Pitch: "+pitchThrottle);
        // print("Roll: "+rollThrottle);
        // print("Yaw: "+yawThrottle);
        // print("Target: "+targetAngleEuler);

        // if (throttle>.1f) {
        //     wake.gameObject.SetActive(true);
        //     wake.wakeLengthFactor=.13f*throttle;
        // } else {
        //     wake.gameObject.SetActive(false);
        // }
        _velocity+=throttle*thrust*Time.fixedDeltaTime*transform.forward;
        
        angularVelocity.x+=pitchThrottle*turnStrength*Time.fixedDeltaTime;
        angularVelocity.y+=yawThrottle*turnStrength*Time.fixedDeltaTime;
        angularVelocity.z+=rollThrottle*turnStrength*Time.fixedDeltaTime;

        float steepness = Vector3.Angle(transform.forward,Vector3.Scale(transform.forward,Utils.flat));

        if(steepness>90) {
            steepness-=90;
        }

        float tempY = Mathf.Abs(transform.eulerAngles.y);

        while(tempY>90) {
            tempY-=90;
        }

        float liftFactor = 1-((1-(steepness/90))*(1-(tempY/90))*LIFT_COFFEFICENT);
        

        _velocity+=Utils.GRAVITY*Time.fixedDeltaTime*Vector3.down*liftFactor;
        transform.position+=_velocity*Time.fixedDeltaTime;
        transform.Rotate(angularVelocity);
        float angluarDrag = Utils.ANGULAR_DRAG_COEFFICENT*(angularVelocity.magnitude/highRotSpeed);
        float velocityDrag = Utils.DRAG_COEFFICENT*(_velocity.magnitude/highSpeed);
        angluarDrag=Mathf.Clamp(angluarDrag,0f,1f);
        velocityDrag=Mathf.Clamp(velocityDrag,0f,1f);
        angularVelocity*=1-angluarDrag;
        _velocity*=1-velocityDrag;
    }
}
