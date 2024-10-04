using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIMgr : MonoBehaviour
{
    public static UIMgr inst;
    public GameObject ToggleMultiSelect;
    public bool isActive;
    private void Awake()
    {
        inst = this;
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
    }
}
