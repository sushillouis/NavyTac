using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class AIMgr : MonoBehaviour
{
    public static AIMgr inst;
    private void Awake()
    {
        inst = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        layerMask = 1 << 9;// LayerMask.GetMask("Water");
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
    List<Approach> pincerApproaches = new();
    List<GameObject> pincerVisuals = new();
    [SerializeField] GameObject pincerVisualPrefab;
    Entity pincerCenterTarget = null;
    [SerializeReference] List<Formation> formations = new();
    // Update is called once per frame
    void Update()
    {
        
    }

    public void HandleCommand(Vector2 mousePos, bool intercept, bool add)
    {
        if (Physics.Raycast(Camera.main.ScreenPointToRay(mousePos), out hit, float.MaxValue, layerMask))
        {
            //Debug.DrawLine(Camera.main.transform.position, hit.point, Color.yellow, 2); //for debugging
            Vector3 pos = hit.point;
            pos.y = 0;
            Entity ent = FindClosestEntInRadius(pos, rClickRadiusSq);
            pincerCenterTarget = ent;
            if(!formationDown) {
                if (ent == null)
                {
                    HandleMove(SelectionMgr.inst.selectedEntities, pos, add);
                }
                else
                {
                    if (intercept)
                        HandleIntercept(SelectionMgr.inst.selectedEntities, ent, add);
                    else if(pincerDown)
                            pincerDragIsActive=true;
                    else
                        HandleFollow(SelectionMgr.inst.selectedEntities, ent, add);
                }
            } else {
                    Formation formation = AssembleFormation(SelectionMgr.inst.selectedEntities);
                    if (ent == null && formation != null) {
                        HandleEscortFormate(formation, pos);
                    } else {
                        
                    }
                }
        }
        else
        {
            //Debug.DrawRay(Camera.main.transform.position, Camera.main.transform.TransformDirection(Vector3.forward) * 1000, Color.white, 2);
        }
    }

    public void HandleMove(List<Entity> entities, Vector3 point, bool add)
    {
        foreach (Entity entity in entities) {
            Move m = new Move(entity, point);
            UnitAI uai = entity.GetComponentInChildren<UnitAI>();
            AddOrSet(m, uai, add);
        }
    }

    void AddOrSet(Command c, UnitAI uai, bool add)
    {
        if (add)
            uai.AddCommand(c);
        else
            uai.SetCommand(c);
    }

    public void RemoveFormation(Formation formation) {
        if (formations.Contains(formation)) {
            formations.Remove(formation);
        }
    }

    public Formation AssembleFormation(List<Entity> entities) {
        Formation targetFormation = null;
        foreach (Entity ent, bool add in entities) {
            foreach (Formation formation in formations) {
                if (formation.target == ent) {
                    targetFormation = formation;
                    break;
                }
            }
        }
        List<UnitAI> aIs = new();

        foreach (Entity ent in SelectionMgr.inst.selectedEntities) {
            UnitAI uai = ent.GetComponent<UnitAI>();
            aIs.Add(uai);
        }

        if(aIs.Count==0)
            return null;

        if(targetFormation==null) {
            targetFormation = new(aIs);
            formations.Add(targetFormation);
        } else {
            targetFormation.AddMembers(aIs.ToArray());
        }
        return targetFormation;
    }

    public void HandleFollow(List<Entity> entities, Entity target, bool add)
    {
        if(entities.Count==0) {
            return;
        }

        if(target.GetComponent<UnitAI>().formation is null) {
            foreach (Entity ent in entities) {
                if(target == ent) 
                    continue;
                Follow f = new(ent, target, new Vector3(100, 0, 0));
                UnitAI uai = ent.GetComponentInChildren<UnitAI>();
                AddOrSet(f, uai, add);
            }
            return;
        }
        List<UnitAI> aIs = new();
        foreach (Entity ent in entities) {
            UnitAI uai = ent.GetComponent<UnitAI>();
            aIs.Add(uai);
        }
        target.GetComponent<UnitAI>().formation.AddMembers(aIs.ToArray());
    }

    void HandleIntercept(List<Entity> entities, Entity ent, bool add)
    {
        foreach (Entity entity in entities) {
            Intercept intercept = new Intercept(entity, ent);
            UnitAI uai = entity.GetComponentInChildren<UnitAI>();
            AddOrSet(intercept, uai, add);
        }
    }

    void HandlePincer(List<Entity> entities, Entity ent, Approach[] approaches= null)
    {
        //Round Robbin Attacking
        int attackApproach = 0;
        if (approaches is null || approaches.Length==0) {
            approaches = new Approach[2];
            approaches[0] = new Approach(90,200);
            approaches[1] = new Approach(-90,200);
        }
        float approachAngleRange = 180/approaches.Length * .33f;
        float[] distributeApproaches = new float[approaches.Length];
        for (int i = 0; i<distributeApproaches.Length;i++) {

            distributeApproaches[i]=-1f;
            
        } 
        foreach (Entity entity in entities) {
            if(ent == entity) 
                continue;
            float tempAprroach = approachAngleRange * distributeApproaches[attackApproach] + approaches[attackApproach].angle;
            distributeApproaches[attackApproach] += 2f/(((float)(entities.Count+(entities.Count%approaches.Length))-1)/approaches.Length);
            Pincer pincer = new(entity, ent, approaches[attackApproach].mag, tempAprroach);
            // print("Mag: "+approaches[attackApproach].mag+" Angle: "+approaches[attackApproach].angle);
            if(++attackApproach >= approaches.Length) {
                attackApproach=0;
            }
            UnitAI uai = entity.GetComponent<UnitAI>();
            AddOrSet(pincer, uai);
        }
        
        ClearPincerData();
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
}