using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class ExplosionTest : MonoBehaviour
{
    public AudioSource sound;
    public VisualEffect effect;
    public bool play = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (play)
        {
            effect.Play();
            sound.Play();
            play = false;
        }
    }
}
