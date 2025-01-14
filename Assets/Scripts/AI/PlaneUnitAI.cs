using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneUnitAI : UnitAI
{
    private void Awake() {
        entity = GetComponentInParent<PlaneEntity>();
        entity.ai = this;
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
        } else {
            entity.desiredSpeed=0f;
        }
    }

    public override void AddCommand(Command c)
    {
        if(c is Intercept3d)
            intercept3ds.Add(c as Intercept3d);
        else if(c is Intercept)
            intercepts.Add(c as Intercept);
        else if (c is Follow)
            follows.Add(c as Follow);
        else if (c is Move cMove) {
            c = new PlaneMove(cMove.entity,cMove.movePosition);
            moves.Add(c as Move);
        }
        commands.Add(c);
        c.Init();
    }

    public override void DecorateAll()
    {
        Command prior = null;
        foreach(Command c in commands) {
            Decorate(prior, c);
            prior = c;
        }
    }

    public override void Decorate(Command prior, Command current)
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
    }

}
