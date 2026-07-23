using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UnityEngine.UI.Slider))]
public class LoadingScreen : MonoBehaviour
{
    public float sliderValue = 0f;
    public float fillSpeed = 0.1f; // Adjust this value for speed

    public UnityEngine.UI.Slider rotationSpeedSlider;

    void Awake()
    {
        rotationSpeedSlider = GetComponent<UnityEngine.UI.Slider>();
       
    }
    void Update()
    {
        rotationSpeedSlider.value = 0.45f;

        // Rotate the GameObject based on sliderValue
        transform.Rotate(Vector3.forward, 360f * -sliderValue * Time.unscaledDeltaTime);
        
    }
}

