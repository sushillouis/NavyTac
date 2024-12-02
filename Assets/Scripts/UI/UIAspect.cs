using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIAspect : MonoBehaviour //change name to UI aspect
{
    public Entity entity;
    public GameObject shipModel;

    [Header("Distance Icon Parameters")]
    public ShipSize shipSize;
    public float iconThreshold;
    public GameObject icon;
    public Vector3 baseIconScale;
    public AnimationCurve heightMultiplier;
    // Start is called before the first frame update
    void Start()
    {
        entity = GetComponentInParent<Entity>();
        icon.GetComponent<Renderer>().material.color = entity.owner.playerColor;
        baseIconScale = icon.transform.localScale;
        icon.SetActive(false);
        if(shipSize == ShipSize.Large)
        {
            iconThreshold = IconsMgr.inst.iconSizeParameters[0].threshold;
            heightMultiplier = IconsMgr.inst.iconSizeParameters[0].curve;
        }
        else if(shipSize == ShipSize.Medium)
        {
            iconThreshold = IconsMgr.inst.iconSizeParameters[1].threshold;
            heightMultiplier = IconsMgr.inst.iconSizeParameters[1].curve;
        }
        else
        {
            iconThreshold = IconsMgr.inst.iconSizeParameters[2].threshold;
            heightMultiplier = IconsMgr.inst.iconSizeParameters[2].curve;
        }
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
    public void ScaleIcon(float height)
    {
        icon.transform.localScale = baseIconScale * heightMultiplier.Evaluate(height);
    }

}
