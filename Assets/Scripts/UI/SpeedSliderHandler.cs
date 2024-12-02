using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpeedSlider : MonoBehaviour
{
    public Slider slider;
    public Entity entity;

    public ControlMgr controlMgr;

    private void Awake() {
        entity = GetComponentInParent<Entity>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        switch(slider.value)
        {
            case <= (float).125:
                controlMgr.ChangeSpeed(1);
            break;
            case <= (float).25:
                controlMgr.ChangeSpeed((float).66);
            break;
            case <= (float).375:
                controlMgr.ChangeSpeed((float).33);
            break;
            case <= (float).5:
                controlMgr.ChangeSpeed(0);
            break;
            case <= (float).61:
                controlMgr.ChangeSpeed((float).33);
            break;
            case <= (float).725:
                controlMgr.ChangeSpeed((float).66);
            break;
            case <= (float).86:
                controlMgr.ChangeSpeed(1);
            break;
            case <= (float).98:
                controlMgr.ChangeSpeed(1);
            break;
            case <= 1:
                controlMgr.ChangeSpeed(1);
            break;
        }
    }
}
