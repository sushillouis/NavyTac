using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WeaponDamage
{
    public EntityType weaponType;
    public List<TargetDamage> targetDamages;
}

[System.Serializable]
public class TargetDamage
{
    public EntityType targetType;
    public float damageValue;
}