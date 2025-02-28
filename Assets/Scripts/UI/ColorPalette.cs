using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorPalette : MonoBehaviour
{
    public static ColorPalette inst { get; private set; }
    private void Awake() {
        inst = this;
    }

    public List<Color> colors = new List<Color>();


}
