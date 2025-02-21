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

    void Start()
    {
        RTSCameraRig.transform.position = CalculateNewPosition(RTSCameraRig.transform.position, GameMgr.inst.posPlayer1);
        YawNode.transform.rotation = Quaternion.Euler(0, GameMgr.inst.headingPlayer1, 0);
    }

    Vector3 CalculateNewPosition(Vector3 current, Vector3 offset)
    {
        return new Vector3(
            (Mathf.Abs(current.x) + Mathf.Abs(offset.x)) * Mathf.Sign(offset.x),
            current.y,
            (Mathf.Abs(current.z) + Mathf.Abs(offset.z)) * Mathf.Sign(offset.z)
        );
    }

    public GameObject RTSCameraRig;
    public GameObject YawNode;
    public GameObject PitchNode;
    public GameObject RollNode;
    public Camera myCamera;
[Header("Current Position")]
[SerializeField] private float currentXPosition;
[SerializeField] private float currentYPosition;
[SerializeField] private float currentZPosition;
    public float cameraMoveSpeed = 500;
    public float heightSensitivty = 5;
    public float maxCameraHeight = 9600;
    public float minCameraHeight = 20;
    public float cameraTurnRate = 10;
    float moveCoefficent;
    public Vector3 currentYawEulerAngles = Vector3.zero;
    public Vector3 currentPitchEulerAngles = Vector3.zero;

    // New variables for camera constraints and tilt
    public float minX = -500f;
    public float maxX = 500f;
    public float minZ = -500f;
    public float maxZ = 500f;
    public float tiltStartHeight = 500f;
    public float maxTiltAngle = 30f;

    void Update()
    {
        moveCoefficent = Mathf.Log(YawNode.transform.position.y * heightSensitivty);
        moveCoefficent = Mathf.Clamp(moveCoefficent, 0.0001f, 999f);
        // Track position values
    currentXPosition = YawNode.transform.position.x;
    currentYPosition = YawNode.transform.position.y;
    currentZPosition = YawNode.transform.position.z;
        // Handle automatic camera tilt in RTS mode
        if (isRTSMode)
        {
            float currentY = YawNode.transform.position.y;
            if (currentY > tiltStartHeight)
            {
                float t = (currentY - tiltStartHeight) / (maxCameraHeight - tiltStartHeight);
                t = Mathf.Clamp01(t);
                float targetPitch = Mathf.Lerp(0, maxTiltAngle, t);
                currentPitchEulerAngles.x = targetPitch;
            }
            else
            {
                currentPitchEulerAngles.x = 0;
            }
            PitchNode.transform.localEulerAngles = currentPitchEulerAngles;
        }
        
    }

    public bool isRTSMode = true;

    public void MoveCameraY(float yMoveValue)
    {
        if (float.IsNaN(yMoveValue) || float.IsNaN(moveCoefficent) || float.IsNaN(cameraMoveSpeed)) return;
        Vector3 moveVector = Vector3.zero;
        moveVector.y = yMoveValue * moveCoefficent;
        YawNode.transform.Translate(moveVector * Time.deltaTime * cameraMoveSpeed);
        float newY = Mathf.Clamp(YawNode.transform.position.y, minCameraHeight, maxCameraHeight);
        YawNode.transform.position = new Vector3(YawNode.transform.position.x, newY, YawNode.transform.position.z);
    }

    public void MoveCameraXZ(Vector2 moveValue)
    {
        if (float.IsNaN(moveValue.x) || float.IsNaN(moveValue.y) || float.IsNaN(moveCoefficent) || float.IsNaN(cameraMoveSpeed)) return;
        Vector3 moveVector = Vector3.zero;
        moveVector.x += moveValue.x * moveCoefficent;
        moveVector.z += moveValue.y * moveCoefficent;
        YawNode.transform.Translate(moveVector * Time.deltaTime * cameraMoveSpeed);

        // Clamp X/Z position
        Vector3 clampedPosition = YawNode.transform.position;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, minX, maxX);
        clampedPosition.z = Mathf.Clamp(clampedPosition.z, minZ, maxZ);
        YawNode.transform.position = clampedPosition;
    }

    public void YawCamera(float yawValue)
    {
        currentYawEulerAngles = YawNode.transform.localEulerAngles;
        currentYawEulerAngles.y += yawValue * cameraTurnRate * Time.deltaTime;
        YawNode.transform.localEulerAngles = currentYawEulerAngles;
    }

    public void PitchCamera(float pitchValue)
    {
        // Prevent manual pitch when in RTS mode above tilt height
        if (isRTSMode && YawNode.transform.position.y > tiltStartHeight) return;
        
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