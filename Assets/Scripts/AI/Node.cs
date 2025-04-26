using UnityEngine;

public class Node : IHeapItem<Node>
{
    public bool walkable;
    public Vector3 worldPosition;
    public int gridX;
    public int gridY;
    public int gCost;
    public int hCost;
    public Node parent;
    private int heapIndex;

    public Node(bool walkable, Vector3 worldPos, int gridX, int gridY)
    {
        this.walkable = walkable;
        this.worldPosition = worldPos;
        this.gridX = gridX;
        this.gridY = gridY;
    }

    public int fCost => gCost + hCost;
    
    public int HeapIndex
    {
        get => heapIndex;
        set => heapIndex = value;
    }

    public int CompareTo(Node other)
    {
        int compare = fCost.CompareTo(other.fCost);
        return compare == 0 ? hCost.CompareTo(other.hCost) : -compare;
    }
}