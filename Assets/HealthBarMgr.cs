using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class HealthBarMgr : MonoBehaviour
{
    private Entity entity;
    [SerializeField] private Slider slider;

    

    void Start()
    {
        entity = GetComponentInParent<Entity>();
        if (slider == null)
        {
            Debug.LogError("HealthBarMgr: Slider reference not set!");
            enabled = false;
        }
    }

    void Update()  // Changed from FixedUpdate to Update
    {
        if (entity == null) return;

        // Billboard effect
        transform.LookAt(Camera.main.transform.position);
        transform.Rotate(0, 180, 0);

        // Visibility check
        gameObject.SetActive(entity.isVisible);
        
        SetHealthBar();
    }

    public void SetHealthBar()
    {
        if (slider == null || entity.maxHealth <= 0) return;
        
        // Clamp health and calculate normalized value
        float healthPercentage = Mathf.Clamp(entity.health, 0, entity.maxHealth) / entity.maxHealth;
        slider.value = healthPercentage;
    }
}