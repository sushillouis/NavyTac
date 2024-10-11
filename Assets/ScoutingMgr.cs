using System.Collections.Generic;
using UnityEngine;

public class MultiEntityScout : MonoBehaviour
{
    public float mapSize = 500f;
    public List<Entity> selectedEntities;
    public LineRenderer lineRenderer;

    private List<Vector3> quadrantCenters;

    void Start()
    {
        quadrantCenters = GetQuadrantCenters();
        DrawQuadrantLines();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("Key Pressed");
            selectedEntities = SelectionMgr.inst.selectedEntities;
            AssignEntitiesToQuadrantsAndScout();
        }
    }

    List<Vector3> GetQuadrantCenters()
    {
        return new List<Vector3>
        {
            new Vector3(mapSize / 2, 0, mapSize / 2),
            new Vector3(mapSize / 2, 0, -mapSize / 2),
            new Vector3(-mapSize / 2, 0, -mapSize / 2),
            new Vector3(-mapSize / 2, 0, mapSize / 2)
        };
    }

    void DrawQuadrantLines()
    {
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        lineRenderer.positionCount = 5;
        lineRenderer.loop = true;
        lineRenderer.startWidth = 0.2f;
        lineRenderer.endWidth = 0.2f;

        Vector3[] points =
        {
            new Vector3(mapSize / 2, 0, mapSize / 2),
            new Vector3(mapSize / 2, 0, -mapSize / 2),
            new Vector3(-mapSize / 2, 0, -mapSize / 2),
            new Vector3(-mapSize / 2, 0, mapSize / 2),
            new Vector3(mapSize / 2, 0, mapSize / 2)
        };

        lineRenderer.SetPositions(points);
    }

    void AssignEntitiesToQuadrantsAndScout()
    {
        if (selectedEntities.Count == 0) return;

        int totalEntities = selectedEntities.Count;
        int entityIndex = 0;

        foreach (Entity entity in selectedEntities)
        {
            UnitAI unitAI = entity.GetComponent<UnitAI>();
            unitAI.StopAndRemoveAllCommands();
            Vector3 originalPosition = entity.position;

            List<Vector3> targetQuadrants = GetTargetQuadrantsForEntity(entityIndex, totalEntities);

            foreach (Vector3 targetQuadrant in targetQuadrants)
            {
                Vector3 initialMovePosition = GetInitialMovePosition(entityIndex);
                Vector3 finalMovePosition = GetFinalMovePosition(entityIndex);

                unitAI.AddCommand(new Move(entity, initialMovePosition));
                unitAI.AddCommand(new Move(entity, targetQuadrant));
                unitAI.AddCommand(new Move(entity, finalMovePosition));
            }

            unitAI.AddCommand(new Move(entity, originalPosition));
            entityIndex++;
        }
    }

    List<Vector3> GetTargetQuadrantsForEntity(int entityIndex, int totalEntities)
    {
        List<Vector3> targetQuadrants = new List<Vector3>();
        int quadrantsCount = quadrantCenters.Count;
        targetQuadrants.Clear();
        int quadrantsPerEntity = Mathf.CeilToInt((float)quadrantsCount / totalEntities);

        for (int i = 0; i < quadrantsPerEntity; i++)
        {
            int quadrantIndex = entityIndex * quadrantsPerEntity + i;
            if (quadrantIndex < quadrantsCount)
            {
                targetQuadrants.Add(quadrantCenters[quadrantIndex]);
            }
        }

        return targetQuadrants;
    }

    Vector3 GetInitialMovePosition(int index)
    {
        switch (index % quadrantCenters.Count)
        {
            case 0: return new Vector3(0, 0, mapSize / 2);
            case 1: return new Vector3(mapSize / 2, 0, 0);
            case 2: return new Vector3(0, 0, -mapSize / 2);
            case 3: return new Vector3(-mapSize / 2, 0, 0);
            default: return Vector3.zero;
        }
    }

    Vector3 GetFinalMovePosition(int index)
    {
        switch (index % quadrantCenters.Count)
        {
            case 0: return new Vector3(mapSize / 2, 0, 0);
            case 1: return new Vector3(0, 0, -mapSize / 2);
            case 2: return new Vector3(-mapSize / 2, 0, 0);
            case 3: return new Vector3(0, 0, mapSize / 2);
            default: return Vector3.zero;
        }
    }
}
