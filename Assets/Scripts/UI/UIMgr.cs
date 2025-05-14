﻿using System;
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

    private InputAction mouseDelta;
    private InputAction mouseScroll;
    private InputAction toggleMap;

    private InputAction selectionBox;
    private InputAction singleSelect;
    private InputAction selectionCursorPosition;
    private InputAction selectNextEntity;
    private InputAction addSelection;

    private InputAction command;
    private InputAction intercept;
    private InputAction attackMove;
    private InputAction addCommand;

    private InputAction changeSpeed;
    private InputAction changeHeading;

    private InputAction create100;

    private InputAction selectAll;
    private InputAction selectAllDDG51;
    private InputAction selectAllSEAHUNTER;
    private InputAction selectAllJARIUSV;
    private InputAction selectGroup1;


    // private InputAction attack1;
    private InputAction attack2;
    private InputAction attack3;
    private InputAction attack4;
    private InputAction modifiers;

    private void Awake()
    {
        inst = this;
        inputs = new GameInputs();
    }

    private void OnEnable() {
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


        //moves camera or minimap - bound to middle mouse + mouse move
        mouseDelta = inputs.Camera.MiddleMouseMove;
        mouseDelta.Enable();

        //changes cam height and zooms in cam - bound to mouse scroll
        mouseScroll = inputs.Camera.MouseScroll;
        mouseScroll.Enable();

        //toggles whether map is mini or big - bound to M
        toggleMap = inputs.Camera.Map;
        toggleMap.Enable();
        toggleMap.performed += ToggleMap;


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

        attackMove = inputs.Attacks.Attack1;
        attackMove.Enable();
        

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

        selectAllDDG51 = inputs.Selection.SelectAllDDG51;
        selectAllDDG51.Enable();
        selectAllDDG51.performed += OnSelectAllDDG51Performed;

        selectAllJARIUSV = inputs.Selection.SelectAllJARIUSV;
        selectAllJARIUSV.Enable();
        selectAllJARIUSV.performed += OnSelectAllJARIUSVPerformed;

        selectAllSEAHUNTER = inputs.Selection.SelectAllSEAHUNTER;
        selectAllSEAHUNTER.Enable();
        selectAllSEAHUNTER.performed += OnSelectAllSEAHUNTERPerformed;
        //Ctrl key signifies group commands so when we do right mouse button we run only if ctrl is not pressed
        inputs.Entities.ControlKey.Enable();


        

        attack2 = inputs.Attacks.Attack2;
        attack2.Enable();
        attack2.performed += Attack2;

        attack3 = inputs.Attacks.Attack3;
        attack3.Enable();
        attack3.performed += Attack3;

        attack4 = inputs.Attacks.Attack4;
        attack4.Enable();
        attack4.performed += Attack4;

        modifiers = inputs.Attacks.Modifers;
        modifiers.Enable();
    }
    private void OnDisable()
    {
        toggleRTSCam.Disable();
        yawCamera.Disable();
        pitchCamera.Disable();
        cameraYMove.Disable();
        cameraXZMove.Disable();
        //
        mouseDelta.Disable();
        mouseScroll.Disable();
        toggleMap.Disable();
        //
        selectionBox.Disable();
        singleSelect.Disable();
        selectionCursorPosition.Disable();
        selectNextEntity.Disable();
        addSelection.Disable();
        command.Disable();
        intercept.Disable();
        attackMove.Disable();
        addCommand.Disable();
        changeSpeed.Disable();
        changeHeading.Disable();
        create100.Disable();

        selectAll.Disable();
        selectAllDDG51.Disable();
        selectAllJARIUSV.Disable();
        selectAllSEAHUNTER.Disable();
        inputs.Entities.ControlKey.Disable();

        // attack1.Disable();
        attack2.Disable();
        attack3.Disable();
        attack4.Disable();
        modifiers.Disable();
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
    
    public Slider healthSlider;

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

    [SerializeField]
    private Image healthImage;
    [SerializeField]
    private TextMeshProUGUI healthText;

    private string lastEntityName = "";
    private float lastSpeed = -1;
    private float lastDesiredSpeed = -1;
    private float lastHeading = -1;
    private float lastDesiredHeading = -1;
    private float lastHealth = -1;
    private float lastFuel = -1;
    private float lastRange = -1;
    private float lastAltitude = -1;
    // private float lastDesiredAltitude = -1;


    // Update is called once per frame
    void Update()
    {
        if (SelectionMgr.inst.selectedEntity != null)
        {
            Entity ent = SelectionMgr.inst.selectedEntity;
            if (ent.name != lastEntityName)
            {
                lastEntityName = ent.name;
                entityName.text = ent.name;
            }
            if (ent.speed != lastSpeed)
            {
                lastSpeed = ent.speed;
                speed.text = ent.speed.ToString("F2") + " m/s";
            }
            if (ent.desiredSpeed != lastDesiredSpeed)
            {
                lastDesiredSpeed = ent.desiredSpeed;
                desiredSpeed.text = ent.desiredSpeed.ToString("F2") + " m/s";
            }
            if (ent.heading != lastHeading)
            {
                lastHeading = ent.heading;
                heading.text = ent.heading.ToString("F1") + " deg";
            }
            if (ent.desiredHeading != lastDesiredHeading)
            {
                lastDesiredHeading = ent.desiredHeading;
                desiredHeading.text = ent.desiredHeading.ToString("F1") + " deg";
            }

            DisplayAIInformation(ent);
            UpdateHealth(ent);

            Oriented3dPhysics phx3d = ent.GetComponentInChildren<Oriented3dPhysics>();
            if(phx3d != null)  {
                if(phx3d.altitude != lastAltitude) {
                    lastAltitude = phx3d.altitude;
                    altitude.text = phx3d.altitude.ToString("F2") + "m";
                }
            }
            if(lastFuel != ent.fuel) {
                lastFuel = ent.fuel;
                fuel.text = ent.fuel.ToString("F0");
            }
            if(lastRange != ent.range) {
                lastRange = ent.range;
                range.text = (ent.range * Utils.ToNautialMiles).ToString("F1") + " nm";
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

        if (boxSelecting)
            SelectionMgr.inst.UpdateSelectionBox(selectionCursorPosition.ReadValue<Vector2>());


        if(singleSelect.IsPressed())
            // MinimapMgr.inst.MoveCameraViaMinimap(selectionCursorPosition.ReadValue<Vector2>());

        if(MinimapMgr.inst.CursorOverMap(selectionCursorPosition.ReadValue<Vector2>())) {
            MinimapMgr.inst.ChangeZoom(mouseScroll.ReadValue<Vector2>().y);
            // MinimapMgr.inst.ChangeCenter(mouseDelta.ReadValue<Vector2>());
        } else {
            CameraMgr.inst.MoveCameraY(mouseScroll.ReadValue<Vector2>().y);
            CameraMgr.inst.MoveCameraXZ(mouseDelta.ReadValue<Vector2>());
        }


    }

    private float greenHealth = 67;
    private float orangeHealth = 33;
    private void UpdateHealth(Entity ent) {
        if(ent.health == lastHealth)
            return;
        
        lastHealth = ent.health;
        float health = 100f * ent.health / ent.maxHealth;
        //text
        healthText.text = health.ToString("000");
        //fill
        healthImage.fillAmount = health/100;
        //Color
        if(health >= greenHealth)
            healthImage.color = Color.green;
        else if (health > orangeHealth && health < greenHealth)
            healthImage.color = ColorPalette.inst.colors[15];
        else
            healthImage.color = ColorPalette.inst.colors[17];
    }

    private float lastTimeOnTarget = -1;
    private float lastTargetRange = -1;
    private Vector3 lastTarget = Vector3.positiveInfinity;
    private string lastTargetName = "";

    private void DisplayAIInformation(Entity ent) {
        UnitAI uai = ent.GetComponentInChildren<UnitAI>();
        if(uai.commands.Count > 0) {
            Move move = uai.commands.Peek() as Move;
            if(lastTimeOnTarget == move.timeOnTarget && lastTargetRange == move.range && lastTarget == move.movePosition)
                return;
            lastTimeOnTarget = move.timeOnTarget;
            lastTargetRange = move.range;
            lastTarget = move.movePosition;
            timeOnTarget.text = move.timeOnTarget.ToString("F2") + "sec";
            targetRange.text = move.range.ToString("F2") + "m";
            target.text = move.movePosition.ToString();

            Follow follow = uai.commands.Peek() as Follow;

            if(follow != null){


                if(follow.targetEntity != null){
                    if(follow.targetEntity.name != lastTargetName){
                        lastTargetName = follow.targetEntity.name;
                        target.text = follow.targetEntity.name;
                    }
                }
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
        
        if (!inputs.Entities.ControlKey.IsPressed() &&
        !Keyboard.current.oKey.isPressed &&
        !Keyboard.current.pKey.isPressed &&
        !Keyboard.current.iKey.isPressed &&
        !Keyboard.current.uKey.isPressed)
        {
            AIMgr.inst.HandleCommand(selectionCursorPosition.ReadValue<Vector2>(), intercept.IsPressed(),attackMove.IsPressed(), addCommand.IsPressed());
        }
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


    private void ToggleMap(InputAction.CallbackContext context) {
        // MinimapMgr.inst.ResizeMap();
    }


    private void OnSelectAllPerformed(InputAction.CallbackContext context) {
        SelectionMgr.inst.SelectAll();
    }
    private void OnSelectAllDDG51Performed(InputAction.CallbackContext context) {
        SelectionMgr.inst.SelectAllDDG51();
    }
    
    private void OnSelectAllJARIUSVPerformed(InputAction.CallbackContext context) {
        SelectionMgr.inst.SelectALLJARIUSV();
    }
    private void OnSelectAllSEAHUNTERPerformed(InputAction.CallbackContext context) {
        SelectionMgr.inst.SelectALLSEAHUNTER();
    }

    public WorldPosEntity MousePosToWorldPosEntity(Vector2 mousePos)
    {
        RaycastHit hit = new RaycastHit();
        int layerMask = 512; //Ocean layer = 9, 2^9 = 512
        if (Physics.Raycast(Camera.main.ScreenPointToRay(mousePos), out hit, float.MaxValue, layerMask))
        {
            //Debug.DrawLine(Camera.main.transform.position, hit.point, Color.yellow, 2); //for debugging
            Vector3 pos = hit.point;
            pos.y = 0;
            Entity ent = FindClosestEntInRadius(pos);//, rClickRadiusSq);//
            WorldPosEntity wpe = new WorldPosEntity(pos, ent);
            return wpe;
        }
        return null;
    }

    public const float rClickRadiusSq = 100;

    public Entity FindClosestEntInRadius(Vector3 position, float radius = rClickRadiusSq)
    {
        Collider[] hitColliders = Physics.OverlapSphere(position, radius);
        Entity closestEntity = null;
        float minDistanceSqr = float.MaxValue;

        foreach (var hitCollider in hitColliders)
        {
            Entity entity = hitCollider.GetComponentInParent<Entity>(); // Or GetComponent<Entity>() if Entity is on the same GameObject as the collider
            if (entity != null)
            {
                float distanceSqr = (entity.transform.position - position).sqrMagnitude;
                if (distanceSqr < minDistanceSqr)
                {
                    minDistanceSqr = distanceSqr;
                    closestEntity = entity;
                }
            }
        }
        return closestEntity;
    }

    public void ActivateEntityCommands(bool shouldActivate) {
        if(shouldActivate)
            inputs.Entities.Enable();
        else
            inputs.Entities.Disable();
    }


    private void Attack1(InputAction.CallbackContext context)
    {
        WeaponsMgr.inst.handleWeapon(selectionCursorPosition.ReadValue<Vector2>());
    }

    private void Attack2(InputAction.CallbackContext context)
    {
        Debug.Log("Surface Weapon");
        // WeaponsMgr.inst.handleWeapon(selectionCursorPosition.ReadValue<Vector2>(), WeaponBehaviors.SurfaceInterceptor);
    }

    private void Attack3(InputAction.CallbackContext context)
    {
        Debug.Log("Air Weapon");
        // WeaponsMgr.inst.handleWeapon(selectionCursorPosition.ReadValue<Vector2>(), WeaponBehaviors.AirInterceptor);
    }

    private void Attack4(InputAction.CallbackContext context)
    {
        Debug.Log("Smart Weapon");
        // WeaponsMgr.inst.handleWeapon(selectionCursorPosition.ReadValue<Vector2>(), WeaponBehaviors.Smart);

    }
}