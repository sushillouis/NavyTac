using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class CameraMgr : MonoBehaviour
{
    public static CameraMgr inst;
    public Vector3 moveVector;
    private float yawValue;
    private float pitchValue;

    private Vector3 startYawLocalPosition;
    private Quaternion startYawLocalRotation;
    private Vector3 startPitchLocalPosition;
    private Quaternion startPitchLocalRotation;
    private Vector3 startRollLocalPosition;
    private Quaternion startRollLocalRotation;


    private void Awake()
    {
        inst = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        StoreInitialTransforms();
    }
    private Vector3 baseYawLocalPosition;
    private Quaternion baseYawLocalRotation;

    public void SetCameraPosition()
    {
        // Position camera 1500 units above and 2000 units behind Player 1
        Vector3 baseOffset = new Vector3(0, 1500, -2000);
        Quaternion headingRotation = Quaternion.Euler(0, GameMgr.inst.headingPlayer1, 0);
        Vector3 cameraPosition = GameMgr.inst.posPlayer1 + headingRotation * baseOffset;

        // Set camera position and orientation
        RTSCameraRig.transform.position = cameraPosition;
        YawNode.transform.rotation = headingRotation;

        // Look directly at Player 1's spawn point
        PitchNode.transform.LookAt(GameMgr.inst.posPlayer1);

        // Store the base transform values
        baseYawLocalPosition = YawNode.transform.localPosition;
        baseYawLocalRotation = YawNode.transform.localRotation;
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
    public float edgeScrollMargin = 100f;
    [Header("Mouse Rotation Settings")]
    public float mouseYawSensitivity = 0.1f;
    public float mousePitchSensitivity = 0.1f;
    public float minPitchAngle = -80f;
    public float maxPitchAngle = 80f;

    float moveCoefficent;
    public Vector3 currentYawEulerAngles = Vector3.zero;
    public Vector3 currentPitchEulerAngles = Vector3.zero;

    // Update is called once per frame
    void Update()
    {

        moveCoefficent = Mathf.Log(YawNode.transform.position.y * heightSensitivty);
        moveCoefficent = Mathf.Clamp(moveCoefficent, 0.0001f, 999f);
        HandleEdgeScrolling();
        HandleMiddleMouseDrag();

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
        if (ReplayMgr.inst.isReplaying)
        {
            moveVector.y += moveValue.y * moveCoefficent;
        }
        else
        {
           moveVector.z += moveValue.y * moveCoefficent; 
        }
        
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
    public void StoreInitialTransforms()
    {
        // Store all initial transform values
        startYawLocalPosition = YawNode.transform.localPosition;
        startYawLocalRotation = YawNode.transform.localRotation;

        startPitchLocalPosition = PitchNode.transform.localPosition;
        startPitchLocalRotation = PitchNode.transform.localRotation;

        startRollLocalPosition = RollNode.transform.localPosition;
        startRollLocalRotation = RollNode.transform.localRotation;
    }

    public void ToggleRTSView()
    {
        YawNode.transform.SetParent(RTSCameraRig.transform);
        YawNode.transform.localPosition = baseYawLocalPosition; // Restore saved position
        YawNode.transform.localRotation = baseYawLocalRotation;
        // if (isRTSMode)
        // {
        //     if (SelectionMgr.inst.selectedEntity != null) 
        //     {
        //         YawNode.transform.SetParent(SelectionMgr.inst.selectedEntity.cameraRig.transform);
        //         YawNode.transform.localPosition = Vector3.zero;
        //         YawNode.transform.localEulerAngles = Vector3.zero;
        //     }
        //     else{
        //         YawNode.transform.SetParent(RTSCameraRig.transform);
        //         YawNode.transform.localPosition = baseYawLocalPosition; // Restore saved position
        //         YawNode.transform.localRotation = baseYawLocalRotation;
        //         isRTSMode = !isRTSMode;
        //     }
        // }
        // else
        // {
        //     // Restore the base RTS position and rotation
        //     YawNode.transform.SetParent(RTSCameraRig.transform);
        //     YawNode.transform.localPosition = baseYawLocalPosition; // Restore saved position
        //     YawNode.transform.localRotation = baseYawLocalRotation; // Restore saved rotation
        // }
        // isRTSMode = !isRTSMode;
    }
    public void ResetCamera()
    {
        // Reset all nodes to their initial transforms
        YawNode.transform.localPosition = startYawLocalPosition;
        YawNode.transform.localRotation = startYawLocalRotation;

        PitchNode.transform.localPosition = startPitchLocalPosition;
        PitchNode.transform.localRotation = startPitchLocalRotation;

        RollNode.transform.localPosition = startRollLocalPosition;
        RollNode.transform.localRotation = startRollLocalRotation;
    }
    private void HandleEdgeScrolling()
    {
        if (!isRTSMode) return;
        Vector2 mousePos = Mouse.current.position.ReadValue();
        if (mousePos.x < 0 || mousePos.x > Screen.width || mousePos.y < 0 || mousePos.y > Screen.height)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector2 moveInput = Vector2.zero;

        // Constant speed scrolling when within edge margins
        if (mousePosition.x <= edgeScrollMargin)
        {
            moveInput.x = -1f;
        }
        else if (mousePosition.x >= Screen.width - edgeScrollMargin)
        {
            moveInput.x = 1f;
        }
        if (ReplayMgr.inst.isReplaying)
        {
            
        }
        if (mousePosition.y <= edgeScrollMargin)
        {
            moveInput.y = -1f;
        }
        else if (mousePosition.y >= Screen.height - edgeScrollMargin)
        {
            moveInput.y = 1f;
        }

        if (moveInput != Vector2.zero)
        {
            MoveCameraXZ(moveInput);
        }
    }
    private void HandleMiddleMouseDrag()
    {
        if (Mouse.current.middleButton.isPressed)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            // Yaw rotation (horizontal mouse movement)
            float yawChange = mouseDelta.x * mouseYawSensitivity;
            YawNode.transform.Rotate(Vector3.up * yawChange, Space.Self);

            // Pitch rotation (vertical mouse movement)
            float pitchChange = mouseDelta.y * mousePitchSensitivity;
            float currentPitch = PitchNode.transform.localEulerAngles.x;

            // Convert to -180 to 180 range for clamping
            if (currentPitch > 180f)
                currentPitch -= 360f;

            currentPitch -= pitchChange; // Adjust based on mouse movement

            currentPitch = Mathf.Clamp(currentPitch, minPitchAngle, maxPitchAngle);
            PitchNode.transform.localEulerAngles = new Vector3(currentPitch, 0f, 0f);
        }
    }

    public void ReplayCamera()
    {
        RTSCameraRig.transform.position = Vector3.zero;
        YawNode.transform.localPosition = new Vector3(0f, 14000f, 0f);
        YawNode.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        PitchNode.transform.localPosition = Vector3.zero;
        PitchNode.transform.localRotation = Quaternion.Euler(0f, 0f, 0f); 
    }

}
