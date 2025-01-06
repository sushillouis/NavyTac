using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PIDAspect : MonoBehaviour {
    public abstract PIDController GetController();
    public abstract void SetTarget(int index);
    public abstract float power { get; set; }
}