using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JudeMissile : MonoBehaviour
{
    public float thrust =100f;
    public float turnStrength = 90f;
    public float maxSpeed = 1000f;
    public float cruseAltitude = 500f;
    public float terminalRadius;
    public Entity target = null;
    [SerializeField] PIDController pitchPID;
    [SerializeField] PIDController rollPID;
    [SerializeField] PIDController yawPID;
    [SerializeField] Vector3 velocity = Vector3.zero;
    [SerializeField] Vector3 angularVelocity = Vector3.zero;
    [SerializeField] Wake wake;
    [SerializeField] int phase = 0;
    static Vector3 flat = new(1,0,1);

    void FixedUpdate()
    {
        if(target==null || (phase==2 && Vector3.Distance(transform.position,target.position)<60f)) {
            target=EntityMgr.inst.entities[0];
            FXMgr.inst.CreateExplosionAt(transform.position);
            Destroy(gameObject);
            return;
        }
        float throttle = 1f;
        Vector3 targetVector = Vector3.up;
        
        if(phase==0) {
            if(transform.position.y>=cruseAltitude-1f) {
                phase=1;
            }
        } else if(phase==1) {
            throttle=.8f;
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
            targetVector=target.position-transform.position;
        }
        Vector3 targetAngleEuler = Quaternion.LookRotation(targetVector,Vector3.up).eulerAngles;
        float pitchThrottle = pitchPID.UpdateAngle(Time.fixedDeltaTime, transform.eulerAngles.x, targetAngleEuler.x);
        float rollThrottle = rollPID.UpdateAngle(Time.fixedDeltaTime, transform.eulerAngles.y, targetAngleEuler.y);
        float yawThrottle = yawPID.UpdateAngle(Time.fixedDeltaTime, transform.eulerAngles.z, targetAngleEuler.z);

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
        velocity+=throttle*thrust*Time.fixedDeltaTime*transform.forward;
        
        angularVelocity.x+=pitchThrottle*turnStrength*Time.fixedDeltaTime;
        angularVelocity.y+=rollThrottle*turnStrength*Time.fixedDeltaTime;
        angularVelocity.z+=yawThrottle*turnStrength*Time.fixedDeltaTime;

        velocity+=Utils.GRAVITY*Time.fixedDeltaTime*Vector3.down;
        transform.position+=velocity*Time.fixedDeltaTime;
        transform.Rotate(angularVelocity);
        angularVelocity*=1-Utils.ANGULAR_DRAG_COEFFICENT;
        velocity*=1-Utils.DRAG_COEFFICENT;
    }
}
