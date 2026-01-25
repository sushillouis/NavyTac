using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.XR;
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

    private Coroutine bRollCoroutine;
    public bool isBrollActive = false;
    public bool isEdgeScrollingEnabled = true;
    public bool isMiddleMouseDragEnabled = true;
    public bool isReplayScrollEnabled = true;

    // New B-roll control flags
    public bool playBrollOnce = false; // Play B-roll only once per game session
    public bool neverPlayBroll = false; // Never play B-roll
    public bool playBrollEveryScenario = true; // Play B-roll before every scenario
    private bool hasBrollPlayed = false; // Tracks if B-roll has played in this session

    private void Awake()
    {
        inst = this;
    }

    void Start()
    {
        StoreInitialTransforms();
    }

    private Vector3 baseYawLocalPosition;
    private Quaternion baseYawLocalRotation;
    private Vector3 basePitchLocalPosition;
    private Quaternion basePitchLocalRotation;
    private Vector3 baseRollLocalPosition;
    private Quaternion baseRollLocalRotation;

    public void SetCameraPosition()
    {
        // Stop any existing B-roll to prevent multiple instances
        if (bRollCoroutine != null)
        {
            StopCoroutine(bRollCoroutine);
        }

        if (OpenOceanMain.inst.currentTrainingState == TrainingState.Tutorial)
        {
            // Reset camera to initial position
            OpenOceanMain.inst.SkipButton.gameObject.SetActive(false);
            ResetCamera();
            SetupInitialGameCamera();
            return;
        }

        // Check B-roll settings
        if (neverPlayBroll)
        {
            // Skip B-roll and go straight to game camera setup
            OpenOceanMain.inst.SkipButton.gameObject.SetActive(false);
            ResetCamera();
            SetupInitialGameCamera();
            return;
        }

        if (playBrollOnce && hasBrollPlayed)
        {
            // Skip B-roll if it has already played once
            OpenOceanMain.inst.SkipButton.gameObject.SetActive(false);
            ResetCamera();
            SetupInitialGameCamera();
            return;
        }

        // Show skip button and add listener
        if (OpenOceanMain.inst != null && OpenOceanMain.inst.SkipButton != null)
        {
            OpenOceanMain.inst.SkipButton.gameObject.SetActive(true);
            OpenOceanMain.inst.SkipButton.onClick.RemoveAllListeners(); // Clear existing listeners
            OpenOceanMain.inst.SkipButton.onClick.AddListener(ExitBRollAndStartGame);
        }

        // Perform a B-roll if allowed
        if (playBrollEveryScenario || (playBrollOnce && !hasBrollPlayed))
        {
            bRollCoroutine = StartCoroutine(BRollAndSetCameraPosition());
            if (playBrollOnce)
            {
                hasBrollPlayed = true; // Mark B-roll as played for this session
            }
        }
        else
        {
            // No B-roll, set up game camera directly
            SetupInitialGameCamera();
        }
    }

    private IEnumerator BRollAndSetCameraPosition()
    {
        yield return new WaitForSeconds(.1f); // Optional delay before starting B-roll
        isBrollActive = true;
        isEdgeScrollingEnabled = false;
        isMiddleMouseDragEnabled = false;
        UIMgr.inst.inputs.Disable();

        try
        {
            // Slowly orbit halfway (semi-circle) around the scenario center for 6 seconds (slower B-roll)
            float duration = 5f;
            float elapsed = 0f;
            Vector3 scenarioCenter = ScenarioGenerator.inst?.posPlayer1List?.FirstOrDefault() ?? Vector3.zero; // Or use a more appropriate center if needed
            float radius = 4000f;
            float height = 2500f;

            // Start angle (e.g., 0 degrees) to end angle (e.g., 180 degrees) for a semi-circle
            float startAngle = 0f;
            float endAngle = 180f;

            while (elapsed < duration)
            {
                float angle = Mathf.Lerp(startAngle, endAngle, elapsed / duration);
                float rad = angle * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Sin(rad) * radius, height, Mathf.Cos(rad) * radius);
                RTSCameraRig.transform.position = scenarioCenter + offset;
                RTSCameraRig.transform.LookAt(scenarioCenter);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // B-roll of the complete map: orbit around the map center at a higher altitude and larger radius
            float mapBRollDuration = 10f;
            float mapElapsed = 0f;
            Vector3 mapCenter = Vector3.zero;
            float mapRadius = 9125f; // Half of 18250
            float mapHeight = 6000f;

            // Calculate direction from map center to Player 1
            Vector3 playerPos = ScenarioGenerator.inst?.posPlayer1List?.FirstOrDefault() ?? Vector3.zero;
            Vector3 toPlayer = (playerPos - mapCenter).normalized;
            // Get the forward direction (z axis) and right direction (x axis) on the XZ plane
            Vector3 forward = new Vector3(toPlayer.x, 0, toPlayer.z).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward);

            // Start at -180 degrees (left of player) to +180 degrees (right of player), so the semi-circle faces Player 1
            float mapStartAngle = -180f;
            float mapEndAngle = 180f;

            while (mapElapsed < mapBRollDuration)
            {
                float angle = Mathf.Lerp(mapStartAngle, mapEndAngle, mapElapsed / mapBRollDuration);
                float rad = angle * Mathf.Deg2Rad;
                // Offset is rotated around the forward/right axes so the semi-circle faces Player 1
                Vector3 offset = (Mathf.Cos(rad) * forward + Mathf.Sin(rad) * right) * mapRadius;
                offset.y = mapHeight;
                RTSCameraRig.transform.position = mapCenter + offset;
                Vector3 player1Pos = ScenarioGenerator.inst?.posPlayer1List?.FirstOrDefault() ?? Vector3.zero;
                RTSCameraRig.transform.LookAt(player1Pos);
                mapElapsed += Time.deltaTime;
                yield return null;
            }

            // When the B-roll completes, call the exit function to clean up and start the game.
            ExitBRollAndStartGame();
            yield break;
        }
        finally
        {
            isBrollActive = false;
            isEdgeScrollingEnabled = true;
            isMiddleMouseDragEnabled = true;
            UIMgr.inst.inputs.Enable();
        }
    }

    /// <summary>
    /// Exits the B-Roll sequence and sets the camera to the initial game position.
    /// </summary>
    public void ExitBRollAndStartGame()
    {
        if (bRollCoroutine != null)
        {
            StopCoroutine(bRollCoroutine);
            bRollCoroutine = null;
        }

        // Hide the skip button and remove listeners
        if (OpenOceanMain.inst != null && OpenOceanMain.inst.SkipButton != null)
        {
            OpenOceanMain.inst.SkipButton.gameObject.SetActive(false);
            OpenOceanMain.inst.SkipButton.onClick.RemoveAllListeners();
        }
        isBrollActive = false;
        isEdgeScrollingEnabled = true;
        isMiddleMouseDragEnabled = true;
        UIMgr.inst.inputs.Enable();
        SetupInitialGameCamera();
    }

    private void SetupInitialGameCamera()
    {
        ResetCamera();
        // Position camera 1500 units above and 2000 units behind Player 1
        Vector3 baseOffset = new Vector3(0, 2000, -3000);
        float playerHeading = ScenarioGenerator.inst?.headingPlayer1List?.FirstOrDefault() ?? 0f;
        Vector3 player1Position = ScenarioGenerator.inst?.posPlayer1List?.FirstOrDefault() ?? Vector3.zero;
        Quaternion headingRotation = Quaternion.Euler(0, playerHeading, 0);
        Vector3 cameraPosition = player1Position + headingRotation * baseOffset;

        // Set camera position and orientation
        RTSCameraRig.transform.position = cameraPosition;
        YawNode.transform.rotation = headingRotation;

        // Look directly at Player 1's spawn point
        Vector3 lookAtPosition = ScenarioGenerator.inst?.posPlayer1List?.FirstOrDefault() ?? Vector3.zero;
        PitchNode.transform.LookAt(lookAtPosition);
        GameMgr.inst.isIntroPlaying = false;
        foreach (Entity e in EntityMgr.inst.entities)
        {
            if (e != null && e.TryGetComponent<GreyOverlayGenerator>(out var greyOverlay))
            {
                greyOverlay.ApplyGreyOverlay();
            }
        }

        // Store the base transform values
        baseYawLocalPosition = YawNode.transform.localPosition;
        baseYawLocalRotation = YawNode.transform.localRotation;
        basePitchLocalPosition = PitchNode.transform.localPosition;
        basePitchLocalRotation = PitchNode.transform.localRotation;
        baseRollLocalPosition = RollNode.transform.localPosition;
        baseRollLocalRotation = RollNode.transform.localRotation;
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

    [Header("Scroll Settings")]
    [Tooltip("Multiplier for non-replay mouse wheel vertical movement (Y-axis). Lower = slower.")]
    [Range(0.01f, 10f)]
    public float yScrollFactor = 0.25f;

    // Update is called once per frame
    void Update()
    {

        moveCoefficent = Mathf.Log(YawNode.transform.position.y * heightSensitivty);
        moveCoefficent = Mathf.Clamp(moveCoefficent, 0.0001f, 999f);
        // HandleEdgeScrolling();
        HandleMiddleMouseDrag();
        HandleReplayScrollWheelHeight();

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

    // Use this for mouse wheel Y movement outside replay to apply an adjustable sensitivity
    public void MoveCameraYFromScroll(float scrollDelta)
    {
        // Many mice report deltas in steps of ~120; keeping raw delta and scaling by inspector factor is simpler to tune
        MoveCameraY(scrollDelta * yScrollFactor);
    }
    // This field can be adjusted in the Inspector to change scroll sensitivity during replay.
    [Range(0.01f, 10f)]
    public float replayScrollFactor = 0.8f; // Lower value = slower zoom per wheel notch

    /// <summary>
    /// Handles camera height adjustment using the mouse scroll wheel during replay mode.
    /// This method should be called from Update().
    /// </summary>
    private void HandleReplayScrollWheelHeight()
    {
        if (ReplayMgr.inst.isReplaying)
        {
            if (!isReplayScrollEnabled) return;
            Vector3 moveVector = Vector3.zero;
            // Read the scroll wheel's vertical movement delta for this frame.
            float scrollInputY = Mouse.current.scroll.ReadValue().y;

            if (scrollInputY != 0)
            {
                // Normalize the scroll input. Mouse scroll delta is often in multiples of 120.
                // Dividing by 120f gives a value like +1.0 or -1.0 per notch.
                float normalizedScroll = scrollInputY / 120f;

                // Determine the amount to move. Positive scroll (wheel forward/up) should increase height.
                // MoveCameraY expects a positive value to move up.
                float moveAmount = normalizedScroll * replayScrollFactor; // reduced by lower default factor
                moveVector.z = moveAmount * moveCoefficent;

            }
            YawNode.transform.Translate(moveVector * Time.deltaTime * cameraMoveSpeed);
        }
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
        YawNode.transform.localPosition = baseYawLocalPosition; // Restore saved position
        YawNode.transform.localRotation = baseYawLocalRotation; // Restore saved rotation
        PitchNode.transform.localPosition = basePitchLocalPosition; // Restore saved position
        PitchNode.transform.localRotation = basePitchLocalRotation; // Restore saved rotation
        RollNode.transform.localPosition = baseRollLocalPosition; // Restore saved position
        RollNode.transform.localRotation = baseRollLocalRotation; // Restore saved rotation
    }
    public void ResetCamera()
    {
        // Reset all nodes and RTSCameraRig to zero position and rotation
        RTSCameraRig.transform.position = Vector3.zero;
        RTSCameraRig.transform.rotation = Quaternion.identity;

        YawNode.transform.localPosition = Vector3.zero;
        YawNode.transform.localRotation = Quaternion.identity;

        PitchNode.transform.localPosition = Vector3.zero;
        PitchNode.transform.localRotation = Quaternion.identity;

        RollNode.transform.localPosition = Vector3.zero;
        RollNode.transform.localRotation = Quaternion.identity;
    }
    private void HandleEdgeScrolling()
    {
        if (!isEdgeScrollingEnabled) return;
        Vector2 mousePos = Mouse.current.position.ReadValue();
#if UNITY_EDITOR
        if (mousePos.x < 0 || mousePos.x > Screen.width || mousePos.y < 0 || mousePos.y > Screen.height)
            return;
#endif

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
        if (!isMiddleMouseDragEnabled) return;
        if (ReplayMgr.inst.isReplaying) return;
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
