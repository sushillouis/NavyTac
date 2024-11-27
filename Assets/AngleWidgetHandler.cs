using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class AngleWidgetHandler : MonoBehaviour
{
    public static AngleWidgetHandler inst;
    private void Awake() {
        inst = this;
    }
    // Start is called before the first frame update
    void Start() {

    }

    // Update is called once per frame
    void Update() {

    }



    [SerializeField] private RectTransform rotatingPanel; //for desiredheading
    [SerializeField] private AngleWidgetMouseEventHandler mouseEventHandler;

    [SerializeField] private Vector2 arrowPanel2DPosition = Vector2.zero; // To compute difference vector;
    [SerializeField] private Vector3 arrowEulerAngles = Vector3.zero;
    [SerializeField] private Vector3 entityHeadingEulerAngles = Vector3.zero;
    [SerializeField] private float angle;


    public void PointArrowAtAngleAndUpdateVars(PointerEventData eventData) {
        Vector2 diff = eventData.position - arrowPanel2DPosition;
        float computedAngle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        angle = RoRMath.MakeAnglePosDeg(90 - computedAngle);
        arrowEulerAngles.z = computedAngle - 90;
        rotatingPanel.localEulerAngles = arrowEulerAngles;
    }

    public void ResetMouseControlPosition() {
        arrowPanel2DPosition = new Vector2(mouseEventHandler.transform.position.x, mouseEventHandler.transform.position.y);
    }

    public void UpdateOnAngleSet() {
        ControlMgr.inst.UpdateOnHeadingSet(angle);
    }

}
