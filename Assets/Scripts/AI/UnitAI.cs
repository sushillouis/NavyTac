using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitAI : MonoBehaviour
{
    public Entity entity; //public only for ease of debugging

    private void Awake() {
        entity = GetComponentInParent<Entity>();
        entity.ai = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        commands = new List<Command>();
        intercepts = new List<Intercept>();
        intercept3ds = new List<Intercept3d>();
        follows = new List<Follow>();
        moves = new List<Move>();
    }

    public List<Move> moves;
    public List<Follow> follows;
    public List<Command> commands;
    public List<Intercept> intercepts;
    public List<Intercept3d> intercept3ds;

    [Header("PF nodes")]
    public List<Transform> pfList = new List<Transform>();


    // Update is called once per frame
    void FixedUpdate()
    {
        if (commands.Count > 0) {
            if (commands[0].IsDone()) {
                StopAndRemoveCommand(0);
            } else {
                commands[0].Tick();
                commands[0].isRunning = true;
                DecorateAll();
            }
        }
    }

    void StopAndRemoveCommand(int index)
    {
        Command cmd = commands[index];
        commands.RemoveAt(index);

        if(cmd is Intercept3d) {//reverse inheritance order...
            Intercept3d intercept3d = (Intercept3d) cmd;
            intercept3d.Stop();
            intercept3ds.Remove(intercept3d);
        } else if(cmd is Intercept) {
            Intercept intercept = (Intercept) cmd;
            intercept.Stop();
            intercepts.Remove(intercept);
        } else if(cmd is Follow) {
            Follow follow = (Follow) cmd;
            follow.Stop();
            follows.Remove(follow);
        } else if(cmd is Move) {
            Move move = (Move)cmd;
            move.Stop();
            moves.Remove(move);
        } 

    }
    
    public void StopAndRemoveAllCommands()
    {
        for(int i = commands.Count - 1; i >= 0; i--) {
            StopAndRemoveCommand(i);
        }
    }

    public void AddCommand(Command c)
    {
        //print("Adding command; " + c.ToString());
        c.Init();
        commands.Add(c);
        if(c is Intercept3d)
            intercept3ds.Add(c as Intercept3d);
        else if(c is Intercept)
            intercepts.Add(c as Intercept);
        else if (c is Follow)
            follows.Add(c as Follow);
        else
            moves.Add(c as Move);
    }

    public void SetCommand(Command c)
    {
        //print("Setting command: " + c.ToString());
        StopAndRemoveAllCommands();
        commands.Clear();
        moves.Clear();
        intercepts.Clear();
        follows.Clear();
        intercept3ds.Clear();
        AddCommand(c);

    }
    //---------------------------------

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
            m.potentialLine.SetPosition(0, entity.position);
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
