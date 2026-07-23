using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrientedPhysics : MonoBehaviour
{
    public Entity entity;
    
    public virtual void Awake() {
        entity = GetComponentInParent<Entity>();
        entity.phx = this;
        entity.position = entity.transform.localPosition;
    }

    // Start is called before the first frame update
    void Start()
    {

    }




    // FixedUpdate is called once per frame
    public virtual void FixedUpdate()
    {
        // Speed update
        float speedChangeAmount = entity.acceleration * Time.fixedDeltaTime * Time.timeScale;
        entity.speed = Mathf.MoveTowards(entity.speed, entity.desiredSpeed, speedChangeAmount);
        entity.speed = Utils.Clamp(entity.speed, entity.minSpeed, entity.maxSpeed); // Ensure speed stays within defined limits

        // Heading update
        
        float angleChangeAmount = entity.turnRate * Time.fixedDeltaTime * Time.timeScale; // Adjust angle change based on turn rate and time scale
        entity.heading = Mathf.MoveTowardsAngle(entity.heading, entity.desiredHeading, angleChangeAmount);
        entity.heading = Utils.Degrees360(entity.heading); // Normalize heading to 0-360 range

        // Calculate velocity vector based on new speed and heading
        entity.velocity.x = Mathf.Sin(entity.heading * Mathf.Deg2Rad) * entity.speed;
        entity.velocity.y = 0; // Assuming 2D movement on XZ plane, so Y velocity is zero
        entity.velocity.z = Mathf.Cos(entity.heading * Mathf.Deg2Rad) * entity.speed;

        // Update position
        entity.position += entity.velocity * Time.fixedDeltaTime * Time.timeScale; // Use compound assignment
        entity.transform.localPosition = entity.position;

        // Update GameObject's rotation
        eulerRotation.y = entity.heading;
        entity.transform.localEulerAngles = eulerRotation;
    }

    public Vector3 eulerRotation = Vector3.zero;

}
