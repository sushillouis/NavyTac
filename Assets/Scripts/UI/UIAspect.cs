using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIAspect : MonoBehaviour //change name to UI aspect
{
    public Entity entity;
    private void Awake() {
        entity = GetComponentInParent<Entity>();
        entity.ui = this;
    }

    // Start is called before the first frame update
    void Start()
    {

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

}
