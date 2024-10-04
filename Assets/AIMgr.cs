﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class AIMgr : MonoBehaviour
{
    public static AIMgr inst;
    private GameInputs input;
    private void Awake()
    {
        inst = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        layerMask = 1 << 9;// LayerMask.GetMask("Water");
        input = new GameInputs();
        input.Enable();
        input.Entities.Intercept.performed += OnInterceptPerformed;
        input.Entities.Intercept.canceled += OnInterceptCanceled;
        input.Entities.Pincer.performed += OnPincerPerformed;
        input.Entities.Pincer.canceled += OnPincerCanceled;
        input.Entities.ClearSelection.performed += OnClearSelectionPerformed;
        input.Entities.ClearSelection.canceled += OnClearSelectionCanceled;
    }

    public bool isPotentialFieldsMovement = false;
    public float potentialDistanceThreshold = 1000;
    public float attractionCoefficient = 500;
    public float attractiveExponent = -1;
    public float repulsiveCoefficient = 60000;
    public float repulsiveExponent = -2.0f;


    public RaycastHit hit;
    public int layerMask;
    bool pincerDragIsActive = false;
    List<float> pincerApproaches = new List<float>();
    Entity pincerCenterTarget = null;
    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(1)) {
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, float.MaxValue, layerMask)) {
                //Debug.DrawLine(Camera.main.transform.position, hit.point, Color.yellow, 2); //for debugging
                Vector3 pos = hit.point;
                pos.y = 0;
                Entity ent = FindClosestEntInRadius(pos, rClickRadiusSq);
                pincerCenterTarget = ent;
                if (ent == null) {
                    HandleMove(SelectionMgr.inst.selectedEntities, pos);
                } else {
                    if (interceptDown)
                        HandleIntercept(SelectionMgr.inst.selectedEntities, ent);
                    else if(pincerDown)
                        pincerDragIsActive=true;
                        
                    else
                        HandleFollow(SelectionMgr.inst.selectedEntities, ent);
                }
            } else {
                //Debug.DrawRay(Camera.main.transform.position, Camera.main.transform.TransformDirection(Vector3.forward) * 1000, Color.white, 2);
            }
        }

        if(Input.GetMouseButton(1)) {
            if(!pincerDown) {
                pincerDragIsActive=false;
            }
        }

        if(Input.GetMouseButtonUp(1)) {
            if(pincerDragIsActive && Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, float.MaxValue, layerMask) && pincerCenterTarget) {
                Vector3 dif = hit.point - pincerCenterTarget.position;
                if(dif.sqrMagnitude>5) {
                    float angle = Vector3.SignedAngle(pincerCenterTarget.transform.forward,dif, Vector3.up);
                    // angle =  hit.point.x * pincerCenterTarget.position.y - hit.point.y * pincerCenterTarget.position.x <= 0 ? -angle : angle;
                    pincerApproaches.Add(angle);
                    print("Adding:"+angle);
                }
            } else if(pincerApproaches.Count>0 && pincerCenterTarget) {
                
                HandlePincer(SelectionMgr.inst.selectedEntities,pincerCenterTarget,pincerApproaches.ToArray());                
            }
        }
    }

    public void HandleMove(List<Entity> entities, Vector3 point)
    {
        foreach (Entity entity in entities) {
            Move m = new Move(entity, point);
            UnitAI uai = entity.GetComponent<UnitAI>();
            AddOrSet(m, uai);
        }
    }

    void AddOrSet(Command c, UnitAI uai)
    {
        if (addDown)
            uai.AddCommand(c);
        else
            uai.SetCommand(c);
    }



    public void HandleFollow(List<Entity> entities, Entity ent)
    {
        foreach (Entity entity in SelectionMgr.inst.selectedEntities) {
            Follow f = new Follow(entity, ent, new Vector3(100, 0, 0));
            UnitAI uai = entity.GetComponent<UnitAI>();
            AddOrSet(f, uai);
        }
    }

    void HandleIntercept(List<Entity> entities, Entity ent)
    {
        foreach (Entity entity in SelectionMgr.inst.selectedEntities) {
            Intercept intercept = new Intercept(entity, ent);
            UnitAI uai = entity.GetComponent<UnitAI>();
            AddOrSet(intercept, uai);
        }

    }

    void HandlePincer(List<Entity> entities, Entity ent, float[] approaches)
    {
        //Round Robbin Attacking
        int attackApproach = 0;
        foreach (Entity entity in SelectionMgr.inst.selectedEntities) {
            if(ent == entity) 
                continue;
            Pincer intercept = new(entity, ent, 200, approaches,attackApproach);
            attackApproach++;
            if(attackApproach >= approaches.Length) {
                attackApproach=0;
            }
            UnitAI uai = entity.GetComponent<UnitAI>();
            AddOrSet(intercept, uai);
        }
        pincerApproaches.Clear();
        pincerCenterTarget=null;
    }

    public float rClickRadiusSq = 10000;
    public Entity FindClosestEntInRadius(Vector3 point, float rsq)
    {
        Entity minEnt = null;
        float min = float.MaxValue;
        foreach (Entity ent in EntityMgr.inst.entities) {
            float distanceSq = (ent.transform.position - point).sqrMagnitude;
            if (distanceSq < rsq) {
                if (distanceSq < min) {
                    minEnt = ent;
                    min = distanceSq;
                }
            }    
        }
        return minEnt;
    }

    bool interceptDown = false;
    private void OnInterceptPerformed(InputAction.CallbackContext context)
    {
        interceptDown = true;
    }

    private void OnInterceptCanceled(InputAction.CallbackContext context)
    {
        interceptDown = false;
    }

    bool pincerDown = false;
    private void OnPincerPerformed(InputAction.CallbackContext context)
    {
        pincerDown = true;
    }

    private void OnPincerCanceled(InputAction.CallbackContext context)
    {
        pincerDown = false;
        if(pincerApproaches.Count>0 && pincerCenterTarget) {
            HandlePincer(SelectionMgr.inst.selectedEntities,pincerCenterTarget,pincerApproaches.ToArray());
        }
    }

    bool addDown = false;
    private void OnClearSelectionPerformed(InputAction.CallbackContext context)
    {
        addDown = true;
    }

    private void OnClearSelectionCanceled(InputAction.CallbackContext context)
    {
        addDown = false;
    }
}
