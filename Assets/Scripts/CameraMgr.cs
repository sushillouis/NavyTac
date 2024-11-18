using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
        
    }

    public GameObject RTSCameraRig;
    public GameObject YawNode;   // Child of RTSCameraRig
    public GameObject PitchNode; // Child of YawNode
    public GameObject RollNode;  // Child of PitchNode
    public Camera myCamera;
    //Camera is child of RollNode

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

    // Update is called once per frame
    void Update()
    {
        moveCoefficent = Mathf.Log(YawNode.transform.position.y * heightSensitivty);
        moveCoefficent = Mathf.Clamp(moveCoefficent, 0.0001f, 999f);        
    }
    public bool isRTSMode = true;

    public void MoveCameraY(float yMoveValue)
    {
        Vector3 moveVector = Vector3.zero;
        moveVector.y = yMoveValue * moveCoefficent;
        YawNode.transform.Translate(moveVector * Time.deltaTime * cameraMoveSpeed);
        float newY = Mathf.Clamp(YawNode.transform.position.y, minCameraHeight, maxCameraHeight);
        YawNode.transform.position = new(YawNode.transform.position.x, newY, YawNode.transform.position.z);
    }

    public void MoveCameraXZ(Vector2 moveValue) 
    {
        Vector3 moveVector = Vector3.zero; 
        moveVector.x += moveValue.x * moveCoefficent;
        moveVector.z += moveValue.y * moveCoefficent;
        YawNode.transform.Translate(moveVector * Time.deltaTime * cameraMoveSpeed);
    }

    public void YawCamera(float yawValue)
    {
        currentYawEulerAngles = YawNode.transform.localEulerAngles;
        currentYawEulerAngles.y += yawValue * cameraTurnRate * Time.deltaTime;
        YawNode.transform.localEulerAngles = currentYawEulerAngles;
    }

    public void PitchCamera(float pitchValue) 
    {
        currentPitchEulerAngles = PitchNode.transform.localEulerAngles;
        currentPitchEulerAngles.x += pitchValue * cameraTurnRate * Time.deltaTime;
        PitchNode.transform.localEulerAngles = currentPitchEulerAngles;
    }

    public void ToggleRTSView()
    {
        if (isRTSMode)
        {
            if (SelectionMgr.inst.selectedEntity != null) 
            {
                YawNode.transform.SetParent(SelectionMgr.inst.selectedEntity.cameraRig.transform);
                YawNode.transform.localPosition = Vector3.zero;
                YawNode.transform.localEulerAngles = Vector3.zero;
            }
        }
        else
        {
            YawNode.transform.SetParent(RTSCameraRig.transform);
            YawNode.transform.localPosition = Vector3.zero;
            YawNode.transform.localEulerAngles = Vector3.zero;
        }
        isRTSMode = !isRTSMode;
    }
}
