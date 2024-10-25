using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIAspect : MonoBehaviour //change name to UI aspect
{
    public Entity entity;
    public float iconThreshold;
    public GameObject shipModel;
    public GameObject icon;
    // Start is called before the first frame update
    void Start()
    {
        entity = GetComponentInParent<Entity>();
        icon.GetComponent<Renderer>().material.color = Random.ColorHSV();
        icon.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if(entity != null)
            entity.selectionCircle.SetActive(entity.isSelected);
    }

    /*
    private void OnMouseDown()
    {
        //if (Input.GetMouseButtonDown(0)) {
            SelectionMgr.inst.SelectEntity(entity);
        //}
    }
    */

    public void TurnOnModels()
    {
        shipModel.SetActive(true);
        icon.SetActive(false);
    }

    public void TurnOnIcons()
    {
        shipModel.SetActive(false);
        icon.SetActive(true);
    }

}
