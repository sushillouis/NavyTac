using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public static class Utils {


    public static float EPSILON = 0.01f;
    public static float ToNautialMiles = 0.000539957f;
    public static float FromNauticalMiles = 1852f;
    public const float GRAVITY = 9.8f;
    public const float DRAG_COEFFICENT = 0.04f;
    public const float ANGULAR_DRAG_COEFFICENT = 0.02f;
    public static Vector3 flat = new(1,0,1);
    public static bool ApproximatelyEqual(float a, float b)
    {
        return (Mathf.Abs(a - b) < EPSILON);
    }

    public static float Clamp(float val, float min, float max)
    {
        if (val < min)
            val = min;
        if (val > max)
            val = max;
        return val;
    }

    public static float AngleDiffPosNeg(float a, float b)
    {
        float diff = a - b;
        if (diff > 180)
            return diff - 360;
        if (diff < -180)
            return diff + 360;
        return diff;
    }

    public static float Degrees360(float angleDegrees)
    {
        while (angleDegrees >= 360)
            angleDegrees -= 360;
        while (angleDegrees < 0)
            angleDegrees += 360;
        return angleDegrees;

    }

    public static float VectorToHeadingDegrees(Vector3 v)
    {
        return Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg;
    }

    public static void CPA(Entity e1, Entity e2)
    {


    }

    /// <summary>
    /// From https://stackoverflow.com/questions/5796383/insert-spaces-between-words-on-a-camel-cased-token
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string SplitCamelCase(string str) {
        return Regex.Replace(
            Regex.Replace(
                str,
                @"(\P{Ll})(\P{Ll}\p{Ll})",
                "$1 $2"
            ),
            @"(\p{Ll})(\P{Ll})",
            "$1 $2"
        );
    }

    public static readonly Dictionary<EntityType, int> costDict = new Dictionary<EntityType, int>{
        {EntityType.DDG51,50},
        {EntityType.Container,30},
        {EntityType.MineSweeper,20},
        {EntityType.OilServiceVessel,17},
        {EntityType.OrientExplorer,17},
        {EntityType.PilotVessel,10},
        {EntityType.SmitHouston,20},
        {EntityType.Tanker,25},
        {EntityType.TugBoat,15},
        {EntityType.JARIUSV,25},
        {EntityType.SeaHunter,20},
        {EntityType.Mykola,10},
        {EntityType.SeaBaby,5},
        {EntityType.CVN75,100},
        {EntityType.Submarine,40},
        {EntityType.AntiShipMissile,1},
        {EntityType.Plane,15}
    };

    public static readonly Dictionary<EntityType, int> strengthDict = new Dictionary<EntityType, int>{
        {EntityType.DDG51,50},
        {EntityType.Container,0},
        {EntityType.MineSweeper,0},
        {EntityType.OilServiceVessel,0},
        {EntityType.OrientExplorer,0},
        {EntityType.PilotVessel,5},
        {EntityType.SmitHouston,0},
        {EntityType.Tanker,0},
        {EntityType.TugBoat,0},
        {EntityType.JARIUSV,25},
        {EntityType.SeaHunter,20},
        {EntityType.Mykola,10},
        {EntityType.SeaBaby,5},
        {EntityType.CVN75,100},
        {EntityType.Submarine,40},
        {EntityType.AntiShipMissile,1},
        {EntityType.Plane,15}
    };

    public static readonly Dictionary<(WeaponType, EntityClass), float> weaponDamageScalar = new() {
        {(WeaponType.AAMissle,EntityClass.Airplane), 2f},
        {(WeaponType.AAMissle,EntityClass.Destroyer), .1f},
    };

}
