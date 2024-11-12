using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Windows;

/// <summary>
/// Key and mouse bindings are all in this document: _______________
/// </summary>
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

    private InputAction selectionBox;
    private InputAction singleSelect;
    private InputAction selectionCursorPosition;
    private InputAction selectNextEntity;
    private InputAction addSelection;

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
        //changes view from RTSCameraRig to entity CameraRig - bound to C
        toggleRTSCam = inputs.Camera.RTSView;
        toggleRTSCam.Enable();
        toggleRTSCam.performed += ToggleRTSView;

        //yaws camera - bound to Q and E
        yawCamera = inputs.Camera.Yaw;
        yawCamera.Enable();

        //pitches camera - bound to Z and X
        pitchCamera = inputs.Camera.Pitch;
        pitchCamera.Enable();

        //moves camera up and down - bound to R and F, Scroll Wheel, and Numpad + and -
        cameraYMove = inputs.Camera.YMove;
        cameraYMove.Enable();

        //moves camera forward, backward, left, and right - bound to WASD, Arrow Keys, and Middle Mouse + Moving Mouse
        cameraXZMove = inputs.Camera.XZMove;
        cameraXZMove.Enable();

        //handles box selection - bound to Left Click with a hold
        selectionBox = inputs.Selection.BoxSelect;
        selectionBox.Enable();
        selectionBox.started += OnBoxSelectPerformed;
        selectionBox.canceled += OnBoxSelectCanceled;

        //handles single click selection - bound to Left Click with a tap
        singleSelect = inputs.Selection.SingleSelect;
        singleSelect.Enable();
        singleSelect.performed += OnSingleSelectPerformed;

        //determines where the cursor is - bound to Mouse Screen Position
        selectionCursorPosition = inputs.Selection.CursorPosition;
        selectionCursorPosition.Enable();

        //selects the next entity in the entity list - bound to Tab
        selectNextEntity = inputs.Selection.NextEntity;
        selectNextEntity.Enable();
        selectNextEntity.performed += SelectNextEntity;

        //lets entities be added to already selected entities when pressed - bound to Shift
        addSelection = inputs.Selection.ClearSelection;
        addSelection.Enable();

        //handles inputing new commands - bound to Right Click
        command = inputs.Entities.Command;
        command.Enable();
        command.performed += HandleCommand;

        //when held down and a follow is input, that command will be an intercept - bound to Ctrl
        intercept = inputs.Entities.Intercept;
        intercept.Enable();

        //when held down, commands are added, not cleared - bound to Shift
        addCommand = inputs.Entities.AddCommand;
        addCommand.Enable();

        //increases/decreases selected entity speed - bound to Up/Down Arrows
        changeSpeed = inputs.Entities.Speed;
        changeSpeed.Enable();
        changeSpeed.performed += ChangeSpeed;

        //increases/decreases selected entity heading - bound to Right/Left Arrows
        changeHeading = inputs.Entities.Heading;
        changeHeading.Enable();
        changeHeading.performed += ChangeHeading;

        //spawns 100 entities in the scene - bound to F12
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
        selectionBox.Disable();
        singleSelect.Disable();
        selectionCursorPosition.Disable();
        selectNextEntity.Disable();
        addSelection.Disable();
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
    public TextMeshProUGUI entityName;
    public TextMeshProUGUI speed;
    public TextMeshProUGUI desiredSpeed;
    public TextMeshProUGUI heading;
    public TextMeshProUGUI desiredHeading;

    public TextMeshProUGUI altitude;
    public TextMeshProUGUI desiredAltitude;

    public TextMeshProUGUI target;
    public TextMeshProUGUI timeOnTarget;
    public TextMeshProUGUI targetRange;

    // Update is called once per frame
    void Update()
    {
        if(SelectionMgr.inst.selectedEntity != null) {
            Entity ent = SelectionMgr.inst.selectedEntity;
            entityName.text = ent.name;
            speed.text = ent.speed.ToString("F2") + " m/s";
            desiredSpeed.text = ent.desiredSpeed.ToString("F2") + " m/s";
            heading.text = ent.heading.ToString("F1") + " deg";
            desiredHeading.text = ent.desiredHeading.ToString("F1") + " deg";

            DisplayAIInformation(ent);

            Oriented3dPhysics phx3d = ent.GetComponentInChildren<Oriented3dPhysics>();
            if(phx3d != null)  {
                altitude.text = phx3d.altitude.ToString("F2") + "m";
                desiredAltitude.text = phx3d.desiredAltitude.ToString("F2") + "m";
            }


        }

        if (ToggleMultiSelect.activeSelf)
            isActive = ToggleMultiSelect.GetComponent<Toggle>().isOn;
        else
            isActive = false;

        CameraMgr.inst.YawCamera(yawCamera.ReadValue<float>());
        CameraMgr.inst.PitchCamera(pitchCamera.ReadValue<float>());
        CameraMgr.inst.MoveCameraY(cameraYMove.ReadValue<Vector2>().y);
        CameraMgr.inst.MoveCameraXZ(cameraXZMove.ReadValue<Vector2>());

        if(boxSelecting)
            SelectionMgr.inst.UpdateSelectionBox(selectionCursorPosition.ReadValue<Vector2>());
    }
    
    private void DisplayAIInformation(Entity ent) {
        UnitAI uai = ent.GetComponentInChildren<UnitAI>();
        if(uai.commands.Count > 0) {
            Move move = uai.commands[0] as Move;
            timeOnTarget.text = move.timeOnTarget.ToString("F2") + "sec";
            targetRange.text = move.range.ToString("F2") + "m";
            target.text = move.movePosition.ToString();

            Follow follow = uai.commands[0] as Follow;
            if(follow != null){
                target.text = follow.targetEntity.name;
            } 

        }


    }


    private void ToggleRTSView(InputAction.CallbackContext context)
    {
        CameraMgr.inst.ToggleRTSView();
    }

    bool boxSelecting;
    private void OnBoxSelectPerformed(InputAction.CallbackContext context)
    {
        SelectionMgr.inst.StartBoxSelecting();
        boxSelecting = true;
    }

    private void OnBoxSelectCanceled(InputAction.CallbackContext context)
    {
        SelectionMgr.inst.EndBoxSelecting();
        boxSelecting = false;
    }

    private void OnSingleSelectPerformed(InputAction.CallbackContext context)
    {
        SelectionMgr.inst.SelectEntity(selectionCursorPosition.ReadValue<Vector2>(), !addSelection.IsPressed());
    }

    private void SelectNextEntity(InputAction.CallbackContext context)
    {
        SelectionMgr.inst.SelectNextEntity(addSelection.IsPressed());
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
