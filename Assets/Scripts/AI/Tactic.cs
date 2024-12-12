using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Tactic 
{
    public TacticsType tacticsType;
    public Entity highValueUnit;
    public List<Entity> entities;

    public Tactic(List<Entity> ents) {
        entities = ents;
        highValueUnit = null;
        tacticsType = TacticsType.Cancel;
    }

    public virtual void Init() {
    
    }

    public virtual void Tick() {
    }

    public virtual void Stop() {
    }


    public virtual bool IsDone() {
        return false;
    }

}
