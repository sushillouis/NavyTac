using System.Collections.Generic;
using UnityEngine;

public class DamageMatrix
{
    public float defaultDamage = 0f;
    private Dictionary<EntityType, Dictionary<EntityType, float>> damageMatrix;

    public void InitializeDamageMatrix(List<WeaponDamage> weaponDamages)
    {
        if (weaponDamages == null || weaponDamages.Count == 0)
        {
            Debug.LogError("WeaponDamages list is null or empty!");
            damageMatrix = new Dictionary<EntityType, Dictionary<EntityType, float>>(); // Initialize empty to prevent NRE
            return;
        }

        damageMatrix = new Dictionary<EntityType, Dictionary<EntityType, float>>();
        foreach (var weaponDamage in weaponDamages)
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
        if (damageMatrix == null)
        {
            Debug.LogError("DamageMatrix dictionary not initialized!");
            return defaultDamage;
        }

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