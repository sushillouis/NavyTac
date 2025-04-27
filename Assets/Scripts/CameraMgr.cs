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
        rtsRollNodeLocalPosition = RollNode.transform.localPosition;
    }

    public GameObject RTSCameraRig;
    public GameObject YawNode;   // Child of RTSCameraRig
    public GameObject PitchNode; // Child of YawNode
    public GameObject RollNode;  // Child of PitchNode
    public Camera myCamera;

    public float cameraMoveSpeed = 500;
    public float heightSensitivty = 5;
    public float maxCameraHeight = 9600;
    public float minCameraHeight = 20;
    public float cameraTurnRate = 10;
    float moveCoefficent;
    public Vector3 currentYawEulerAngles = Vector3.zero;
    public Vector3 currentPitchEulerAngles = Vector3.zero;

    // Orbit variables
    public float orbitSpeed = 10f; // Degrees per second
    private bool isOrbiting = false;
    private Vector3 rtsRollNodeLocalPosition;
    private Vector3 savedRollNodeLocalPosition;

    void Update()
    {
        moveCoefficent = Mathf.Log(YawNode.transform.position.y * heightSensitivty);
        moveCoefficent = Mathf.Clamp(moveCoefficent, 0.0001f, 999f);

        if (isOrbiting)
        {
            currentYawEulerAngles = YawNode.transform.localEulerAngles;
            currentYawEulerAngles.y += orbitSpeed * Time.deltaTime;
            YawNode.transform.localEulerAngles = currentYawEulerAngles;
        }
    }

    public bool isRTSMode = true;

    public void MoveCameraY(float yMoveValue)
    {
        if (float.IsNaN(yMoveValue) )return;
        Vector3 moveVector = Vector3.zero;
        moveVector.y = yMoveValue * moveCoefficent;
        YawNode.transform.Translate(moveVector * Time.deltaTime * cameraMoveSpeed);
        float newY = Mathf.Clamp(YawNode.transform.position.y, minCameraHeight, maxCameraHeight);
        YawNode.transform.position = new Vector3(YawNode.transform.position.x, newY, YawNode.transform.position.z);
    }

    public void MoveCameraXZ(Vector2 moveValue)
    {
        Vector3 moveVector = new Vector3(moveValue.x, 0, moveValue.y) * moveCoefficent;
        YawNode.transform.Translate(moveVector * Time.deltaTime * cameraMoveSpeed);
    }

    public void YawCamera(float yawInput)
    {
        currentYawEulerAngles = YawNode.transform.localEulerAngles;
        currentYawEulerAngles.y += yawInput * cameraTurnRate * Time.deltaTime;
        YawNode.transform.localEulerAngles = currentYawEulerAngles;
    }

    public void PitchCamera(float pitchInput)
    {
        currentPitchEulerAngles = PitchNode.transform.localEulerAngles;
        currentPitchEulerAngles.x = Mathf.Clamp(currentPitchEulerAngles.x + pitchInput * cameraTurnRate * Time.deltaTime, -80f, 80f);
        PitchNode.transform.localEulerAngles = currentPitchEulerAngles;
    }

    public void ToggleRTSView()
    {
        if (isRTSMode)
        {
            Entity selectedEntity = SelectionMgr.inst.selectedEntity;
            if (selectedEntity != null)
            {
              // Switch to entity view
            Vector3 entityPos = selectedEntity.transform.position;
            Vector3 cameraPos = myCamera.transform.position;
            
            // Calculate direction and distance
            Vector3 direction = (cameraPos - entityPos).normalized;
            float distance = Vector3.Distance(entityPos, cameraPos);
            
            // Calculate angles with height reduction
            float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float pitch = Mathf.Asin(direction.y) * Mathf.Rad2Deg;

            // Save state before changing hierarchy
            savedRollNodeLocalPosition = RollNode.transform.localPosition;
            
            // Set up orbital camera rig
            YawNode.transform.SetParent(selectedEntity.cameraRig.transform);
            YawNode.transform.localPosition = Vector3.zero;
            
            // Apply rotations with downward tilt adjustment
            YawNode.transform.localEulerAngles = new Vector3(0, yaw, 0);
            
            // Add 15 degree downward tilt and clamp between -20° and 45°
            float targetPitch = Mathf.Clamp(pitch - 15f, -20f, 45f);
            PitchNode.transform.localEulerAngles = new Vector3(targetPitch, 0, 0);
            
            // Position camera closer with height adjustment
            float verticalOffset = Mathf.Lerp(2f, 5f, Mathf.InverseLerp(minCameraHeight, maxCameraHeight, distance));
            RollNode.transform.localPosition = new Vector3(0, -verticalOffset, -distance * 0.2f);

            isOrbiting = true;
            isRTSMode = false;
            }
        }
        else
        {
            // Return to RTS mode
            YawNode.transform.SetParent(RTSCameraRig.transform);
            YawNode.transform.localPosition = Vector3.zero;
            YawNode.transform.localEulerAngles = Vector3.zero;
            PitchNode.transform.localEulerAngles = Vector3.zero;
            RollNode.transform.localPosition = savedRollNodeLocalPosition;

            isOrbiting = false;
            isRTSMode = true;
        }
    }
}