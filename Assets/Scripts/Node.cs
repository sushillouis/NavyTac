using UnityEngine;
using System;

public class Node  : IHeapItem<Node>, IComparable<Node> {
    public bool walkable;
    public Vector3 worldPosition;
    public int gridX;
    public int gridZ;
    
    public int gCost;
    public int hCost;
    public Node parent;
    private int heapIndex;

    public Node(bool walkable, Vector3 worldPos, int gridX, int gridZ) {
        this.walkable = walkable;
        this.worldPosition = worldPos;
        this.gridX = gridX;
        this.gridZ = gridZ;
    }

    public int fCost => gCost + hCost;

    public int HeapIndex {
        get => heapIndex;
        set => heapIndex = value;
    }

    public int CompareTo(Node other) {
        int compare = fCost.CompareTo(other.fCost);
        if (compare == 0) compare = hCost.CompareTo(other.hCost);
        return -compare;
    }
}