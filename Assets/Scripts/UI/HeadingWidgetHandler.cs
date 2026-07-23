using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI.Table;

public class HeadingWidgetHandler : MonoBehaviour
{



    public Vector3 ddg51ImageEulerAngles = Vector3.zero;

    [SerializeField] Entity selectedEntity = null;
    // Update is called once per frame
    void Update() {
        if(SelectionMgr.inst.selectedEntity != null) {
            selectedEntity = SelectionMgr.inst.selectedEntity;

            ddg51ImageEulerAngles.z = -selectedEntity.heading; // have to very careful here
            ddg51ImagePanel.transform.localEulerAngles = ddg51ImageEulerAngles;
            //SetSliderValue(selectedEntity.desiredSpeed, selectedEntity.maxSpeed);
        }
    }

    [Header("Set these in editor")]
    public Slider SpeedSlider;
    public RectTransform ddg51ImagePanel;

    public void HandleSlider() {
        //SetDesiredSpeed

    }

    public float[] diffs = new float[9];
    public void SetSliderValue(float ds, float maxSpeed) {
        /*
        float fraction = ds / maxSpeed;
        //float[] diffs = new float[9];
        float min = float.MaxValue;
        int mini = -1;
        for(int i = 3; i < SpeedSlider.maxValue + 1; i++) {//ensure slider never goes below 3
            diffs[i] = Mathf.Abs(fraction - RulesControlMgr.inst.speedMap[i]);
            if(diffs[i] < min) {
                min = diffs[i];
                mini = i;
            }
        }
        SpeedSlider.SetValueWithoutNotify(mini);
        */
    }


}
