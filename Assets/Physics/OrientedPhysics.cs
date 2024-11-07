using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrientedPhysics : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        entity = GetComponentInParent<Entity>();
        entity.position = entity.transform.localPosition;
    }

    public Entity entity;


    // Update is called once per frame
    void FixedUpdate()
    {
        if(Utils.ApproximatelyEqual(entity.speed, entity.desiredSpeed)) {
            ;
        } else if(entity.speed < entity.desiredSpeed) {
            entity.speed = entity.speed + entity.acceleration * Time.fixedDeltaTime;
        } else if (entity.speed > entity.desiredSpeed) {
            entity.speed = entity.speed - entity.acceleration * Time.fixedDeltaTime;
        }
        entity.speed = Utils.Clamp(entity.speed, entity.minSpeed, entity.maxSpeed);

        //heading
        if (Utils.ApproximatelyEqual(entity.heading, entity.desiredHeading)) {
            ;
        } else if (Utils.AngleDiffPosNeg(entity.desiredHeading, entity.heading) > 0) {
            entity.heading += entity.turnRate * Time.fixedDeltaTime;
        } else if (Utils.AngleDiffPosNeg(entity.desiredHeading, entity.heading) < 0) {
            entity.heading -= entity.turnRate * Time.fixedDeltaTime;
        }
        entity.heading = Utils.Degrees360(entity.heading);
        //
        entity.velocity.x = Mathf.Sin(entity.heading * Mathf.Deg2Rad) * entity.speed;
        entity.velocity.y = 0;
        entity.velocity.z = Mathf.Cos(entity.heading * Mathf.Deg2Rad) * entity.speed;

        entity.position = entity.position + entity.velocity * Time.fixedDeltaTime;
        entity.transform.localPosition = entity.position;

        eulerRotation.y = entity.heading;
        entity.transform.localEulerAngles = eulerRotation;
    }

    public Vector3 eulerRotation = Vector3.zero;


}
