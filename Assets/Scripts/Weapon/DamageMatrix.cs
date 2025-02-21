using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
public class DamageMatrix
{
    [SerializeField] private float defaultDamage = 1f;

    private Dictionary<EntityType, Dictionary<EntityType, float>> damageMatrix;

    public void InitializeDamageMatrix()
    {
        damageMatrix = new Dictionary<EntityType, Dictionary<EntityType, float>>();
        foreach (var weaponDamage in WeaponsMgr.inst.weaponDamages)
        {
            var targetDict = new Dictionary<EntityType, float>();
            foreach (var targetDamage in weaponDamage.targetDamages)
            {
                targetDict[targetDamage.targetType] = targetDamage.damageValue;
            }
            damageMatrix[weaponDamage.weaponType] = targetDict;
        }
    }

    public float GetDamage(EntityType weaponType, EntityType targetType)
    {
        if (damageMatrix.TryGetValue(weaponType, out var targetDict))
        {
            if (targetDict.TryGetValue(targetType, out float damage))
            {
                return damage;
            }
        }
        return defaultDamage;
    }
}