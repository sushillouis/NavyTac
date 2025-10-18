using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class CaptureNeutralBaseMgr : MonoBehaviour
{
    [Range(0, 220)] public float captureProgressEnemy = 0f; // 0 (enemy fully captured) to 220 (player fully captured)
    [Range(0, 220)] public float captureProgressPlayer = 0f; // 0 (enemy fully captured) to 220 (player fully captured)
    public Image enemyFill;   // Red (fills from left)
    public Image playerFill;  // Blue (fills from right)
    public Image background; // Background image for the capture bar
    public TMP_Text CaptureText; // Text to display capture progress
    public float maxWidth = 220f; // Total width of the background image
    private float captureProgress = 0f; // Initial neutral state (halfway between enemy and player)

    private bool isCaptured = false;
    public static CaptureNeutralBaseMgr inst;
    private void Awake()
    {
        inst = this;
    }

    void Update()
    {
        if (CheckCaptureCompletion())
        {
            return;
        }
            HandleCaptureLogic();
            UpdateCaptureProgress();
            UpdateUIWidths();

        if (IsNeutralEntityVisible())
        {
            ActivateUIElements();

        }
        else
        {
            DeactivateUIElements();
        }
    }

    private void HandleCaptureLogic()
    {
        int playerEntitiesInRange = 0;
        int enemyEntitiesInRange = 0;

        if (EntityMgr.inst?.entities == null || EnemyAIMgr.inst?.neutralBases == null || EnemyAIMgr.inst.neutralBases.Count == 0)
        {
            return;
        }

        Entity neutralBase = EnemyAIMgr.inst.neutralBases[0];
        if (neutralBase == null || neutralBase.transform == null)
        {
            return;
        }

        foreach (Entity entity in EntityMgr.inst.entities)
        {
            if (entity == null || entity.owner == null || entity.transform == null) continue;

            float distance = Vector3.Distance(entity.transform.position, neutralBase.transform.position);
            if (distance <= 1400f)
            {
                if (entity.owner == PlayerMgr.inst.player1 && entity.entityType != EntityType.AntiShipMissile)
                {
                    playerEntitiesInRange++;
                }
                else if (entity.owner == PlayerMgr.inst.player2 && entity.entityType != EntityType.AntiShipMissile)
                {
                    enemyEntitiesInRange++;
                }
            }
        }

        float captureRate = Time.deltaTime * (maxWidth / 60f); // Base capture rate for one entity in 60 seconds

        if (playerEntitiesInRange > enemyEntitiesInRange)
        {
            int netEntities = playerEntitiesInRange - enemyEntitiesInRange;
            captureProgressPlayer += captureRate * netEntities;
            captureProgressEnemy -= captureRate * netEntities;
        }
        else if (enemyEntitiesInRange > playerEntitiesInRange)
        {
            int netEntities = enemyEntitiesInRange - playerEntitiesInRange;
            captureProgressEnemy += captureRate * netEntities;
            captureProgressPlayer -= captureRate * netEntities;
        }
        // If equal, do nothing (no progress for either side)

        // Clamp to prevent negative progress
        captureProgressPlayer = Mathf.Clamp(captureProgressPlayer, 0f, maxWidth);
        captureProgressEnemy = Mathf.Clamp(captureProgressEnemy, 0f, maxWidth);
    }

    public bool CheckCaptureCompletion()
    {
        if (captureProgressEnemy >= maxWidth || captureProgressPlayer >= maxWidth)
        {
            isCaptured = true;
            if (captureProgressEnemy >= maxWidth)
            {
                ChangeOwnership(PlayerMgr.inst.player2);
            }
            else if (captureProgressPlayer >= maxWidth)
            {
                ChangeOwnership(PlayerMgr.inst.player1);
            }

            // Set isNeutral to false for all neutral entities
            foreach (Entity entity in EnemyAIMgr.inst.neutralBases)
            {
                if (entity != null)
                {
                    entity.isNeutral = false;
                }
            }
            foreach (Entity entity in EnemyAIMgr.inst.neutralEntitiesList)
            {
                if (entity != null)
                {
                    entity.isNeutral = false;
                }
            }

            return true;
        }
        return false;
    }

   private void ChangeOwnership(TactPlayer newOwner)
{
    foreach (Entity entity in EnemyAIMgr.inst.neutralBases)
    {
        if (entity != null && entity.owner != newOwner)
        {
            entity.owner = newOwner;
            entity.SetEntityColors();
            UpdateEntityQuantities(newOwner, entity.entityType);
        }
    }

    foreach (Entity entity in EnemyAIMgr.inst.neutralEntitiesList)
    {
        if (entity != null && entity.owner != newOwner)
        {
            entity.owner = newOwner;
            entity.SetEntityColors();
            UpdateEntityQuantities(newOwner, entity.entityType);
        }
    }
}
private void UpdateEntityQuantities(TactPlayer owner, EntityType type)
{
    if (owner == PlayerMgr.inst.player1 || owner == PlayerMgr.inst.player2)
    {
        var eqList = GameMgr.inst.entityQuantities;
        var entry = eqList.FirstOrDefault(eq => eq.entityType == type);
        if (entry != null)
        {
            entry.unitCount += 1;
        }
        else
        {
            eqList.Add(new EntityQuantity { entityType = type, unitCount = 1 });
        }

        ScenarioGenerator.inst.BuildEntityDictionary();
    }
}
    

    private void ActivateUIElements()
    {
        if (enemyFill != null) enemyFill.gameObject.SetActive(true);
        if (playerFill != null) playerFill.gameObject.SetActive(true);
        if (background != null) background.gameObject.SetActive(true);
        if (CaptureText != null) CaptureText.gameObject.SetActive(true);
    }

    private void DeactivateUIElements()
    {
        if (enemyFill != null) enemyFill.gameObject.SetActive(false);
        if (playerFill != null) playerFill.gameObject.SetActive(false);
        if (background != null) background.gameObject.SetActive(false);
        if (CaptureText != null) CaptureText.gameObject.SetActive(false);
    }

    private void UpdateCaptureProgress()
    {
        captureProgressEnemy = Mathf.Clamp(captureProgressEnemy, 0f, maxWidth);
        captureProgressPlayer = Mathf.Clamp(captureProgressPlayer, 0f, maxWidth);

        float totalProgress = captureProgressEnemy + captureProgressPlayer;
        if (totalProgress > maxWidth)
        {
            float excess = totalProgress - maxWidth;
            if (captureProgressEnemy > captureProgressPlayer)
            {
                captureProgressEnemy -= excess;
            }
            else
            {
                captureProgressPlayer -= excess;
            }
        }
    }

    private void UpdateUIWidths()
    {
        float enemyWidth = captureProgressEnemy;
        float playerWidth = captureProgressPlayer;

        if (enemyFill != null && playerFill != null)
        {
            RectTransform enemyRect = enemyFill.GetComponent<RectTransform>();
            RectTransform playerRect = playerFill.GetComponent<RectTransform>();

            if (enemyRect != null)
                enemyRect.sizeDelta = new Vector2(enemyWidth, enemyRect.sizeDelta.y);

            if (playerRect != null)
                playerRect.sizeDelta = new Vector2(playerWidth, playerRect.sizeDelta.y);
        }
    }
    private bool IsNeutralEntityVisible()
    {
        Entity neutralBase = EnemyAIMgr.inst?.neutralBases?.Count > 0 ? EnemyAIMgr.inst.neutralBases[0] : null;
        if (neutralBase == null || neutralBase.transform == null || Camera.main == null || isCaptured || !neutralBase.isVisible)
        {
            return false;
        }

        Vector3 viewportPoint = Camera.main.WorldToViewportPoint(neutralBase.transform.position);
        float distanceToBase = Vector3.Distance(Camera.main.transform.position, neutralBase.transform.position);

        if (viewportPoint.z > 0 && viewportPoint.x > 0 && viewportPoint.x < 1 && viewportPoint.y > 0 && viewportPoint.y < 1)
        {
            Ray ray = new Ray(Camera.main.transform.position, neutralBase.transform.position - Camera.main.transform.position);
            if (Physics.Raycast(ray, out RaycastHit hit, distanceToBase))
            {
                if (hit.transform == neutralBase.transform)
                {
                    return true; // Neutral entity is visible and not obstructed
                }
            }
        }

        return false; // Neutral entity is not visible or is obstructed
    }


}
