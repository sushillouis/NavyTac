using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GroupUIMgr : MonoBehaviour
{
    public static GroupUIMgr inst;

    private GroupInputs groupInputs;

    [SerializeField]
    private TMP_Dropdown tacDropdown;
    [SerializeField]
    private RectTransform TacDropdownPanel;
    [SerializeField]
    private RectTransform mainCanvas;

    [SerializeField]
    private PanelPlus TacticalCommandsPanel;

    [SerializeField]
    private List<Button> tacticsButtonsList = new List<Button>();


    private void Awake() {
        inst = this;
    }

    void Start()
    {
        groupInputs = new GroupInputs();

        UIMapControlGroups();

        groupInputs.Tactical.ShowTactics.Enable();
        groupInputs.Tactical.ShowTactics.performed += HandleTacticalCommand2;
        groupInputs.Tactical.CursorPosition.Enable();

        /*
        TacDropdownPanel.gameObject.SetActive(false);
        tacDropdown.onValueChanged.AddListener(HandleDropdown);
        InitDropdown();
        */
        InitGroupCommandButtons();
        //-----------------------------------------------------------------------------
        
    }


    private void OnDisable() {

        UIMapDisable();
        groupInputs.Tactical.ShowTactics.Disable();
    }

    void Update()
    {

    }

    public void Bind(InputAction.CallbackContext ctx) {
        int groupNumber = ParseContextForControlGroupNumber(ctx.control.path);
        NetDebugConsole.inst.Log("Binding..." + ctx.control + " : " + groupNumber);
        SelectionMgr.inst.FormControlGroup(groupNumber);
    }

    public void Retreive(InputAction.CallbackContext context) {
        int groupNumber = ParseContextForControlGroupNumber(context.control.path);
        NetDebugConsole.inst.Log("Retreiving..." + context.control + " : " + groupNumber);
        SelectionMgr.inst.SelectControlGroup(groupNumber);
    }
    int ParseContextForControlGroupNumber(string path) {
        string[] pathElements = path.Split('/');
        string keycode = pathElements[pathElements.Length - 1];
        return int.Parse(keycode);
    }

    void CancelEntCommands(List<Entity> entities) {
        foreach(Entity ent in entities) {
            ent.ai.StopAndRemoveAllCommands();
        }
    }

    void HandleTacticalCommand(InputAction.CallbackContext context) {
        //Vector2 mousePos = Mouse.current.position.value;
        if(SelectionMgr.inst.selectedEntities.Count > 1) { // a group is more than 1
            //Cancel single ent commands brought on by right click
            CancelEntCommands(SelectionMgr.inst.selectedEntities);
            UIMgr.inst.ActivateEntityCommands(false);

            Vector2 mousePos = groupInputs.Tactical.CursorPosition.ReadValue<Vector2>();
            //Debug.Log("ctx: " + context);  Debug.Log("mpos: " + mousePos);
            worldPosAndEntity = UIMgr.inst.MousePosToWorldPosEntity(mousePos);
            //Debug.Log(worldPosAndEntity);

            Vector2 localPoint = new Vector2(0, 0);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(mainCanvas, mousePos, null, out localPoint);
            TacDropdownPanel.localPosition = localPoint;
            TacDropdownPanel.gameObject.SetActive(true);

            currentGroup = TacticalAIMgr.inst.CreateGroup(SelectionMgr.inst.selectedEntities);

            //tacDropdown.SetValueWithoutNotify((int) TacticsType.Choose);
        }
    }


    void HandleTacticalCommand2(InputAction.CallbackContext context) {
        if(SelectionMgr.inst.selectedEntities.Count > 1) { // a group is more than 1
            Vector2 mousePos = groupInputs.Tactical.CursorPosition.ReadValue<Vector2>();
            worldPosAndEntity = UIMgr.inst.MousePosToWorldPosEntity(mousePos);
            currentGroup = TacticalAIMgr.inst.CreateGroup(SelectionMgr.inst.selectedEntities);
            TacticalCommandsPanel.isVisible = true; //becomes invisible after 5 secs
        }
    }

    public WorldPosEntity worldPosAndEntity;
    public Group currentGroup;


    void HandleButton(TacticsType tt) {
        //TacticsType tt = (TacticsType) i;
        Debug.Log("handling button: " + tt);

        switch(tt) {
            case TacticsType.EscortMove:
                if(worldPosAndEntity != null)
                    currentGroup.CreateExecuteEscortMove(worldPosAndEntity.worldPosition);
                break;
            case TacticsType.Scout:
            case TacticsType.AtkDistract:
            case TacticsType.Pincer:
            case TacticsType.AtkMove:
            case TacticsType.Defend:
                Debug.Log("Not implemented yet");
                break;
            case TacticsType.Cancel:
                CancelEntCommands(currentGroup.groupEntities);
                break;
            default:
                Debug.Log("None: Not implemented yet");
                break;
        }
        TacticalCommandsPanel.isVisible = false;
    }

    void InitGroupCommandButtons() {

        foreach(TacticsType tt in Enum.GetValues(typeof(TacticsType))) {
            if(tt != TacticsType.None) {
                //Debug.Log("Button: " + tt);
                Button tb = tacticsButtonsList[(int) tt];
                tb.GetComponentInChildren<TextMeshProUGUI>().text = Utils.SplitCamelCase(tt.ToString());
                tacticsButtonsList[(int) tt].onClick.AddListener(() => HandleButton(tt));
            }
        }
    }

    void UIMapDisable() {

        groupInputs.BindGroup.Disable();

        groupInputs.BindGroup.BindControlGroup1.Disable();
        groupInputs.BindGroup.RetreiveControlGroup1.Disable();

        groupInputs.BindGroup.BindControlGroup2.Disable();
        groupInputs.BindGroup.RetreiveControlGroup2.Disable();

        groupInputs.BindGroup.BindControlGroup3.Disable();
        groupInputs.BindGroup.RetreiveControlGroup3.Disable();

        groupInputs.BindGroup.BindControlGroup4.Disable();
        groupInputs.BindGroup.RetreiveControlGroup4.Disable();

        groupInputs.BindGroup.BindControlGroup5.Disable();
        groupInputs.BindGroup.RetreiveControlGroup5.Disable();

        groupInputs.BindGroup.BindControlGroup6.Disable();
        groupInputs.BindGroup.RetreiveControlGroup6.Disable();

        groupInputs.BindGroup.BindControlGroup7.Disable();
        groupInputs.BindGroup.RetreiveControlGroup7.Disable();

        groupInputs.BindGroup.BindControlGroup8.Disable();
        groupInputs.BindGroup.RetreiveControlGroup8.Disable();

        groupInputs.BindGroup.BindControlGroup9.Disable();
        groupInputs.BindGroup.RetreiveControlGroup9.Disable();

        //------------------------------------------------
        groupInputs.BindGroup.BindControlGroup10.Disable();
        groupInputs.BindGroup.RetreiveControlGroup10.Disable();
    }

    void UIMapControlGroups() {
        groupInputs.Enable();
        groupInputs.BindGroup.Enable();
        //-----------------------------------------------------------------------------
        groupInputs.BindGroup.BindControlGroup1.Enable();
        groupInputs.BindGroup.RetreiveControlGroup1.Enable();

        groupInputs.BindGroup.BindControlGroup2.Enable();
        groupInputs.BindGroup.RetreiveControlGroup2.Enable();

        groupInputs.BindGroup.BindControlGroup3.Enable();
        groupInputs.BindGroup.RetreiveControlGroup3.Enable();

        groupInputs.BindGroup.BindControlGroup4.Enable();
        groupInputs.BindGroup.RetreiveControlGroup4.Enable();

        groupInputs.BindGroup.BindControlGroup5.Enable();
        groupInputs.BindGroup.RetreiveControlGroup5.Enable();

        groupInputs.BindGroup.BindControlGroup6.Enable();
        groupInputs.BindGroup.RetreiveControlGroup6.Enable();

        groupInputs.BindGroup.BindControlGroup7.Enable();
        groupInputs.BindGroup.RetreiveControlGroup7.Enable();

        groupInputs.BindGroup.BindControlGroup8.Enable();
        groupInputs.BindGroup.RetreiveControlGroup8.Enable();

        groupInputs.BindGroup.BindControlGroup9.Enable();
        groupInputs.BindGroup.RetreiveControlGroup9.Enable();

        //Key Alpha 0
        groupInputs.BindGroup.BindControlGroup10.Enable();
        groupInputs.BindGroup.RetreiveControlGroup10.Enable();

        //-----------------------------------------------------------------------------

        groupInputs.BindGroup.BindControlGroup1.performed += Bind;
        groupInputs.BindGroup.RetreiveControlGroup1.performed += Retreive;

        groupInputs.BindGroup.BindControlGroup2.performed += Bind;
        groupInputs.BindGroup.RetreiveControlGroup2.performed += Retreive;

        groupInputs.BindGroup.BindControlGroup3.performed += Bind;
        groupInputs.BindGroup.RetreiveControlGroup3.performed += Retreive;

        groupInputs.BindGroup.BindControlGroup4.performed += Bind;
        groupInputs.BindGroup.RetreiveControlGroup4.performed += Retreive;

        groupInputs.BindGroup.BindControlGroup5.performed += Bind;
        groupInputs.BindGroup.RetreiveControlGroup5.performed += Retreive;

        groupInputs.BindGroup.BindControlGroup6.performed += Bind;
        groupInputs.BindGroup.RetreiveControlGroup6.performed += Retreive;

        groupInputs.BindGroup.BindControlGroup7.performed += Bind;
        groupInputs.BindGroup.RetreiveControlGroup7.performed += Retreive;

        groupInputs.BindGroup.BindControlGroup8.performed += Bind;
        groupInputs.BindGroup.RetreiveControlGroup8.performed += Retreive;

        groupInputs.BindGroup.BindControlGroup9.performed += Bind;
        groupInputs.BindGroup.RetreiveControlGroup9.performed += Retreive;

        groupInputs.BindGroup.BindControlGroup10.performed += Bind;
        groupInputs.BindGroup.RetreiveControlGroup10.performed += Retreive;

    }


}


