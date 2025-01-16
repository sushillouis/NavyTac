using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class JudeOriented3dPID : OrientedPhysics
{
    public float thrust =100f;
    public float turnStrength = 90f;
    public float highSpeed = 100f;
    public float highRotSpeed = 55f;
    public float cruseAltitude = 500f;
    [SerializeField] PIDController pitchPID;
    [SerializeField] PIDController rollPID;
    [SerializeField] PIDController yawPID;
    [SerializeField] Vector3 angularVelocity = Vector3.zero;
    [SerializeField] int phase = 0;
    [SerializeField] Transform parentTransform;
    public const float LIFT_COFFEFICENT = 0.8f;

    /// <summary>
    /// Start is called on the frame when a script is enabled just before
    /// any of the Update methods is called the first time.
    /// </summary>
    void Start()
    {
        entity = GetComponentInParent<PlaneEntity>();
        parentTransform = entity.transform;
    }
    
    void FixedUpdate()
    {
        float throttle = 1f;
        Vector3 targetVector = Vector3.up;

        if(entity.desiredSpeed==0f) {
            entity.desiredHeading+=90f*Time.fixedDeltaTime;
        }
        
        // targetVector = new Vector3(Mathf.Cos(entity.desiredHeading*Mathf.Deg2Rad),0,Mathf.Sin(entity.desiredHeading*Mathf.Deg2Rad));
        targetVector = Quaternion.AngleAxis(entity.desiredHeading,Vector3.up)*Vector3.forward;
        if(phase==0) {
            targetVector=targetVector.normalized+(Vector3.up*.33f);
            if(parentTransform.position.y>=cruseAltitude-1f) {
                phase=1;
            }
        } else if(phase==1) {
            if(parentTransform.position.y<cruseAltitude-20f) {
                phase=0;
            }
        }
        // } else if(phase==2) {
        //     targetVector=target.position-parentTransform.position;
        // }
        Vector3 targetAngleEuler = Quaternion.LookRotation(targetVector,Vector3.up).eulerAngles;
        float pitchThrottle = pitchPID.UpdateAngle(Time.fixedDeltaTime, parentTransform.eulerAngles.x, targetAngleEuler.x);
        float yawThrottle = rollPID.UpdateAngle(Time.fixedDeltaTime, parentTransform.eulerAngles.y, targetAngleEuler.y);
        targetAngleEuler.z= (parentTransform.eulerAngles.y-targetAngleEuler.y)*Math.Abs(yawThrottle)*.66f;
        if(Mathf.Abs(targetAngleEuler.z)>90) {
            targetAngleEuler.z/=2;
        }
        float rollThrottle = yawPID.UpdateAngle(Time.fixedDeltaTime, parentTransform.eulerAngles.z, targetAngleEuler.z);

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
        entity.velocity+=throttle*thrust*Time.fixedDeltaTime*parentTransform.forward;
        
        angularVelocity.x+=pitchThrottle*turnStrength*Time.fixedDeltaTime;
        angularVelocity.y+=yawThrottle*turnStrength*Time.fixedDeltaTime;
        angularVelocity.z+=rollThrottle*turnStrength*Time.fixedDeltaTime;

        float steepness = Vector3.Angle(parentTransform.forward,Vector3.Scale(parentTransform.forward,Utils.flat));

        if(steepness>90) {
            steepness-=90;
        }

        float rollAmmount = Mathf.Abs(parentTransform.eulerAngles.z);

        while(rollAmmount>90) {
            rollAmmount-=90;
        }

        float liftFactor = 1-((1-(steepness/90))*(1-(rollAmmount/90))*LIFT_COFFEFICENT);
        

        entity.velocity+=Utils.GRAVITY*Time.fixedDeltaTime*Vector3.down*liftFactor;
        parentTransform.position+=entity.velocity*Time.fixedDeltaTime;
        entity.position=parentTransform.position;
        parentTransform.Rotate(angularVelocity);
        entity.heading=parentTransform.eulerAngles.y;
        float angluarDrag = Utils.ANGULAR_DRAG_COEFFICENT*(angularVelocity.magnitude/highRotSpeed)*(entity.velocity.magnitude/highSpeed);
        float velocityDrag = Utils.DRAG_COEFFICENT*(entity.velocity.magnitude/highSpeed);
        angluarDrag=Mathf.Clamp(angluarDrag,0f,1f);
        velocityDrag=Mathf.Clamp(velocityDrag,0f,1f);
        angularVelocity*=1-angluarDrag;
        entity.velocity*=1-velocityDrag;

        entity.speed=entity.velocity.magnitude;
        
    }
}
