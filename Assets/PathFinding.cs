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

                    int newMovementCost = currentNode.gCost + GetDistance(currentNode, neighbour);
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
        List<Vector3> waypoints = new List<Vector3>();
        foreach(Node node in path)
        {
            waypoints.Add(node.worldPosition);
        }
        return waypoints.ToArray();
    }

    int GetDistance(Node a, Node b)
    {
        int dstX = Mathf.Abs(a.gridX - b.gridX);
        int dstY = Mathf.Abs(a.gridY - b.gridY);
        return dstX > dstY ? 
            14*dstY + 10*(dstX-dstY) : 
            14*dstX + 10*(dstY-dstX);
    }
}