/*
 * 
 * 
    public void HandleDropdown(int val) {
        TacDropdownPanel.gameObject.SetActive(false);
        TacticsType tt = (TacticsType) val;
        switch(tt) {
            //case TacticsType.Cancel:
            //    break;
            case TacticsType.GroupMove:
                if(worldPosAndEntity != null)
                    currentGroup.CreateExecuteFormMove(worldPosAndEntity.worldPosition);
                break;
            case TacticsType.Scout:
            case TacticsType.AtkDistract:
            case TacticsType.Pincer:
            case TacticsType.AtkMove:
                Debug.Log("Not implemented yet");
                break;
            default:
                break;
        }

        //Dropdown choosing kills selection because of interaction with unity input, so 
        SelectionMgr.inst.ClearSelection();
        foreach(Entity ent in currentGroup.entities) {
            SelectionMgr.inst.SelectEntity(ent, false);
        }
        UIMgr.inst.ActivateEntityCommands(true);
    }

    public void OnMouseUp() {
        TacDropdownPanel.gameObject.SetActive(false);
    }


    void InitDropdown() {
        tacDropdown.ClearOptions();
        List<string> options = new List<string>();
        foreach(TacticsType tt in Enum.GetValues(typeof(TacticsType))) {
            options.Add(tt.ToString());
        }
        tacDropdown.AddOptions(options);
    }
*/