using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class CameraMgr : MonoBehaviour
{
    public static CameraMgr inst;
    private Vector3 moveVector;
    private float yawValue;
    private float pitchValue;
    

    private void Awake()
    {
        inst = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        onCameraMove.AddListener(UpdateMoveCoffef);
    }

    // public GameObject RTSCameraRig;
    // public GameObject myCamera.gameObject;   // Child of RTSCameraRig
    // public GameObject myCamera.gameObject; // Child of myCamera.gameObject
    // public GameObject myCamera.gameObject;  // Child of myCamera.gameObject
    public Camera myCamera;
    //Camera is child of myCamera.gameObject

    public float cameraMoveSpeed = 500;

    /// <summary>
    /// Note this is reduced by a log scale;
    /// </summary>
    public float heightSensitivty = 5;
    public float maxCameraHeight = 9600;
    public float minCameraHeight = 20;
    public float cameraTurnRate = 10;
    float moveCoefficent;
    public Vector3 currentYawEulerAngles = Vector3.zero;
    public Vector3 currentPitchEulerAngles = Vector3.zero;
    public UnityEvent onCameraMove;

    // Update is called once per frame
    void Update()
    {
             
    }

    public void UpdateMoveCoffef() {
        moveCoefficent = Mathf.Log(myCamera.transform.position.y * heightSensitivty);
        moveCoefficent = Mathf.Clamp(moveCoefficent, 0.0001f, 999f);   
    }
    public bool isRTSMode = true;

    public void MoveCameraY(float yMoveValue)
    {
        myCamera.transform.Translate(moveCoefficent * yMoveValue * Vector3.up,Space.World);
        float newY = Mathf.Clamp(myCamera.transform.position.y, minCameraHeight, maxCameraHeight);
        myCamera.transform.position.Set(myCamera.transform.position.x,newY,myCamera.transform.position.z);
        onCameraMove.Invoke();
    }

    public void MoveCameraXZ(Vector2 moveValue) 
    {
        Vector3 moveVector = Vector3.zero; 
        moveVector.x += moveValue.x * moveCoefficent;
        moveVector.z += moveValue.y * moveCoefficent;
        myCamera.transform.Translate(Quaternion.Euler(0,myCamera.transform.eulerAngles.y,0)* (cameraMoveSpeed * Time.deltaTime * moveVector),Space.World);
        onCameraMove.Invoke();
    }

    public void YawCamera(float yawValue)
    {
        // currentYawEulerAngles = myCamera.transform.localEulerAngles;
        // currentYawEulerAngles.y += yawValue * cameraTurnRate * Time.deltaTime;
        myCamera.transform.Rotate(Vector3.up,yawValue * cameraTurnRate * Time.deltaTime,Space.World);
        
    }

    public void PitchCamera(float pitchValue) 
    {
        // currentPitchEulerAngles = myCamera.transform.localEulerAngles;
        // currentPitchEulerAngles.x += pitchValue * cameraTurnRate * Time.deltaTime;
        // myCamera.transform.localEulerAngles = currentPitchEulerAngles;
        myCamera.transform.Rotate(Vector3.right, pitchValue * cameraTurnRate* Time.deltaTime);
        float newX = Mathf.Clamp(myCamera.transform.eulerAngles.x,-88f,88f);
        myCamera.transform.eulerAngles.Set(newX,myCamera.transform.eulerAngles.y,myCamera.transform.eulerAngles.z);
    }

    public void ToggleRTSView()
    {
        if (isRTSMode)
        {
            if (SelectionMgr.inst.selectedEntity != null) 
            {
                myCamera.transform.SetParent(SelectionMgr.inst.selectedEntity.cameraRig.transform);
                myCamera.transform.localPosition = Vector3.zero;
                myCamera.transform.localEulerAngles = Vector3.zero;
            }
        }
        else
        {
            myCamera.transform.SetParent(null);
            myCamera.transform.localPosition = Vector3.zero;
            myCamera.transform.localEulerAngles = Vector3.zero;
        }
        isRTSMode = !isRTSMode;
        onCameraMove.Invoke();
    }
}
