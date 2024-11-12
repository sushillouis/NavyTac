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

    // Update is called once per frame
    void Update()
    {
        
    }

    public void HandleCommand(Vector2 mousePos, bool intercept, bool add)
    {
        if (UIMgr.inst.getTargetPosition(mousePos,out Vector3 targetPosition))
        {
            //Debug.DrawLine(Camera.main.transform.position, hit.point, Color.yellow, 2); //for debugging
            
            Entity ent = UIMgr.inst.GetTargetEntity(targetPosition);
            if (ent == null)
            {
                HandleMove(SelectionMgr.inst.selectedEntities, targetPosition, add);
            }
            else
            {
                if (intercept)
                    HandleIntercept(SelectionMgr.inst.selectedEntities, ent, add);
                else
                    HandleFollow(SelectionMgr.inst.selectedEntities, ent, new Vector3(100, 0, 0), add);
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
                UnitAI uai = entity.GetComponentInChildren<UnitAI>();
                AddOrSet(intercept, uai, add);
            }
        }

    }

    public void Handle3dIntercept(List<Entity> entities, Entity ent, bool add)
    {
        foreach(Entity entity in entities)
        {
            if(ent != entity)
            {
                Intercept3d intercept3d = new Intercept3d(entity, ent);
                UnitAI uai = entity.GetComponentInChildren<UnitAI>();
                AddOrSet(intercept3d, uai, add);
            }
        }

    }
    public void HandleSmartIntercept(List<Entity> entities, Entity ent, bool add)
    {
        foreach(Entity entity in entities)
        {
            if(ent != entity)
            {
                SmartIntercept smartIntercept = new SmartIntercept(entity, ent);
                UnitAI uai = entity.GetComponentInChildren<UnitAI>();
                AddOrSet(smartIntercept, uai, add);
            }
        }

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
