using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowHandler : MonoBehaviour
{
    GameObject handle;
    Vector3 pos;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    public static Vector3 GetScreenPositionFromWorldPosition(Vector3 targetPosition)
    {
        Vector3 screenPos = Camera.main.WorldToScreenPoint(targetPosition);
        return screenPos;
    }
}
