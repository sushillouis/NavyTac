using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EntityStatusPanelMgr : MonoBehaviour
{
    [System.Serializable]
    public class EntityStatusUI
    {
        public EntityType entityType;
        public TMP_Text quantityText;
        public Slider healthSlider;
    }

    [Header("Assign panels manually in Inspector")]
    public List<EntityStatusUI> entityStatusPanels;

    void Start()
    {
        // Initialize all panels to full health and quantity 1 at start
        foreach (var panel in entityStatusPanels)
        {
            panel.quantityText.text = "1";
            panel.healthSlider.value = 1f;
        }
        InvokeRepeating(nameof(UpdateEntityUI), 0f, 1f); // Update every 1 sec
    }

    void UpdateEntityUI()
    {
        if (EntityMgr.inst == null || EntityMgr.inst.entities == null || PlayerMgr.inst == null) return;

        Dictionary<EntityType, int> countMap = new();
        Dictionary<EntityType, float> totalHealthMap = new();

        foreach (Entity entity in EntityMgr.inst.entities)
        {
            if (entity == null || entity.owner != PlayerMgr.inst.player1) continue;

            EntityType type = entity.entityType;
            if (!countMap.ContainsKey(type))
            {
                countMap[type] = 0;
                totalHealthMap[type] = 0f;
            }

            countMap[type]++;
            totalHealthMap[type] += entity.health;
        }

        foreach (var panel in entityStatusPanels)
        {
            // Always update based on prefab maxHealth, even if count is 0
            float maxHealth = EntityMgr.inst.GetEntityPrefab(panel.entityType).maxHealth;

            if (countMap.TryGetValue(panel.entityType, out int count) && count > 0)
            {
            float avgHealth = totalHealthMap[panel.entityType] / count;
            panel.quantityText.text = count.ToString();
            panel.healthSlider.value = avgHealth / maxHealth;
            }
            else
            {
            panel.quantityText.text = "0";
            panel.healthSlider.value = 0f; // Show empty health when none exist
            }
        }
    }
}
