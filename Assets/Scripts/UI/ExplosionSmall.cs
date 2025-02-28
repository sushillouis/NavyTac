using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class ExplosionSmall : MonoBehaviour
{
    public AudioSource sound;
    public VisualEffect effect;
    public bool play = false;
    public void SetExplosionInterval(float interval) {
        effect.SetFloat("TimeScale", interval);
    }

    public void Explode() {
        effect.Play();
        sound.Play();
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
