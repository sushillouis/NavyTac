using System.Collections.Generic;
using UnityEngine;

public class NoGoZoneManager : MonoBehaviour
{

    public static List<NoGoZoneBounds> Zones = new List<NoGoZoneBounds>();


    public static void Register(NoGoZoneBounds zone) => Zones.Add(zone);
    public static void Unregister(NoGoZoneBounds zone) => Zones.Remove(zone);

    

    // Check if a point is within any NoGoZone
}