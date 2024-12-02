using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BarFillAnimator : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        panelImage = transform.GetComponent<Image>();
        timeCountDown = timeInterval;
        fillAmount = 0;
        fillOrigin = panelImage.fillOrigin;
    }
    private Image panelImage;

    [SerializeField]private float timeInterval = 3f;
    private float fillAmount = 0;
    private int fillOrigin;
    bool fillDirectionLToR = true;
    private float timeCountDown;
    void Update()
    {

        panelImage.fillAmount = fillAmount;

        timeCountDown -= Time.deltaTime;
        if(timeCountDown < 0) {
            timeCountDown = timeInterval;
            fillDirectionLToR = !fillDirectionLToR;
            panelImage.color = ColorPalette.inst.colors[Random.Range(0, 20)];
            panelImage.fillOrigin = 1 - fillOrigin;
        }

        if(fillDirectionLToR)
            fillAmount = Mathf.Clamp01((timeInterval - timeCountDown) / timeInterval);
        else
            fillAmount = Mathf.Clamp01(timeCountDown / timeInterval);



/*
        if(fillDirectionLToR)
            fillAmount += (timeInterval - timeCountDown) / timeInterval;
        else
            fillAmount -= timeCountDown / timeInterval;
*/
    }
}
