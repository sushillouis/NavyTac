using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitAI : MonoBehaviour , IComparable<UnitAI>
{
    public Entity entity; //public only for ease of debugging
    // Start is called before the first frame update
    void Start()
    {
        entity = GetComponentInParent<Entity>();
        commands = new List<Command>();
        intercepts = new List<Intercept>();
        intercept3ds = new List<Intercept3d>();
        follows = new List<Follow>();
        moves = new List<Move>();
        _group = null;
    }
    public ShipRoles shipRole;

    public List<Move> moves;
    public List<Follow> follows;
    public List<Command> commands;
    public List<Intercept> intercepts;
    public List<Intercept3d> intercept3ds;
    public LineRenderer groupConnectingLine;
    public int preOrderOffset = 0;
    [SerializeField] Group _group;
    public Group group {
    get {return _group;} 
    set {
            Group temp = _group;
            _group = value;
            if(groupConnectingLine!=null && value == null) {
                Destroy(groupConnectingLine);
                groupConnectingLine=null;
            }
            if(temp is not null && temp != value) {
                temp.RemoveMember(this);
            } else if(value !=null && value.target != null && value.target != entity && groupConnectingLine == null) {
                Vector3[] points = new Vector3[2];
                points[0] = entity.position;
                points[1] = value.target.position;
                groupConnectingLine = LineMgr.inst.CreateColoredDashedLine(points,Color.cyan);
            }
        } 
    }

    public void HardSetGroup(Group n_Group) {
        _group = n_Group;
    }


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
        if (cmd is GroupEscort fmove)
        {
            fmove.Stop();
            moves.Remove(fmove);
        }

        if (cmd is GroupTargetMove gmove) {
            gmove.Stop();
            moves.Remove(gmove);
        }

        if (cmd is Move) {
            Move move = (Move)cmd;
            move.Stop();
            moves.Remove(move);
        }

        if(cmd is Intercept) {
            Intercept intercept = (Intercept)cmd;
            intercept.Stop();
            intercepts.Remove(intercept);
        }
        
        if(cmd is Intercept3d) {
            Intercept3d intercept3d = (Intercept3d)cmd;
            intercept3d.Stop();
            intercept3ds.Remove(intercept3d);
        }

        if(cmd is Follow){
            Follow follow = (Follow)cmd;
            follow.Stop();
            follows.Remove(follow);
        }
            
        commands.RemoveAt(index);


    }

    /// <summary>
    /// This function is called when the MonoBehaviour will be destroyed.
    /// </summary>
    void OnDestroy()
    {
        if(groupConnectingLine!=null) {
            LineMgr.inst.DestroyLR(groupConnectingLine);
            groupConnectingLine=null;
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

            } else if( current is Pincer) {
                Pincer pincer = current as Pincer;
                pincer.line.SetPosition(1, pincer.ComputePincerPoint());
                pincer.line.SetPosition(2, pincer.targetEntity.position);
            } else if (current is Follow && current is not GroupEscort) { // Less specific
                Follow f = current as Follow;
                f.line.SetPosition(1, f.targetEntity.position + f.offset);
                f.line.SetPosition(2, f.targetEntity.position);
                //f.line.SetPosition(1, f.predictedMovePosition);
            } 
            //Moveposition never changes
        }

        //potential fields lines
        if(!(current is Intercept) && !(current is Pincer) && AIMgr.inst.isPotentialFieldsMovement){ 
            if(current is GroupTargetMove gMove) {
                if(UIMgr.inst.displayPotentialLines) {
                    gMove.potentialLine.SetPosition(0, entity.position);
                    Vector3 newpos = Vector3.zero;
                    newpos.x = Mathf.Sin(entity.desiredHeading * Mathf.Deg2Rad) * entity.desiredSpeed;
                    newpos.z = Mathf.Cos(entity.desiredHeading * Mathf.Deg2Rad) * entity.desiredSpeed;
                    newpos *= 20;
                    newpos.y = 1;
                    gMove.potentialLine.SetPosition(1, entity.position + newpos);
                    gMove.potentialLine.gameObject.SetActive(entity.isSelected);
                } else {
                    gMove.potentialLine.gameObject.SetActive(false);
                }
            } else if(current is GroupEscort groupEscort) {
                if(UIMgr.inst.displayPotentialLines) {
                    groupEscort.potentialLine.SetPosition(0, entity.position);
                    Vector3 newpos = Vector3.zero;
                    newpos.x = Mathf.Sin(entity.desiredHeading * Mathf.Deg2Rad) * entity.desiredSpeed;
                    newpos.z = Mathf.Cos(entity.desiredHeading * Mathf.Deg2Rad) * entity.desiredSpeed;
                    newpos *= 20;
                    newpos.y = 1;
                    groupEscort.potentialLine.SetPosition(1, entity.position + newpos);
                    groupEscort.potentialLine.gameObject.SetActive(entity.isSelected);
                } else {
                    groupEscort.potentialLine.gameObject.SetActive(false);
                }
            } else {
                Move m = current as Move;
                if(UIMgr.inst.displayPotentialLines) {
                    m.potentialLine.SetPosition(0, entity.position);
                    Vector3 newpos = Vector3.zero;
                    newpos.x = Mathf.Sin(entity.desiredHeading * Mathf.Deg2Rad) * entity.desiredSpeed;
                    newpos.z = Mathf.Cos(entity.desiredHeading * Mathf.Deg2Rad) * entity.desiredSpeed;
                    newpos *= 20;
                    newpos.y = 1;
                    m.potentialLine.SetPosition(1, entity.position + newpos);
                    m.potentialLine.gameObject.SetActive(entity.isSelected);
                } else {
                    m.potentialLine.gameObject.SetActive(false);
                }
            }
        }
        // Group Connecting Line
        if(groupConnectingLine!=null && group!=null) {
            groupConnectingLine.gameObject.SetActive(entity.isSelected && UIMgr.inst.displayGroupLines);
            groupConnectingLine.SetPosition(0, entity.position);
            groupConnectingLine.SetPosition(1, group.target.position);
        }
 
    }

        // Default comparer for Part type.
    public int CompareTo(UnitAI compare)
    {
          // A null value means that this object is greater.
        if (compare == null)
            return 1;

        if(entity.mass == compare.entity.mass)
            return this.preOrderOffset < compare.preOrderOffset ? -1 : 1;

        else
            return entity.mass>compare.entity.mass ? -1 : 1;
    }
}
