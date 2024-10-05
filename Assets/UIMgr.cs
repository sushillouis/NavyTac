using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Windows;

public class UIMgr : MonoBehaviour
{
    public static UIMgr inst;
    public GameObject ToggleMultiSelect;
    public bool isActive;

    public GameInputs inputs;

    private InputAction yawCamera;
    private InputAction pitchCamera;
    private InputAction cameraYMove;
    private InputAction cameraXZMove;
    private InputAction toggleRTSCam;

    private InputAction selectionCursor;
    private InputAction selectionCursorPosition;
    private InputAction selectNextEntity;
    private InputAction clearSelection;

    private InputAction command;
    private InputAction intercept;
    private InputAction addCommand;

    private InputAction changeSpeed;
    private InputAction changeHeading;

    private InputAction create100;

    private void Awake()
    {
        inst = this;
        inputs = new GameInputs();
    }

    private void OnEnable()
    {
        toggleRTSCam = inputs.Camera.RTSView;
        toggleRTSCam.Enable();
        toggleRTSCam.performed += ToggleRTSView;

        yawCamera = inputs.Camera.Yaw;
        yawCamera.Enable();

        pitchCamera = inputs.Camera.Pitch;
        pitchCamera.Enable();

        cameraYMove = inputs.Camera.YMove;
        cameraYMove.Enable();

        cameraXZMove = inputs.Camera.XZMove;
        cameraXZMove.Enable();

        selectionCursor = inputs.Selection.Cursor;
        selectionCursor.Enable();
        selectionCursor.started += OnCursorStarted;
        selectionCursor.canceled += OnCursorCanceled;

        selectionCursorPosition = inputs.Selection.CursorPosition;
        selectionCursorPosition.Enable();

        selectNextEntity = inputs.Selection.NextEntity;
        selectNextEntity.Enable();
        selectNextEntity.performed += SelectNextEntity;

        clearSelection = inputs.Selection.ClearSelection;
        clearSelection.Enable();

        command = inputs.Entities.Command;
        command.Enable();
        command.performed += HandleCommand;

        intercept = inputs.Entities.Intercept;
        intercept.Enable();

        addCommand = inputs.Entities.AddCommand;
        addCommand.Enable();

        changeSpeed = inputs.Entities.Speed;
        changeSpeed.Enable();
        changeSpeed.performed += ChangeSpeed;

        changeHeading = inputs.Entities.Heading;
        changeHeading.Enable();
        changeHeading.performed += ChangeHeading;

        create100 = inputs.Entities.Create100;
        create100.Enable();
        create100.performed += Create100;
    }

    private void OnDisable()
    {
        toggleRTSCam.Disable();
        yawCamera.Disable();
        pitchCamera.Disable();
        cameraYMove.Disable();
        cameraXZMove.Disable();
        selectionCursor.Disable();
        selectionCursorPosition.Disable();
        selectNextEntity.Disable();
        clearSelection.Disable();
        command.Disable();
        intercept.Disable();
        addCommand.Disable();
        changeSpeed.Disable();
        changeHeading.Disable();
        create100.Disable();
    }

    // Start is called before the first frame update
    void Start()
    {
        ToggleMultiSelect.SetActive(false);
        #if UNITY_ANDROID
            ToggleMultiSelect.SetActive(true);
        #endif
        #if UNITY_ANDROID
            ToggleMultiSelect.SetActive(true);
        #endif
    }
    public Text entityName;
    public Text speed;
    public Text desiredSpeed;
    public Text heading;
    public Text desiredHeading;

    // Update is called once per frame
    void Update()
    {
        if(SelectionMgr.inst.selectedEntity != null) {
            Entity ent = SelectionMgr.inst.selectedEntity;
            entityName.text = ent.gameObject.name;
            speed.text = ent.speed.ToString("F2") + " m/s";
            desiredSpeed.text = ent.desiredSpeed.ToString("F2") + " m/s";
            heading.text = ent.heading.ToString("F1") + " deg";
            desiredHeading.text = ent.desiredHeading.ToString("F1") + " deg";
        }

        if (ToggleMultiSelect.activeSelf)
            isActive = ToggleMultiSelect.GetComponent<Toggle>().isOn;
        else
            isActive = false;

        CameraMgr.inst.YawCamera(yawCamera.ReadValue<float>());
        CameraMgr.inst.YawCamera(pitchCamera.ReadValue<float>());
        CameraMgr.inst.MoveCameraY(cameraYMove.ReadValue<Vector2>().y);
        CameraMgr.inst.MoveCameraXZ(cameraXZMove.ReadValue<Vector2>());

        if(selectionCursor.IsPressed())
            SelectionMgr.inst.UpdateSelectionBox(selectionCursorPosition.ReadValue<Vector2>());
    }

    private void ToggleRTSView(InputAction.CallbackContext context)
    {
        CameraMgr.inst.ToggleRTSView();
    }

    private void OnCursorStarted(InputAction.CallbackContext context)
    {
        SelectionMgr.inst.StartBoxSelecting();
    }

    private void OnCursorCanceled(InputAction.CallbackContext context)
    {
        SelectionMgr.inst.EndBoxSelecting();
    }

    private void SelectNextEntity(InputAction.CallbackContext context)
    {
        SelectionMgr.inst.SelectNextEntity(clearSelection.IsPressed());
    }

    private void HandleCommand(InputAction.CallbackContext context)
    {
        AIMgr.inst.HandleCommand(selectionCursorPosition.ReadValue<Vector2>(), intercept.IsPressed(), addCommand.IsPressed());
    }

    private void ChangeSpeed(InputAction.CallbackContext context) 
    {
        ControlMgr.inst.ChangeSpeed(changeSpeed.ReadValue<float>());
    }

    private void ChangeHeading(InputAction.CallbackContext context)
    {
        ControlMgr.inst.ChangeHeading(changeHeading.ReadValue<float>());
    }

    private void Create100(InputAction.CallbackContext context)
    {
        GameMgr.inst.Create100();
    }
}
