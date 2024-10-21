using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AmmoBar : MonoBehaviour
{
    
    [SerializeField] private Image ammoBar;
    [SerializeField] private float reduceSpeed = 2;
    private float targetSpeed = 1;
    
    public void UpdateAmmoBar(float maxAmmo, float currentAmmo)
    {
        targetSpeed = currentAmmo / maxAmmo;
    }

    void Update()
    {
        ammoBar.fillAmount = Mathf.MoveTowards(ammoBar.fillAmount, targetSpeed, reduceSpeed * Time.deltaTime);
    }
}
