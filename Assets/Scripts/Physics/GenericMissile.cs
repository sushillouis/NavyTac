using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface GenericMissile
{
    public abstract void Init(TactPlayer player); 
    public abstract void Explode();
    public Vector3 position {get;}
    public Vector3 velocity {get;set;}
    public TactPlayer team {get;set;}
    public delegate void MissileDieEventMethod(GenericMissile ent);
    public event MissileDieEventMethod OnDieEvent;
}
