using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelTowardsScreenSCript : MonoBehaviour
{
    Camera cameraVar;
    public GameObject worldCanvas;
    Vector3 distance;
    // Start is called before the first frame update
    void Start()
    {
        cameraVar = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        //distance = worldCanvas.transform.position - cameraVar.transform.position;
        //worldCanvas.transform.LookAt(distance);
        worldCanvas.transform.eulerAngles = new Vector3(0, cameraVar.transform.eulerAngles.y, 0);
    }
}
