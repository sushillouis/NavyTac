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
    // List<Approach> pincerApproaches = new();
    // List<GameObject> pincerVisuals = new();
    // [SerializeField] GameObject pincerVisualPrefab;
    // Entity pincerCenterTarget = null;
    // Update is called once per frame
    void Update()
    {
        
    }

    public void HandleCommand(Vector2 mousePos, bool intercept, bool add, bool pincer, bool isgroup)
    {
        if (Physics.Raycast(Camera.main.ScreenPointToRay(mousePos), out hit, float.MaxValue, layerMask))
        {
            //Debug.DrawLine(Camera.main.transform.position, hit.point, Color.yellow, 2); //for debugging
            Vector3 pos = hit.point;
            pos.y = 0;
            Entity ent = FindClosestEntInRadius(pos, rClickRadiusSq);
            // pincerCenterTarget = ent;
            if(!isgroup) {
                if (ent == null)
                {
                    HandleMove(SelectionMgr.inst.selectedEntities, pos, add);
                }
                else
                {
                    if (intercept)
                        HandleIntercept(SelectionMgr.inst.selectedEntities, ent, add);
                    else if(pincer)
                        HandlePincer(SelectionMgr.inst.selectedEntities, ent, add);
                    else
                        HandleFollow(SelectionMgr.inst.selectedEntities, ent, add);
                }
            } else {
                print("LMAO");
                Group group = TacticalAIMgr.inst.AssembleGroup(SelectionMgr.inst.selectedEntities);
                if (ent == null && group != null) {
                    HandleEscortFormate(group, pos, add);
                } else {
                        
                }
            }
        }
        else
        {
            //Debug.DrawRay(Camera.main.transform.position, Camera.main.transform.TransformDirection(Vector3.forward) * 1000, Color.white, 2);
        }
    }

    // if(Input.GetMouseButton(1)) {
    //         if(!pincerDown) {
    //             pincerDragIsActive=false;
    //         }
    //     }

    //     if(Input.GetMouseButtonUp(1)) {
    //         if(pincerDragIsActive && Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, float.MaxValue, layerMask) && pincerCenterTarget) {
    //             hit.point = Vector3.Scale(hit.point, new Vector3(1,0,1));
    //             Vector3 dif = hit.point - Vector3.Scale(pincerCenterTarget.position, new Vector3(1,0,1));
    //             if(dif.magnitude>50) {
    //                 float angle = Vector3.SignedAngle(pincerCenterTarget.transform.forward,dif, Vector3.up);
    //                 // angle =  hit.point.x * pincerCenterTarget.position.y - hit.point.y * pincerCenterTarget.position.x <= 0 ? -angle : angle;
    //                 pincerApproaches.Add(new(angle,dif.magnitude));
    //                 pincerVisuals.Add(Instantiate(pincerVisualPrefab, hit.point+Vector3.up*5, Quaternion.identity, pincerCenterTarget.transform));
    //             } else if(pincerCenterTarget && pincerDown) {
    //                 HandlePincer(SelectionMgr.inst.selectedEntities,pincerCenterTarget,pincerApproaches.ToArray());
    //             }
    //         } else if(pincerCenterTarget && pincerDown) {
    //             HandlePincer(SelectionMgr.inst.selectedEntities,pincerCenterTarget,pincerApproaches.ToArray());                
    //         } else {
    //             ClearPincerData();
    //         }
    //     }
    // }

    // public void ClearPincerData() {
    //     for (int i = 0; i<pincerVisuals.Count;i++) {
    //         Destroy(pincerVisuals[i]);
    //     }
    //     pincerVisuals.Clear();
    //     pincerApproaches.Clear();
    // }

    public void HandleEscortFormate(Group group, Vector3 point, bool add) {
        foreach (UnitAI aI in group.members) {
            if (aI.GetComponentInParent<Entity>() == group.target) {
                Move m = new Move(group.target, point);
                AddOrSet(m, aI, add);
            } else {
                EscortFormate escort = new EscortFormate(aI.GetComponentInParent<Entity>(), group.target, Vector3.zero);
                AddOrSet(escort, aI, add);
            }
        }
        group.groupStrategy = new CircleEscortMove();
        group.RebuildGroup();
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

    

    public void HandleFollow(List<Entity> entities, Entity target, bool add)
    {
        if(entities.Count==0) {
            return;
        }

        if(target.GetComponentInChildren<UnitAI>().group is null) {
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
            UnitAI uai = ent.GetComponentInChildren<UnitAI>();
            aIs.Add(uai);
        }
        target.GetComponentInChildren<UnitAI>().group.AddMembers(aIs.ToArray());
    }

    void HandleIntercept(List<Entity> entities, Entity ent, bool add)
    {
        foreach (Entity entity in entities) {
            Intercept intercept = new Intercept(entity, ent);
            UnitAI uai = entity.GetComponentInChildren<UnitAI>();
            AddOrSet(intercept, uai, add);
        }
    }

    void HandlePincer(List<Entity> entities, Entity ent, bool add, Approach[] approaches= null)
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
            UnitAI uai = entity.GetComponentInChildren<UnitAI>();
            AddOrSet(pincer, uai, add);
        }
        
        // ClearPincerData();
        // pincerCenterTarget=null;
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