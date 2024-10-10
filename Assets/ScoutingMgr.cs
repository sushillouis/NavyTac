using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiEntityScout : MonoBehaviour
{
    public float mapSize = 500f;
    public List<Entity> selectedEntities;

    private List<Vector3> zoneCenters; // Centers of dynamically generated zones
    private int currentZoneCount;

    void Start()
    {
        currentZoneCount = 4; // Minimum of four zones
        // Render zones initially
    }

    void Update()
    {
        
        if (Input.GetKeyDown(KeyCode.P))
        {
            zoneCenters = new List<Vector3>(); // Initial empty list for zone centers
            RenderZones(); 
            Debug.Log("Key Pressed");
            selectedEntities = SelectionMgr.inst.selectedEntities;
            AssignEntitiesToZonesAndScout();
        }
    }

    void AssignEntitiesToZonesAndScout()
    {
        int numberOfEntities = selectedEntities.Count;

        if (numberOfEntities == 0) return;

        // Always have a minimum of four zones
        currentZoneCount = Mathf.Max(4, numberOfEntities);
        zoneCenters = GetDynamicZoneCenters(currentZoneCount);

        Vector3 centerPosition = Vector3.zero; // Central point of the map

        for (int i = 0; i < numberOfEntities; i++)
        {
            Entity entity = selectedEntities[i];
            UnitAI unitAI = entity.GetComponent<UnitAI>();
            unitAI.StopAndRemoveAllCommands();

            // Assign entity to scout its assigned zones
            List<Vector3> targetZones = GetTargetZones(i, numberOfEntities);
            foreach (Vector3 targetZone in targetZones)
            {
                Move moveToZone = new Move(entity, targetZone);
                unitAI.AddCommand(moveToZone);
            }

            // Return to the original position
            Vector3 originalPosition = entity.position;
            Move returnToOriginal = new Move(entity, originalPosition);
            unitAI.AddCommand(returnToOriginal);
        }
    }

    List<Vector3> GetDynamicZoneCenters(int numberOfZones)
    {
        List<Vector3> centers = new List<Vector3>();

        // Divide the map into a grid with at least 4 zones
        int rows = Mathf.CeilToInt(Mathf.Sqrt(numberOfZones));
        int columns = Mathf.CeilToInt((float)numberOfZones / rows);
        float zoneWidth = mapSize / columns;
        float zoneHeight = mapSize / rows;

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                if (centers.Count >= numberOfZones)
                    break;

                float xOffset = (column * zoneWidth) - (mapSize / 2) + (zoneWidth / 2);
                float zOffset = (row * zoneHeight) - (mapSize / 2) + (zoneHeight / 2);
                centers.Add(new Vector3(xOffset, 0, zOffset));
            }
        }

        return centers;
    }

    List<Vector3> GetTargetZones(int entityIndex, int totalEntities)
    {
        List<Vector3> targetZones = new List<Vector3>();

        // Calculate zones for each entity, ensuring all entities are assigned evenly
        int zonesPerEntity = Mathf.CeilToInt((float)currentZoneCount / totalEntities);

        for (int i = 0; i < zonesPerEntity; i++)
        {
            int zoneIndex = (entityIndex * zonesPerEntity + i) % currentZoneCount;
            targetZones.Add(zoneCenters[zoneIndex]);
        }

        return targetZones;
    }

    void RenderZones()
    {
        zoneCenters = GetDynamicZoneCenters(currentZoneCount);

        foreach (var zoneCenter in zoneCenters)
        {
            CreateZoneRenderer(zoneCenter);
        }
    }

    void CreateZoneRenderer(Vector3 center)
    {
        float zoneWidth = mapSize / Mathf.CeilToInt(Mathf.Sqrt(currentZoneCount));
        float zoneHeight = mapSize / Mathf.CeilToInt((float)currentZoneCount / Mathf.CeilToInt(Mathf.Sqrt(currentZoneCount)));

        Vector3[] corners = new Vector3[5];
        corners[0] = new Vector3(center.x - zoneWidth / 2, 0, center.z - zoneHeight / 2);
        corners[1] = new Vector3(center.x + zoneWidth / 2, 0, center.z - zoneHeight / 2);
        corners[2] = new Vector3(center.x + zoneWidth / 2, 0, center.z + zoneHeight / 2);
        corners[3] = new Vector3(center.x - zoneWidth / 2, 0, center.z + zoneHeight / 2);
        corners[4] = corners[0]; // Close the loop

        GameObject lineObj = new GameObject("Zone Border");
        LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();

        lineRenderer.startWidth = 2f;
        lineRenderer.endWidth = 2f;
        lineRenderer.positionCount = corners.Length;
        lineRenderer.useWorldSpace = true;

        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.green;
        lineRenderer.endColor = Color.green;

        lineRenderer.SetPositions(corners);
    }
}