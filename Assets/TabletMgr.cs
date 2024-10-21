using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

public class TabletMgr : MonoBehaviour
{

    private GameInputs input;
    private float prevPinchMag = 0;
    public int numTouches;

    // Start is called before the first frame update
    void Start()
    {
        input = new GameInputs();
        input.Enable();

        //handles touchscreen zoom
        var touch0pos = new InputAction(type: InputActionType.Value, binding: "<Touchscreen>/touch0/position");
        touch0pos.Enable();
        var touch1pos = new InputAction(type: InputActionType.Value, binding: "<Touchscreen>/touch1/position");
        touch1pos.Enable();
        touch1pos.performed += _ =>
        {
            if (numTouches == 2)
            {
                float mag = (touch0pos.ReadValue<Vector2>() - touch1pos.ReadValue<Vector2>()).magnitude;
                if (prevPinchMag == 0)
                    prevPinchMag = mag;
                float diff = mag - prevPinchMag;
                prevPinchMag = mag;
                CamZoom(-diff * 0.1f);
            }
            else
            {
                prevPinchMag = 0;
            }
        };
    }

    // Update is called once per frame
    void Update()
    {
        //numTouches = Touch.activeTouches.Count;

        /*
        if (numTouches == 0 || numTouches == 3)
        {
            moveVector += input.Camera.XZMove.ReadValue<Vector3>();
        }
        if (numTouches == 0 || numTouches == 4)
        {
            //moveVector.y += input.Camera.YMove.ReadValue<Vector2>().y;
        }
        if (numTouches == 0 || (numTouches == 1 && !UIMgr.inst.isActive))
        {
            float sens = 1;

            if (numTouches == 1 && !UIMgr.inst.isActive)
                sens = 0.1f;

            yawValue = input.Camera.Yaw.ReadValue<float>() * sens;
            pitchValue = input.Camera.Pitch.ReadValue<float>() * -sens;
        }
        */
    }

    private void CamZoom(float increment)
    {
        //myCamera.fieldOfView = Mathf.Clamp(myCamera.fieldOfView + increment, 0, 100);
    }
}
