using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OceanGrid : MonoBehaviour
{
    public int gridWidth = 10;
    public int gridHeight = 10;
    public float cellSpacing = 1.0f;

    void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                Vector3 position = new Vector3(x * cellSpacing, 0, z * cellSpacing);
                Gizmos.DrawWireCube(transform.position + position, new Vector3(cellSpacing, 0, cellSpacing));
            }
        }
    }
}
