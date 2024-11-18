using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlMgr : MonoBehaviour
{
    public static ControlMgr inst;

    private void Awake()
    {
        inst = this;
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    public float deltaSpeed = 1;
    public float deltaHeading = 2;
    private float speedChange;
    private float headingChange;

    // Update is called once per frame
    void Update()
    {

    }

    public void ChangeSpeed(float speedChange)
    {
        if (SelectionMgr.inst.selectedEntity != null) 
        {
            SelectionMgr.inst.selectedEntity.desiredSpeed += speedChange * deltaSpeed;
            SelectionMgr.inst.selectedEntity.desiredSpeed =
                Utils.Clamp(SelectionMgr.inst.selectedEntity.desiredSpeed, SelectionMgr.inst.selectedEntity.minSpeed, SelectionMgr.inst.selectedEntity.maxSpeed);
        }
    }

    public void ChangeHeading(float headingChange)
    {
        if (SelectionMgr.inst.selectedEntity != null)
        {
            SelectionMgr.inst.selectedEntity.desiredHeading += headingChange * deltaHeading;
            SelectionMgr.inst.selectedEntity.desiredHeading = Utils.Degrees360(SelectionMgr.inst.selectedEntity.desiredHeading);
        }
    }
}

    
    //------------------------------------------

