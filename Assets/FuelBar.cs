using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FuelBar : MonoBehaviour
{
    [SerializeField] private Image fuelBar;
    [SerializeField] private float reduceSpeed = 2;
    private float targetSpeed = 1;
    
    public void UpdateFuelBar(float maxFuel, float currentFuel)
    {
        targetSpeed = currentFuel / maxFuel;
    }

    void Update()
    {
        fuelBar.fillAmount = Mathf.MoveTowards(fuelBar.fillAmount, targetSpeed, reduceSpeed * Time.deltaTime);
    }
}
