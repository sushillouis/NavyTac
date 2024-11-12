using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioMgr : MonoBehaviour
{
    public static AudioMgr inst;


    public AudioMixer mixer;

    public bool muteSound;

    [Range(0, 1)]
    public float masterVolume;
    [Range(0, 1)]
    public float ambientVolume;
    [Range(0, 1)]
    public float bgmVolume;

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
}
