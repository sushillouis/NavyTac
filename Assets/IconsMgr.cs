using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IconsMgr : MonoBehaviour
{
    Camera cam;
    bool iconsOn;
    public bool distanceFromEntityBased;
    public float distanceThreshold;
    public AnimationCurve heightMultiplier;
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

            if (iconsOn && cam.transform.position.y > distanceThreshold)
            {
                ScaleIcons(cam.transform.position.y);
            }
        }
        
    }

    void TurnOnModels()
    {
        foreach (Entity ent in EntityMgr.inst.entities)
        {
            ent.uia.TurnOnModels();
        }
        iconsOn = false;
    }

    void TurnOnIcons()
    {
        foreach(Entity ent in EntityMgr.inst.entities)
        {
            ent.uia.TurnOnIcons();
        }
        iconsOn = true;
    }

    void ScaleIcons(float height)
    {
        foreach (Entity ent in EntityMgr.inst.entities)
        {
            ent.uia.icon.transform.localScale = ent.uia.baseIconScale * heightMultiplier.Evaluate(height);
        }
    }

    void HandleSwitching()
    {
        foreach (Entity ent in EntityMgr.inst.entities)
        {
            UIAspect uia = ent.uia;
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
