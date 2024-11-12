using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioMgr : MonoBehaviour
{
    public static AudioMgr inst;


    public AudioMixer mixer;

    [Header("Selection")]
    public List<AudioClip> selectionSounds;
    public AudioSource selectionSource;

    [Header("Volume Control")]
    public bool muteSound;
    [Range(0, 1)]
    public float masterVolume;
    [Range(0, 1)]
    public float ambientVolume;
    [Range(0, 1)]
    public float bgmVolume;
    [Range(0, 1)]
    public float soundEffectsVolume;

    void Awake()
    {
        inst = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        mixer.SetFloat("MasterVolume", ScaleSound(masterVolume));
        mixer.SetFloat("AmbientVolume", ScaleSound(ambientVolume));
        mixer.SetFloat("BGMVolume", ScaleSound(bgmVolume));
        mixer.SetFloat("SoundEffectsVolume", ScaleSound(soundEffectsVolume));

        if (muteSound)
        {
            mixer.SetFloat("MasterVolume", -80);
        }
    }

    float scaleConst = 1 / (Mathf.Log10(1.1f) + 1);
    float ScaleSound(float sliderValue)
    {
        float input = scaleConst * (Mathf.Log10(sliderValue + 0.1f) + 1);
        return Mathf.Lerp(-80f, 0f, input);
    }

    public void PlaySelectionSound(Entity ent)
    {
        if(ent.shipClass != ShipClasses.None)
        {
            selectionSource.Stop();
            selectionSource.clip = selectionSounds[(int)ent.shipClass-1];
            selectionSource.Play();
        }
    }
}
