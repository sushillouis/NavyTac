using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
//using UnityEngine.InputSystem;



public class SelectionMgr : MonoBehaviour
{
    public static SelectionMgr inst;
    private void Awake()
    {
        inst = this;
    }

    // Start is called before the first frame update
    void Start()
    {

    }
    //----------------------------------------------------------------------------------------------------
    public bool isSelecting = false;
    public Vector3 startMousePosition;
    public RectTransform SelectionBoxPanel;
    public RectTransform UICanvas;
    public Canvas mainCanvas;
    public int numTouches;

    // Update is called once per frame
    void Update()
    {
        
    }
    public void StartBoxSelecting()
    {
        startMousePosition = Input.mousePosition;
        SelectionBoxPanel.gameObject.SetActive(true);
    }
    public float selectionSensitivity = 25;
    public void EndBoxSelecting()
    {
        if((Input.mousePosition - startMousePosition).sqrMagnitude > selectionSensitivity)
            ClearSelection(); // if not small box, then clear selection

        SelectEntitiesInBox(startMousePosition, Input.mousePosition);
        SelectionBoxPanel.gameObject.SetActive(false);
    }

    public void UpdateSelectionBox(Vector3 end)
    {
        Vector2 localMousePosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(UICanvas, startMousePosition, null, out localMousePosition);
        SelectionBoxPanel.localPosition = localMousePosition;
        SetPivotAndAnchors(startMousePosition, end);
        SelectionBoxPanel.sizeDelta = 
            new Vector2(Mathf.Abs(end.x - startMousePosition.x), Mathf.Abs(startMousePosition.y - end.y))/mainCanvas.scaleFactor;
    }
    public Vector2 anchorMin = Vector2.up;
    public Vector2 anchorMax = Vector2.up;
    public Vector3 pivot = Vector2.up;
    public void SetPivotAndAnchors(Vector3 start, Vector3 end)
    {
        Vector3 diff = end - start;
        // which quadrant?
        if(diff.x >= 0 && diff.y >= 0) {//q1
            SetPAValues(Vector2.zero);
        } else if (diff.x < 0 && diff.y >= 0) { //q2
            SetPAValues(Vector2.right);
        } else if (diff.x < 0 && diff.y < 0) { //q3
            SetPAValues(Vector2.one);
        } else { //q4
            SetPAValues(Vector2.up);
        }
    }
    void SetPAValues(Vector2 val)
    {
        SelectionBoxPanel.anchorMax = val;
        SelectionBoxPanel.anchorMin = val;
        SelectionBoxPanel.pivot = val;
    }
    //--------------------------------------------------------------
    public Vector3 wp1;
    public Vector3 wp2;
    public void SelectEntitiesInBox(Vector3 start, Vector3 end)
    {
        wp1 = Camera.main.ScreenToViewportPoint(start);
        wp2 = Camera.main.ScreenToViewportPoint(end);
        Vector3 min = Vector3.Min(wp1, wp2);
        Vector3 max = Vector3.Max(wp1, wp2);
        min.z = Camera.main.nearClipPlane;
        max.z = Camera.main.farClipPlane;
        Bounds bounds = new Bounds();
        bounds.SetMinMax(min, max);
        foreach(Entity ent in EntityMgr.inst.entities) 
            if (bounds.Contains(Camera.main.WorldToViewportPoint(ent.transform.localPosition))) 
                SelectEntity(ent, shouldClearSelection: false);

        TacticalAIMgr.inst.currentGroup = new Group(new List<Entity>());
    }
    //----------------------------------------------------------------------------------------------------

    public int selectedEntityIndex = -1;
    public Entity selectedEntity = null;
    public List<Entity> selectedEntities = new List<Entity>();

    public void SelectNextEntity(bool clearSelection)
    {
        List<Entity> ownedEntities = new List<Entity>();
        foreach(Entity ent in EntityMgr.inst.entities)
            if(ent.owner.playerId == PlayerMgr.inst.localPlayer.playerId)
                ownedEntities.Add(ent);

        selectedEntityIndex = 
            (selectedEntityIndex >= ownedEntities.Count - 1 ? 0 : selectedEntityIndex + 1);
        SelectEntity(ownedEntities[selectedEntityIndex], 
            shouldClearSelection: !clearSelection);
    }

