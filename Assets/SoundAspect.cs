using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public class SoundAspect : MonoBehaviour
{
    public Entity entity;
    // Start is called before the first frame update
    void Start()
    {
        entity = GetComponentInParent<Entity>();
        boatMove.volume = 0;
    }

    public AudioSource boatMove;
    bool boatFadeIn = false;
    bool boatFadeOut = false;

    public AudioSource selection;

    // Update is called once per frame
    void Update()
    {
        if (entity.speed > Utils.EPSILON && !boatMove.isPlaying)
        {
            boatMove.Play();
            boatFadeIn = true;
            boatFadeOut = false;
            fadeTimer = 0;
            fadeInStart = boatMove.volume;
        }
        if (entity.speed <= Utils.EPSILON && boatMove.isPlaying)
        {
            if (!boatFadeOut)
            {
                fadeOutStart = boatMove.volume;
                fadeTimer = 0;
            }   
            boatFadeOut = true;
            boatFadeIn = false;
            
        }
        if (boatFadeIn)
            FadeIn(boatMove);
        if (boatFadeOut)
            FadeOut(boatMove);
    }

    float fadeTimer = 0;
    public float fadeInStart;
    public void FadeIn(AudioSource sound)
    {
        sound.volume = Mathf.Lerp(fadeInStart, 1, fadeTimer/5f);
        fadeTimer += Time.deltaTime;
        if(fadeTimer > 5)
        {
            fadeTimer = 0;
            boatFadeIn = false;
        }
    }

    public float fadeOutStart;
    public void FadeOut(AudioSource sound)
    {
        sound.volume = Mathf.Lerp(fadeOutStart, 0, fadeTimer / 1f);
        fadeTimer += Time.deltaTime;
        if (fadeTimer > 1)
        {
            fadeTimer = 0;
            boatFadeOut = false;
            sound.Stop();
        }
    }

    public void PlaySelection()
    {
        if(!selection.isPlaying)
            selection.Play();

        Debug.Log("ran");
    }
}
