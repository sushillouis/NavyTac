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
    public float attractiveCloseExponent = 1;
    public float repulsiveCoefficient = 60000;
    public float repulsiveExponent = -2.0f;
    public float destinationThreshold = 50;

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

    public void HandleRegionCommand(Vector3 centerPos, Vector2 edgemousepos, bool intercept, bool add, bool pincer, bool isgroup) {
        if (Physics.Raycast(Camera.main.ScreenPointToRay(edgemousepos), out hit, float.MaxValue, layerMask)) {
            if(!isgroup) {
                foreach (Entity shipInstance in SelectionMgr.inst.selectedEntities)
                {
                    shipInstance.ai.group=null;
                    HandleSunflowerMove(SelectionMgr.inst.selectedEntities, centerPos, (centerPos-hit.point).magnitude, add);
                }
            }
        }
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
                foreach (Entity shipInstance in SelectionMgr.inst.selectedEntities)
                {
                    shipInstance.ai.group=null;
                }
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
                        HandleFollow(SelectionMgr.inst.selectedEntities, ent, new Vector3(100, 0, 0), add);
                }
            } else {
                if (ent == null) {
                    Group group = TacticalAIMgr.inst.AssembleGroup(SelectionMgr.inst.selectedEntities);
                    HandleGroupEscort(group, pos, add);
                } else {
                    //Check if friendly
                    // Calling group+clicking on a firendly ship just adds the selection to that ships group 
                    UnitAI temp = ent.ai;
                    if(temp.group !=null) {
                        temp.group.AddMembers(SelectionMgr.inst.selectedEntities.ToArray());
                    }
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

    public void HandleGroupEscort(Group group, Vector3 point, bool add) {
        if(group == null) {
            return;
        }
        if(add) {
            group.AddTactic(new EscortTactic(ref group, point));
        } else {
            group.SetTactic(new EscortTactic(ref group, point));
        }
    }

    public void HandleMove(List<Entity> entities, Vector3 point, bool add)
    {
        foreach (Entity entity in entities) {
            Move m = new Move(entity, point);
            UnitAI uai = entity.ai;
            AddOrSet(m, uai, add);
        }
    }

    public void HandleSunflowerMove(List<Entity> entities, Vector3 point, float mag, bool add)
    {
        print(mag);
        int b = (int)Mathf.Round(Mathf.Sqrt(entities.Count)*2);
        float phiSquared = 2.618034f;
        for (int i = 0;i<entities.Count;i++) {
            float rad = mag;
            if (i<(entities.Count-b)) {
                rad = mag*Mathf.Sqrt(i/2f)/Mathf.Sqrt(entities.Count-(b+1)/2);
            }
            float theta = (2*Mathf.PI*(i-1))/phiSquared;
            Vector3 offset = new Vector3{
                x = rad*Mathf.Cos(theta),
                y = 0,
                z  = rad*Mathf.Sin(theta)
            };
            Move m = new Move(entities[i], point+offset);
            UnitAI uai = entities[i].ai;
            AddOrSet(m, uai, add);
            
        }
        foreach (Entity entity in entities) {
            
        }
    }

    void AddOrSet(Command c, UnitAI uai, bool add)
    {
        if (add)
            uai.AddCommand(c);
        else
            uai.SetCommand(c);
    }

    

    public void HandleFollow(List<Entity> entities, Entity ent, Vector3 offset, bool add)
    {
        foreach (Entity entity in entities) {
            if(ent != entity) {
                Follow f = new Follow(entity, ent, offset);
                UnitAI uai = entity.GetComponentInChildren<UnitAI>();
                AddOrSet(f, uai, add);
            }
        }
    }

    public void HandleIntercept(List<Entity> entities, Entity ent, bool add)
    {
        foreach(Entity entity in entities)
        {
            if(ent != entity)
            {
                Intercept intercept = new Intercept(entity, ent);
                UnitAI uai = entity.ai;
                AddOrSet(intercept, uai, add);
            }
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
            UnitAI uai = entity.ai;
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