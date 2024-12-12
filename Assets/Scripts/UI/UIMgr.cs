using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Windows;

[Serializable]
public class WorldPosEntity
{
    public Vector3 worldPosition;
    public Entity entity;

    public WorldPosEntity(Vector3 wp, Entity ent) {
        worldPosition = wp; 
        entity = ent;
    }

    public override string ToString() {
        return "Pos: " + worldPosition.ToString() + ", E: " + entity?.ToString();
    }
}

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

    private InputAction selectAll;
    private InputAction selectGroup1;


    private void Awake()
    {
        inst = this;
        inputs = new GameInputs();
    }

    private void OnEnable()
    {
        inputs.Enable();

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

        selectionBox = inputs.Selection.BoxSelect;
        selectionBox.Enable();
        selectionBox.started += OnBoxSelectPerformed;
        selectionBox.canceled += OnBoxSelectCanceled;

        singleSelect = inputs.Selection.SingleSelect;
        singleSelect.Enable();
        singleSelect.performed += OnSingleSelectPerformed;

        selectionCursorPosition = inputs.Selection.CursorPosition;
        selectionCursorPosition.Enable();

        selectNextEntity = inputs.Selection.NextEntity;
        selectNextEntity.Enable();
        selectNextEntity.performed += SelectNextEntity;

        addSelection = inputs.Selection.ClearSelection;
        addSelection.Enable();

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

        //Groups

        selectAll = inputs.Selection.SelectAll;
        selectAll.Enable();
        selectAll.performed += OnSelectAllPerformed;

        //selectGroup1 = inputs.Selection.SelectGroup1;
        //selectGroup1.Enable();
        //selectGroup1.performed += OnGroupSelectPerformed;

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

        selectAll.Disable();
        //selectGroup1.Disable();


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

    public TextMeshProUGUI fuel;
    public TextMeshProUGUI range;

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

            fuel.text = ent.fuel.ToString("F0");
            range.text = (ent.range * Utils.ToNautialMiles).ToString("F1") + " nm";


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
        //SelectionMgr.inst.SelectEntity(selectionCursorPosition.ReadValue<Vector2>(), !addSelection.IsPressed());
        SelectionMgr.inst.SelectEntity2(selectionCursorPosition.ReadValue<Vector2>(), addSelection.IsPressed());
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

    private void OnSelectAllPerformed(InputAction.CallbackContext context) {
        SelectionMgr.inst.SelectAll();
    }


    public WorldPosEntity MousePosToWorldPosEntity(Vector2 mousePos) {
        RaycastHit hit = new RaycastHit();
        int layerMask = 512; //Ocean layer = 9, 2^9 = 512
        if(Physics.Raycast(Camera.main.ScreenPointToRay(mousePos), out hit, float.MaxValue, layerMask)) {
            //Debug.DrawLine(Camera.main.transform.position, hit.point, Color.yellow, 2); //for debugging
            Vector3 pos = hit.point;
            pos.y = 0;
            Entity ent = FindClosestEntInRadius(pos);//, rClickRadiusSq);//
            WorldPosEntity wpe = new WorldPosEntity(pos, ent);
            return wpe;
        }
        return null;
    }

    public const float rClickRadiusSq = 10000;
    public Entity FindClosestEntInRadius(Vector3 point, float rsq = rClickRadiusSq) {
        Entity minEnt = null;
        float min = float.MaxValue;
        foreach(Entity ent in EntityMgr.inst.entities) {
            float distanceSq = (ent.transform.position - point).sqrMagnitude;
            if(distanceSq < rsq) {
                if(distanceSq < min) {
                    minEnt = ent;
                    min = distanceSq;
                }
            }
        }
        return minEnt;
    }

    public void ActivateEntityCommands(bool shouldActivate) {
        if(shouldActivate)
            inputs.Entities.Enable();
        else
            inputs.Entities.Disable();
    }

}
