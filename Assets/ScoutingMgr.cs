using System.Collections.Generic;
using UnityEngine;

public class MultiEntityScout : MonoBehaviour
{
    public float mapSize = 500f;
    public int gridCountPerSide = 5; // Set this to 5 for a 5x5 grid
    public List<Entity> selectedEntities;

    void Start()
    {
        
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("Key Pressed");
            selectedEntities = SelectionMgr.inst.selectedEntities;
            AssignEntitiesToScout();
        }
    }

    // Main method to assign scouting task to each entity
    void AssignEntitiesToScout()
    {
        if (selectedEntities.Count == 0) return;

        foreach (Entity entity in selectedEntities)
        {
            UnitAI unitAI = entity.GetComponent<UnitAI>();
            unitAI.StopAndRemoveAllCommands();

            // Get path through all quadrants and grids in a smooth, continuous fashion
            List<Vector3> scoutingPath = GetFullScoutingPath();

            // Assign path commands
            foreach (Vector3 gridPosition in scoutingPath)
            {
                unitAI.AddCommand(new Move(entity, gridPosition));
            }

            // Return entity to the center after scouting
            unitAI.AddCommand(new Move(entity, Vector3.zero));
        }
    }

    // Get the complete path for visiting all quadrants and their grids in one continuous path
    List<Vector3> GetFullScoutingPath()
    {
        List<Vector3> path = new List<Vector3>();

        // Traverse the full map, including all quadrants, as a continuous grid
        for (int quadrant = 0; quadrant < 4; quadrant++)
        {
            // Get the grids in each quadrant but treat the path as a single, continuous grid
            List<Vector3> gridsInQuadrant = GetGridsInQuadrant(quadrant, gridCountPerSide);

            // Add the grids to the overall path
            path.AddRange(gridsInQuadrant);
        }

        return path;
    }

    // Get all grid positions within a specific quadrant, optimizing for smooth continuous traversal
    List<Vector3> GetGridsInQuadrant(int quadrant, int gridCountPerSide)
    {
        List<Vector3> grids = new List<Vector3>();

        // Calculate the size of each quadrant and grid
        float halfMapSize = mapSize / 2;
        float gridSize = halfMapSize / gridCountPerSide;

        // Determine the bottom-left corner of the quadrant based on which one it is
        Vector3 bottomLeftCorner;
        switch (quadrant)
        {
            case 0: // Top-right quadrant
                bottomLeftCorner = new Vector3(0, 0, 0);
                break;
            case 1: // Bottom-right quadrant
                bottomLeftCorner = new Vector3(0, 0, -halfMapSize);
                break;
            case 2: // Bottom-left quadrant
                bottomLeftCorner = new Vector3(-halfMapSize, 0, -halfMapSize);
                break;
            case 3: // Top-left quadrant
                bottomLeftCorner = new Vector3(-halfMapSize, 0, 0);
                break;
            default:
                bottomLeftCorner = Vector3.zero;
                break;
        }

        // Create a smooth snaking pattern by visiting each row of grids
        for (int x = 0; x < gridCountPerSide; x++)
        {
            // Determine if this row should be traversed left-to-right or right-to-left (to form a smooth box-like pattern)
            if (x % 2 == 0)
            {
                // Move left to right
                for (int z = 0; z < gridCountPerSide; z++)
                {
                    Vector3 gridPosition = bottomLeftCorner + new Vector3(x * gridSize, 0, z * gridSize);
                    grids.Add(gridPosition);
                }
            }
            else
            {
                // Move right to left (zig-zag pattern)
                for (int z = gridCountPerSide - 1; z >= 0; z--)
                {
                    Vector3 gridPosition = bottomLeftCorner + new Vector3(x * gridSize, 0, z * gridSize);
                    grids.Add(gridPosition);
                }
            }
        }

        return grids;
    }
}
