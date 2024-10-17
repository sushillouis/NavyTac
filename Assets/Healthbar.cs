using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Image healthBar;
    [SerializeField] private float reduceSpeed = 2;
    private float targetSpeed = 1;
    
    public void UpdateHealthBar(float maxHealth, float currentHealth)
    {
        targetSpeed = currentHealth / maxHealth;
    }

    void Update()
    {
        healthBar.fillAmount = Mathf.MoveTowards(healthBar.fillAmount, targetSpeed, reduceSpeed * Time.deltaTime);
    }
}
