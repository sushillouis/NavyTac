using UnityEngine;
using UnityEngine.UI;

public class ScrollbarSliderSync : MonoBehaviour

{

    public Scrollbar scrollbar;

    public Slider slider;



    void Update()

    {

        // Update slider value based on scrollbar position

        scrollbar.value = slider.value; 

    }

}
