using System.Collections;
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
    List<Approach> pincerApproaches = new();
    List<GameObject> pincerVisuals = new();
    [SerializeField] GameObject pincerVisualPrefab;
    Entity pincerCenterTarget = null;
    List<Formation> formations = new();
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
                hit.point = Vector3.Scale(hit.point, new Vector3(1,0,1));
                Vector3 dif = hit.point - Vector3.Scale(pincerCenterTarget.position, new Vector3(1,0,1));
                if(dif.sqrMagnitude>5) {
                    float angle = Vector3.SignedAngle(pincerCenterTarget.transform.forward,dif, Vector3.up);
                    // angle =  hit.point.x * pincerCenterTarget.position.y - hit.point.y * pincerCenterTarget.position.x <= 0 ? -angle : angle;
                    pincerApproaches.Add(new(angle,dif.magnitude));
                    pincerVisuals.Add(Instantiate(pincerVisualPrefab, hit.point+Vector3.up*5, Quaternion.identity, pincerCenterTarget.transform));
                }
            } else if(pincerApproaches.Count>0 && pincerCenterTarget) {
                HandlePincer(SelectionMgr.inst.selectedEntities,pincerCenterTarget,pincerApproaches.ToArray());                
            } else {
                ClearPincerData();
            }
        }
    }

    public void ClearPincerData() {
        for (int i = 0; i<pincerVisuals.Count;i++) {
            Destroy(pincerVisuals[i]);
        }
        pincerVisuals.Clear();
        pincerApproaches.Clear();
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

    public void RemoveFormation(Formation formation) {
        if (formations.Contains(formation)) {
            formation.Dispand();
            formations.Remove(formation);
        }
    }

    public void HandleFollow(List<Entity> entities, Entity ent)
    {
        if(entities.Count==1 && ent.followers==0) {
            if(ent == entities[0]) 
                return;
            Follow f = new(entities[0], ent, new Vector3(100, 0, 0));
            UnitAI uai = entities[0].GetComponent<UnitAI>();
            AddOrSet(f, uai);
            return;
        }

        Formation targetFormation = null;

        foreach (Formation formation in formations) {
            if (formation.target == ent) {
                targetFormation = formation;
                break;
            }
        }
        if(targetFormation==null) {
            targetFormation = new(ent);
            formations.Add(targetFormation);
        }

        List<UnitAI> aIs = new();

        for (int i = 0; i<entities.Count; i++) {
            if(ent == entities[i]) 
                continue;
            FormationMove f = new(entities[i], ent,targetFormation);
            UnitAI uai = entities[i].GetComponent<UnitAI>();
            // Cannot Chain Formation Move
            uai.SetCommand(f);
            aIs.Add(uai);
        }
        targetFormation.AddMembers(aIs.ToArray());
    }

    void HandleIntercept(List<Entity> entities, Entity ent)
    {
        foreach (Entity entity in SelectionMgr.inst.selectedEntities) {
            Intercept intercept = new Intercept(entity, ent);
            UnitAI uai = entity.GetComponent<UnitAI>();
            AddOrSet(intercept, uai);
        }
    }

    void HandlePincer(List<Entity> entities, Entity ent, Approach[] approaches)
    {
        //Round Robbin Attacking
        int attackApproach = 0;
        foreach (Entity entity in entities) {
            if(ent == entity) 
                continue;
            Pincer pincer = new(entity, ent, approaches[attackApproach].mag, approaches[attackApproach].angle);
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