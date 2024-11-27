using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class AngleWidgetMouseEventHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    // Start is called before the first frame update
    void Start() {

    }

    // Update is called once per frame
    void Update() {

    }

    public void OnDrag(PointerEventData eventData) {
        AngleWidgetHandler.inst.PointArrowAtAngleAndUpdateVars(eventData);
    }
    public void OnPointerDown(PointerEventData eventData) {
        AngleWidgetHandler.inst.ResetMouseControlPosition();
        AngleWidgetHandler.inst.PointArrowAtAngleAndUpdateVars(eventData);
    }
    public void OnPointerUp(PointerEventData eventData) {
        AngleWidgetHandler.inst.PointArrowAtAngleAndUpdateVars(eventData);
        AngleWidgetHandler.inst.UpdateOnAngleSet();
    }
}
