using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Oriented3dPhysics : OrientedPhysics
{
    // Start is called before the first frame update
    
    public float initialDesiredAltitude;
    public float desiredAltitude;
    public float altitude;
    public float climbRate;
    public float ceiling = 1000;//meters
    // Update is called once per frame
    
    public override void Awake()
    {
        base.Awake();
        desiredAltitude = initialDesiredAltitude;
    }
    public override void FixedUpdate()
    {
        base.FixedUpdate();
        if(Utils.ApproximatelyEqual(desiredAltitude, altitude)) {
            ;
        } else if (desiredAltitude < altitude) {
            altitude -= climbRate * Time.fixedDeltaTime * Time.timeScale;
        } else if (desiredAltitude > altitude) {
            altitude += climbRate * Time.fixedDeltaTime * Time.timeScale;
        }
        altitude = Mathf.Clamp(altitude, 0, ceiling);
        Vector3 pos = entity.transform.localPosition;
        pos.y = altitude;
        entity.transform.localPosition = pos;
        entity.position.y = pos.y;


    }
    public void ResetAltitude() {
        desiredAltitude = initialDesiredAltitude;
        altitude = entity.position.y;
    }
}
