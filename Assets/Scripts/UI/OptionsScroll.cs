using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OptionsScroll : MonoBehaviour
{
    [SerializeField] Scrollbar scrollbar;
    public Transform content;
    public Transform viewPort;
    int dir = 1;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        scrollbar.value+=Time.deltaTime*.5f*dir;
        if(scrollbar.value>=1 || scrollbar.value<=0) {
            dir*=-1;
        }
    }
}
