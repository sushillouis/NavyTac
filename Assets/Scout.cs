using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiEntityScout : MonoBehaviour
{
    public float mapSize = 500f;
    private List<Vector3> quadrantCenters; // Center points of the quadrants
    public List<Entity> selectedEntities; // List of selected entities

    void Start()
    {
        quadrantCenters = GetQuadrantCenters();
    }

    // Update is called once per frame
    void Update()
    {
        // If the 'P' key is pressed, start the scouting mission
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("Key Pressed");
            selectedEntities = SelectionMgr.inst.selectedEntities;
            AssignEntitiesToQuadrantsAndScout(); // Call to start scouting
        }
    }

    List<Vector3> GetQuadrantCenters()
    {
        List<Vector3> centers = new List<Vector3>();

        // Calculate the center of each quadrant
        centers.Add(new Vector3(mapSize / 2, 0, mapSize / 2));    // Quadrant 1 (Top-Right)
        centers.Add(new Vector3(mapSize / 2, 0, -mapSize / 2));   // Quadrant 2 (Bottom-Right)
        centers.Add(new Vector3(-mapSize / 2, 0, -mapSize / 2));  // Quadrant 3 (Bottom-Left)
        centers.Add(new Vector3(-mapSize / 2, 0, mapSize / 2));   // Quadrant 4 (Top-Left)

        return centers;
    }

    void AssignEntitiesToQuadrantsAndScout()
    {
        int numberOfEntities = selectedEntities.Count;

        if (numberOfEntities == 0) return; // If no entities are selected, exit early

        for (int i = 0; i < numberOfEntities; i++)
        {
            Entity entity = selectedEntities[i];
            UnitAI unitAI = entity.GetComponent<UnitAI>();

            // Check if the entity is already scouting
            if (entity.isScouting)
            {
                Debug.Log($"Entity {entity.name} is already scouting. Skipping.");
                continue; // Skip to the next entity if it's already scouting
            }

            // Set the entity as scouting
            entity.isScouting = true;

            // Determine the quadrants for this entity
            List<Vector3> targetQuadrants = GetTargetQuadrants(i, numberOfEntities);

            // Move to each assigned quadrant
            foreach (Vector3 targetQuadrant in targetQuadrants)
            {
                Move moveToQuadrant = new Move(entity, targetQuadrant);
                unitAI.AddCommand(moveToQuadrant);
            }

            // Return to the original position
            Vector3 originalPosition = entity.position;
            Move returnToOriginal = new Move(entity, originalPosition);
            unitAI.AddCommand(returnToOriginal);

            // Optionally reset isScouting after returning
            // if (returnToOriginal.IsDone())
            // {
            //     entity.isScouting = false;
            // }
        }
    }

    List<Vector3> GetTargetQuadrants(int entityIndex, int totalEntities)
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
}
