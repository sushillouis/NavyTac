using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EntityPotential
{
    public Entity entity;
    public Potential potential;
}

public class UnitAI : MonoBehaviour
{   
    public Entity entity; //public only for ease of debugging

    public  Queue<Command> commands = new();
    public List<Move> moves = new();
    public List<AttackMove> attackMoves = new();
    public List<Follow> follows = new();
    public List<Intercept> intercepts = new();
    public List<Intercept3d> intercept3ds = new();
    public List<SmartIntercept> smartIntercepts = new();

    [Header("PF nodes")]
    public List<Transform> pfList = new();
    public Dictionary<Entity, Potential> potentialsD = new();
    public List<EntityPotential> potentialsL = new();



    private void Awake() {
        entity = GetComponentInParent<Entity>();
        entity.ai = this;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (entity.entityType == EntityType.Rig_Balder) return;
        if (commands.Count == 0) return;
        Command currentCommand = commands.Peek();
        if (currentCommand.IsDone())
        {
            commands.Dequeue();
            StopAndRemoveCommand(currentCommand);
        }
        else
        {
            currentCommand.Tick();
            currentCommand.isRunning = true;
        }
    
    }

    private void Update()
    {
        DecorateAll();
    }
    private void StopAndRemoveCommand(Command cmd)
    {
        switch (cmd)
        {
            case Intercept3d intercept3d:
                intercept3d.Stop();
                intercept3ds.Remove(intercept3d);
                break;
            case SmartIntercept smartIntercept:
                smartIntercept.Stop();
                smartIntercepts.Remove(smartIntercept);
                break;
            case AttackMove attackMove:
                attackMove.Stop();
                attackMoves.Remove(attackMove);
                break;
            case Intercept intercept:
                intercept.Stop();
                intercepts.Remove(intercept);
                break;
            case Follow follow:
                follow.Stop();
                follows.Remove(follow);
                break;
            case Move move:
                move.Stop();
                moves.Remove(move);
                break;
            default:
                //Debug.LogWarning($"Unknown command type: {cmd.GetType()}");
                break;
        }
    }

    public void StopAndRemoveAllCommands()
    {
        while (commands.Count > 0)
        {
            Command cmd =  commands.Dequeue();
            StopAndRemoveCommand(cmd);
            // //Debug.Log("Stopping and removing all commands");
        }
    }


    public void AddCommand(Command c)
    {
        c.Init();
        commands.Enqueue(c);

        switch (c)
        {
            case AttackMove attackMove:
                attackMoves.Add(attackMove);
                break;
            case SmartIntercept smartIntercept:
                smartIntercepts.Add(smartIntercept);
                break;
            case Intercept3d intercept3d:
                intercept3ds.Add(intercept3d);
                break;
            case Intercept intercept:
                intercepts.Add(intercept);
                break;
            case Follow follow:
                follows.Add(follow);
                break;
            case Move move:
                moves.Add(move);
                break;
            default:
                //Debug.LogWarning($"Unknown command type: {c.GetType()}");
                break;
        }
    }

    public void SetCommand(Command c)
    {
        StopAndRemoveAllCommands();
        AddCommand(c);
    }

    public Vector3 GetQueueTailPosition()
    {
        if (commands.Count == 0) return entity.position;
        Vector3 tail = entity.position;
        foreach (var cmd in commands)
        {
            switch (cmd)
            {
                case AttackMove am: tail = am.movePosition; break;
                case Intercept3d i3: tail = i3.targetEntity.position; break;
                case SmartIntercept si: tail = si.targetEntity.position; break;
                case Intercept i:    tail = i.targetEntity.position; break;
                case Follow f:       tail = f.targetEntity.position + f.offset; break;
                case Move m:         tail = m.movePosition; break;
            }
        }
        return tail;
    }

    public void DecorateAll()
    {
        Command prior = null;
        foreach(Command c in commands) {
            Decorate(prior, c);
            prior = c;
        }
    }

    //decoration logic (UI logic) in general is always convoluted. Ugh
    public void Decorate(Command prior, Command current)
    {
        if(FogWarMgr.inst.nonRevelers.Contains(entity)) return;
        if (current.line != null) {
            current.line.gameObject.SetActive(entity.isSelected);
            if (prior == null)
                current.line.SetPosition(0, entity.position);
            else
                current.line.SetPosition(0, prior.line.GetPosition(1));

            if (current is Intercept) { //Most specific
                Intercept intercept = current as Intercept;
                if (intercept.isRunning)// 
                    intercept.line.SetPosition(1, intercept.predictedMovePosition);
                else
                    intercept.line.SetPosition(1, intercept.targetEntity.position);
                intercept.line.SetPosition(2, intercept.targetEntity.position);

            } else if (current is Follow) { // Less specific
                Follow f = current as Follow;
                f.line.SetPosition(1, f.targetEntity.position + f.offset);
                f.line.SetPosition(2, f.targetEntity.position);
                //f.line.SetPosition(1, f.predictedMovePosition);
            }
            //Moveposition never changes
        }

        //potential fields lines
        if(!(current is Follow) && !(current is Intercept) && AIMgr.inst.isPotentialFieldsMovement){ 
            Move m = current as Move;
            if (m.potentialLine == null) return;
            if(!FogWarMgr.inst.nonRevelers.Contains(entity)) m.potentialLine.SetPosition(0, entity.position);
            Vector3 newpos = Vector3.zero;
            newpos.x = Mathf.Sin(entity.desiredHeading * Mathf.Deg2Rad) * entity.desiredSpeed;
            newpos.z = Mathf.Cos(entity.desiredHeading * Mathf.Deg2Rad) * entity.desiredSpeed;
            newpos *= 20;
            newpos.y = 1;
            
            m.potentialLine.SetPosition(1, entity.position + newpos);
            m.potentialLine.gameObject.SetActive(entity.isSelected);
        }
    }
}