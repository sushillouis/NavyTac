using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public static class WeaponCSVHandler
{
    public static void DamageMatrixToCSV(List<WeaponDamage> weaponDamages, string fileNameCSV)
    {
        EntityType[] entityTypes = (EntityType[])Enum.GetValues(typeof(EntityType));
        Dictionary<EntityType, WeaponDamage> weaponDamageDict = new Dictionary<EntityType, WeaponDamage>();
        if (weaponDamages != null)
        {
            foreach (WeaponDamage wd in weaponDamages)
            {
                weaponDamageDict[wd.weaponType] = wd;
            }
        }

        StringBuilder csvContent = new StringBuilder();
        csvContent.Append("WeaponType");
        foreach (EntityType targetType in entityTypes)
        {
            csvContent.Append(",");
            csvContent.Append(targetType.ToString());
        }

        csvContent.AppendLine();

        foreach (EntityType weaponType in entityTypes)
        {
            
            csvContent.Append(weaponType.ToString());
            WeaponDamage weaponDamage;
            if (weaponDamageDict.ContainsKey(weaponType))
            {
                weaponDamage = weaponDamageDict[weaponType];
            }
            else
            {
                weaponDamage = new WeaponDamage
                {
                    weaponType = weaponType,
                    targetDamages = new List<TargetDamage>()
                };
            }

            Dictionary<EntityType, float> targetDamageDict = new Dictionary<EntityType, float>();
            if (weaponDamage.targetDamages != null)
            {
                foreach (TargetDamage td in weaponDamage.targetDamages)
                {
                    targetDamageDict[td.targetType] = td.damageValue;
                }
            }

            foreach (EntityType targetType in entityTypes)
            {
                csvContent.Append(",");
                if (targetDamageDict.ContainsKey(targetType))
                {
                    csvContent.Append(targetDamageDict[targetType].ToString());
                }
                else
                {
                    csvContent.Append("0");
                }
            }

            csvContent.AppendLine();
        }

        string filePath = Application.dataPath + "/" + fileNameCSV;
        File.WriteAllText(filePath, csvContent.ToString());
        //Debug.Log("Damage matrix saved to " + filePath);
    }

    public static List<WeaponDamage> CSVToDamageMatrix(string filePath)
    {
        if (!File.Exists(filePath))
        {
            //Debug.LogError("Damage matrix CSV file not found at " + filePath);
            return null;
        }

        string[] lines = File.ReadAllLines(filePath);
        if (lines.Length < 2)
        {
            //Debug.LogError("Damage matrix CSV file is empty or improperly formatted.");
            return null;
        }

        List<WeaponDamage> weaponDamages = new List<WeaponDamage>();
        string headerLine = lines[0];
        string[] headers = headerLine.Split(',');

        List<EntityType> targetTypes = new List<EntityType>();
        for (int i = 1; i < headers.Length; i++)
        {
            try
            {
                EntityType targetType = (EntityType)Enum.Parse(typeof(EntityType), headers[i]);
                targetTypes.Add(targetType);
            }
            catch (Exception e)
            {
                //Debug.LogError("Invalid EntityType in header: " + headers[i] + ". Error: " + e.Message);
                return null;
            }
        }

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            string[] values = line.Split(',');

            if (values.Length != headers.Length)
            {
                //Debug.LogError("Line " + (i + 1) + " is improperly formatted.");
                continue;
            }

            EntityType weaponType;
            try
            {
                weaponType = (EntityType)Enum.Parse(typeof(EntityType), values[0]);
            }
            catch (Exception e)
            {
                //Debug.LogError("Invalid EntityType in weaponType at line " + (i + 1) + ": " + values[0] + ". Error: " + e.Message);
                continue;
            }

            WeaponDamage weaponDamage = new WeaponDamage
            {
                weaponType = weaponType,
                targetDamages = new List<TargetDamage>()
            };

            for (int j = 1; j < values.Length; j++)
            {
                EntityType targetType = targetTypes[j - 1];
                float damageValue;
                if (float.TryParse(values[j], out damageValue))
                {
                    TargetDamage targetDamage = new TargetDamage
                    {
                        targetType = targetType,
                        damageValue = damageValue
                    };
                    weaponDamage.targetDamages.Add(targetDamage);
                }
                else
                {
                    //Debug.LogError("Invalid damage value at line " + (i + 1) + ", column " + (j + 1) + ": " + values[j]);
                }
            }

            weaponDamages.Add(weaponDamage);
        }

        //Debug.Log("Damage matrix loaded from " + filePath);
        return weaponDamages;
    }
}