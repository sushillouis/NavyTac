using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IconsMgr : MonoBehaviour
{
    Camera cam;
    bool iconsOn;
    public bool distanceFromEntityBased;
    public float distanceThreshold;
    // Start is called before the first frame update
    void Start()
    {
        cam = Camera.main;
        iconsOn = false;
    }

    // Update is called once per frame
    void Update()
    {
        if(distanceFromEntityBased)
        {
            HandleSwitching();
        }
        else
        {
            if (!iconsOn && cam.transform.position.y > distanceThreshold)
            {
                TurnOnIcons();
            }

            if (iconsOn && cam.transform.position.y <= distanceThreshold)
            {
                TurnOnModels();
            }
        }
        
    }

    void TurnOnModels()
    {
        foreach (Entity ent in EntityMgr.inst.entities)
        {
            ent.gameObject.GetComponentInChildren<UIAspect>().TurnOnModels();
        }
        iconsOn = false;
    }

    void TurnOnIcons()
    {
        foreach(Entity ent in EntityMgr.inst.entities)
        {
            ent.gameObject.GetComponentInChildren<UIAspect>().TurnOnIcons();
        }
        iconsOn = true;
    }

    void HandleSwitching()
    {
        foreach (Entity ent in EntityMgr.inst.entities)
        {
            UIAspect uia = ent.gameObject.GetComponentInChildren<UIAspect>();
            float dist = Vector3.SqrMagnitude(ent.position - cam.transform.position);
            if (uia.shipModel.activeSelf && dist > uia.iconThreshold * uia.iconThreshold)
            {
                uia.TurnOnIcons();
            }
            if (uia.icon.activeSelf && dist <= uia.iconThreshold * uia.iconThreshold)
            {
                uia.TurnOnModels();
            }
        }
    }
}
