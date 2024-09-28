using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioMgr : MonoBehaviour
{
    public static AudioMgr inst;


    public AudioMixer mixer;

    public bool muteSound;

    [Range(-80.0f, 20.0f)]
    public float masterVolume;
    [Range(-80.0f, 20.0f)]
    public float ambientVolume;
    [Range(-80.0f, 20.0f)]
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
        mixer.SetFloat("MasterVolume", masterVolume);
        mixer.SetFloat("AmbientVolume", ambientVolume);
        mixer.SetFloat("BGMVolume", bgmVolume);

        if (muteSound)
        {
            mixer.SetFloat("MasterVolume", -80);
        }
    }
}
