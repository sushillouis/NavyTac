using System.Collections;
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
        public RectMask2D rectMask;

        [HideInInspector] public bool isAnimating = false;
        [HideInInspector] public bool wasPreviouslyGreyed = false; // Track state for transition
    }

    public List<EntityStatusUI> entityStatusPanels;

    void Start()
    {
        foreach (var panel in entityStatusPanels)
        {
            panel.quantityText.text = "1";
            panel.healthSlider.value = 1f;
            if (panel.rectMask != null)
                panel.rectMask.padding = new Vector4(0, 0, 0, 0); // Initially fully visible
        }

        
    }

    void Update()
    {
        if (EntityMgr.inst == null || PlayerMgr.inst == null) return;

        foreach (var panel in entityStatusPanels)
        {
            int count = 0;
            float healthSum = 0f;
            int greyedCount = 0;

            foreach (var e in EntityMgr.inst.entities)
            {
                if (e == null || e.owner != PlayerMgr.inst.player1 || e.entityType != panel.entityType)
                    continue;

                count++;
                healthSum += e.health;
                if (e.isGreyed) greyedCount++;
            }

            float avgHealth = count > 0 ? healthSum / count : 0f;
            panel.quantityText.text = count.ToString();
            panel.healthSlider.value = (count > 0) ? avgHealth / EntityMgr.inst.GetEntityPrefab(panel.entityType).maxHealth : 0f;

            bool nowGreyed = (count > 0) && (greyedCount == count); // all are greyed

            if (nowGreyed && !panel.wasPreviouslyGreyed && !panel.isAnimating)
            {
                float fadeDuration = GetFadeDuration(panel.entityType);
                StartCoroutine(WipePadding(panel, 50f, 0f, fadeDuration));
            }

            panel.wasPreviouslyGreyed = nowGreyed;
        }
    }

    IEnumerator WipePadding(EntityStatusUI panel, float fromRight, float toRight, float duration)
    {
        panel.isAnimating = true;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float right = Mathf.Lerp(fromRight, toRight, t);
            Vector4 pad = panel.rectMask.padding;
            pad.z = right;
            panel.rectMask.padding = pad;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure final state
        Vector4 finalPad = panel.rectMask.padding;
        finalPad.z = toRight;
        panel.rectMask.padding = finalPad;

        panel.isAnimating = false;
    }

    float GetFadeDuration(EntityType type)
    {
        foreach (var e in EntityMgr.inst.entities)
        {
            if (e != null && e.entityType == type)
                return e.greyOverlayFadeDuration;
        }
        return 2f; // fallback
    }
}