    public void ClearSelection()
    {
        foreach(Entity ent in EntityMgr.inst.entities)
            ent.isSelected = false;
        selectedEntities.Clear();
        selectedEntity = null;
    }


    public void DeselectEntity(Entity ent) {
        if(ent != null 
            && ent.owner.playerId == PlayerMgr.inst.localPlayer.playerId 
            && selectedEntities.Contains(ent)) {
            
            ent.isSelected = false;
            if(selectedEntity == ent) {
                if(selectedEntities.Count > 0)
                    selectedEntity = selectedEntities.Find(x => x != ent);//could be null
                else
                    selectedEntity = null;
            }
            selectedEntities.Remove(ent);
        }
    }

    public void SelectEntity(Entity ent, bool shouldClearSelection = true)
    {
        if (ent != null && (ent.owner.playerId == PlayerMgr.inst.localPlayer.playerId)
            && (selectedEntityIndex = EntityMgr.inst.entities.FindIndex(x => (x == ent))) >= 0) {
           
            if (shouldClearSelection) 
                ClearSelection();

            selectedEntity = ent;
            selectedEntity.isSelected = true;
            if(!selectedEntities.Contains(ent))
                selectedEntities.Add(ent);
        }
    }

    /*
    public void SelectEntity(Vector2 mousePos, bool shouldClearSelection = true)
    {
        RaycastHit hit;
        Physics.Raycast(Camera.main.ScreenPointToRay(mousePos), out hit, float.MaxValue, AIMgr.inst.layerMask);
        Entity ent = UIMgr.inst.FindClosestEntInRadius(hit.point);//, AIMgr.inst.rClickRadiusSq);
        bool shouldAddSelection = !shouldClearSelection;
        if(ent != null) {
            if(!shouldAddSelection)
                ClearSelection();

            if(!selectedEntities.Contains(ent)) {
                selectedEntity = ent;
                selectedEntity.isSelected = true;
                selectedEntities.Add(ent);
            } else if(shouldAddSelection) {
                if(selectedEntity = ent) {
                    if(SelectionMgr.inst.selectedEntities[0] != null)
                        selectedEntity = EntityMgr.inst.entities[0];
                    else
                        selectedEntity = null;
                }
                ent.isSelected = false;
                selectedEntities.Remove(ent);
            }
        } else {
            ClearSelection();
        }
    }
    */

    public void SelectEntity2(Vector2 mousePos, bool addSelection) {
        RaycastHit hit;
        Physics.Raycast(Camera.main.ScreenPointToRay(mousePos), out hit, float.MaxValue, AIMgr.inst.layerMask);
        Entity ent = UIMgr.inst.FindClosestEntInRadius(hit.point);

        if(ent == null) {
            ClearSelection();
        } else {
            if(addSelection) {
                if(!selectedEntities.Contains(ent)) {
                    SelectEntity(ent, shouldClearSelection: false);
                } else {
                    DeselectEntity(ent);
                }
            } else {
                SelectEntity(ent, shouldClearSelection: true);
            }

        }
    }


    /// <summary>
    /// Assigns selected entities for control group given by groupNumber param
    /// </summary>
    /// <param name="groupNumber"></param>
    public void SelectControlGroup(int groupNumber) {
        if(TacticalAIMgr.inst.Exists(groupNumber)) {
            ClearSelection();
            TacticalAIMgr.inst.SelectControlGroup(groupNumber);
        }
    }

    public void SelectAll() {
        ClearSelection();
        foreach(Entity ent in EntityMgr.inst.entities) {
            if(ent.owner.playerId == PlayerMgr.inst.localPlayer.playerId) {
                SelectEntity(ent, shouldClearSelection: false);
            }
        }
    }

    public void FormControlGroup(int groupNumber) {
        if(selectedEntities.Count > 0) {
            TacticalAIMgr.inst.CreateBindControlGroup(selectedEntities, groupNumber);
        }

    }

}
