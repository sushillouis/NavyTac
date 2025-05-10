using UnityEngine;

public class PathRequestManager : MonoBehaviour
{
    private static PathRequestManager instance;
    private Pathfinding pathfinding;

    void Awake()
    {
        instance = this;
        pathfinding = GetComponent<Pathfinding>();
    }

    public static void RequestPath(Vector3 start, Vector3 end, System.Action<Vector3[], bool> callback)
    {
        instance.pathfinding.StartFindPath(start, end, callback);
    }
}