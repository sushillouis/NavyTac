using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Pathfinding : MonoBehaviour
{
    private Grid grid;
    public float maxPathCalculateTime = 0.1f;
    public int maxSearchNodes = 10000;

    void Awake()
    {
        grid = GetComponent<Grid>();
    }

    public void StartFindPath(Vector3 startPos, Vector3 targetPos, System.Action<Vector3[], bool> callback)
    {
        StartCoroutine(FindPath(startPos, targetPos, callback));
    }

    IEnumerator FindPath(Vector3 startPos, Vector3 targetPos, System.Action<Vector3[], bool> callback)
    {
        Vector3[] path = new Vector3[0];
        bool success = false;
        float startTime = Time.realtimeSinceStartup;

        Node startNode = grid.NodeFromWorldPoint(startPos);
        Node targetNode = grid.NodeFromWorldPoint(targetPos);

        if(startNode.walkable && targetNode.walkable)
        {
            Heap<Node> openSet = new Heap<Node>(grid.MaxSize);
            HashSet<Node> closedSet = new HashSet<Node>();
            openSet.Add(startNode);

            while(openSet.Count > 0)
            {
                if(Time.realtimeSinceStartup - startTime > maxPathCalculateTime ||
                   closedSet.Count > maxSearchNodes)
                {
                    yield break;
                }

                Node currentNode = openSet.RemoveFirst();
                closedSet.Add(currentNode);

                if(currentNode == targetNode)
                {
                    success = true;
                    break;
                }

                foreach(Node neighbour in grid.GetNeighbours(currentNode))
                {
                    if(!neighbour.walkable || closedSet.Contains(neighbour)) continue;

                    int newMovementCost = currentNode.gCost + GetDistance(currentNode, neighbour) + GetNodeSafetyCost(neighbour);
                    if(newMovementCost < neighbour.gCost || !openSet.Contains(neighbour))
                    {
                        neighbour.gCost = newMovementCost;
                        neighbour.hCost = GetDistance(neighbour, targetNode);
                        neighbour.parent = currentNode;

                        if(!openSet.Contains(neighbour))
                            openSet.Add(neighbour);
                    }
                }
            }
        }

        if(success)
        {
            path = RetracePath(startNode, targetNode);
        }
        callback(path, success);
    }

    Vector3[] RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;

        while(currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }
        path.Reverse();
        return SimplifyPath(path);
    }

    Vector3[] SimplifyPath(List<Node> path)
    {
        if (path == null || path.Count <= 2)
        {
            List<Vector3> simplePath = new List<Vector3>();
            foreach (Node node in path)
            {
                simplePath.Add(node.worldPosition);
            }
            return simplePath.ToArray();
        }

        List<Vector3> waypoints = new List<Vector3>();
        waypoints.Add(path[0].worldPosition); // Start point

        // Use funnel algorithm for better path smoothing
        for (int i = 1; i < path.Count - 1; i++)
        {
            Vector3 prev = path[i - 1].worldPosition;
            Vector3 current = path[i].worldPosition;
            Vector3 next = path[i + 1].worldPosition;

            // Calculate direction vectors
            Vector3 toPrev = (prev - current).normalized;
            Vector3 toNext = (next - current).normalized;

            // If the angle is too sharp, keep this waypoint
            float dot = Vector3.Dot(toPrev, toNext);
            if (dot < 0.7f) // ~45 degree threshold
            {
                waypoints.Add(current);
            }
            else
            {
                // For straight paths, try to find a center position
                Vector3 averagePos = (prev + current + next) / 3f;

                // Check if average position is walkable
                Node avgNode = grid.NodeFromWorldPoint(averagePos);
                if (avgNode != null && avgNode.walkable)
                {
                    waypoints.Add(averagePos);
                }
                else
                {
                    waypoints.Add(current);
                }
            }
        }

        waypoints.Add(path[path.Count - 1].worldPosition); // End point
        return waypoints.ToArray();
    }
    int GetNodeSafetyCost(Node node)
    {
        // Prefer nodes that are farther from obstacles
        int safetyBonus = 0;

        // Check neighbors to see if this is a "center" node
        var neighbors = grid.GetNeighbours(node);
        int walkableNeighbors = 0;

        foreach (Node neighbour in neighbors)
        {
            if (neighbour.walkable)
                walkableNeighbors++;
        }

        // Give bonus to nodes with more walkable neighbors (more in center)
        if (walkableNeighbors >= 6) // Most neighbors are walkable
            safetyBonus = -3; // Negative because lower cost is better
        else if (walkableNeighbors <= 3) // Few walkable neighbors (near edge)
            safetyBonus = 5;

        return safetyBonus;
    }


    int GetDistance(Node a, Node b)
    {
        int dstX = Mathf.Abs(a.gridX - b.gridX);
        int dstY = Mathf.Abs(a.gridY - b.gridY);
        return dstX > dstY ? 
            14*dstY + 10*(dstX-dstY) : 
            14*dstX + 10*(dstY-dstX);
    }

    // store the last computed path
    public Vector3[] gizmoPath;

    // draw it in the editor
   
}