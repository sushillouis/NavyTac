using System.Collections.Generic;
using UnityEngine;

public class QuadrantManager : MonoBehaviour
{

    public static List<QuadrantBounds> Zones = new List<QuadrantBounds>();


    public static void Register(QuadrantBounds zone) => Zones.Add(zone);
    public static void Unregister(QuadrantBounds zone) => Zones.Remove(zone);

}