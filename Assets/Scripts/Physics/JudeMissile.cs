using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JudeMissile : BasicMissile
{

    public override void Init(TactPlayer player) {
        target.ai.OnDieEvent +=TargetDead;
        team = player;
    }

    void FixedUpdate()
    {
        if(phase==2 && transform.position.y<=2f) {
            if (Vector3.Distance(transform.position,target.position)<60f+target.width) {
                WeaponsMgr.inst.CalculateAndDealDamage(target, WeaponType.CruseMissile, 25f);
            }            
            Explode();
            return;
        }
        float throttle = .5f;
        Vector3 targetVector = Vector3.up;
        
        if(phase==0) {
            if(transform.position.y>=cruseAltitude-1f) {
                phase=1;
            }
        } else if(phase==1) {
            throttle=.4f;
            Vector3 temp = target.position-transform.position;
            temp.y=0;
            targetVector=temp.normalized+(.5f*Vector3.up);
            Vector3 temp1 = transform.position;
            temp1.y=0;
            Vector3 temp2 = target.position;
            temp2.y=0;
            if(Vector3.Distance(temp1,temp2)<terminalRadius) {
                phase=2;
            }
        } else if(phase==2) {
            throttle=1f;
            targetVector=target.position-transform.position;
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


        _velocity += Utils.GRAVITY * Time.fixedDeltaTime * Vector3.down * liftFactor;
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
