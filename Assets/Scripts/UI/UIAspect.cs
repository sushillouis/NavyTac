using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIAspect : MonoBehaviour //change name to UI aspect
{
    public Entity entity;

    [Header("Minimap Icon")]
    [SerializeField]
    private GameObject minimapIcon;

    private void Awake() {
        entity = GetComponentInParent<Entity>();
        entity.ui = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        MinimapMgr.inst.CreateMinimapIcon(entity, minimapIcon);


    }

    // Update is called once per frame
    void Update()
    {
        if(entity != null)
            entity.selectionCircle.SetActive(entity.isSelected);
    }

}
