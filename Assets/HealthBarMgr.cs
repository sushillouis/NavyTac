using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthBarMgr : MonoBehaviour
{
    // Start is called before the first frame update
    Entity entity;
    void Start()
    {
        entity = GetComponentInParent<Entity>();
        if (entity == null) return;
    }

    // Update is called once per frame
    void Update()
    {
        this.transform.LookAt(Camera.main.transform.position);

        // this.gameObject.SetActive(entity.isVisible);
    }

    

    
}
