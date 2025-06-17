using UnityEngine;
using System.Collections.Generic;
public class ResetScene : MonoBehaviour
{
    public static ResetScene inst;
    private void Awake()
    {
        inst = this;
    }
    public void ReloadScene()
    {
        ClearAllEntities();
        ResetGameState();
        if (FogWarMgr.inst != null)
        {
            FogWarMgr.inst.FOW = true;
        }
        GameMgr.inst.OpenOcean1x1();
    }
    public void ClearAllEntities()
    {
        if (WeaponsMgr.inst?.weapons != null)
        {
            DestroyEntitiesInList(new List<Entity>(WeaponsMgr.inst.weapons));
        }
        DestroyEntitiesInList(EntityMgr.inst?.entities);

        if (SelectionMgr.inst != null)
        {
            SelectionMgr.inst.selectedEntities.Clear();
            SelectionMgr.inst.selectedEntity = null;
        }
        System.GC.Collect();
        Resources.UnloadUnusedAssets();
        Physics.SyncTransforms();
    }

    private void DestroyEntitiesInList(List<Entity> entityList)
    {
        if (entityList == null)
        {
            return;
        }
        var entitiesToDestroy = new List<Entity>(entityList);
        foreach (Entity entity in entitiesToDestroy)
        {
            if (entity != null && entity.gameObject != null)
            {
                if (entity.TryGetComponent<UnitAI>(out var unitAI))
                {
                    unitAI.StopAndRemoveAllCommands();
                }
                if (entity.gameObject != null)
                {
                    DestroyImmediate(entity.gameObject);
                }
            }

            if (SelectionMgr.inst != null)
            {
                SelectionMgr.inst.selectedEntities.Clear();
                SelectionMgr.inst.selectedEntity = null;
            }
            System.GC.Collect();
            Resources.UnloadUnusedAssets();
            Physics.SyncTransforms();

        }
    }
    public void ResetGameState()
    {
        Time.timeScale = 1f;
        GameMgr.inst.BuildEntityDictionary();
        AIMgr.inst?.StopAllCoroutines();
        DistanceMgr.inst?.Initialize();
        FogWarMgr.inst?.ResetFog();
        CameraMgr.inst?.ResetCamera();
        ScoreMgr.inst?.ResetScores();
        OpenOceanMain.inst?.ResetGameState();
        MinimapMgr.inst?.ResetMinimap();
        LineMgr.inst?.DestroyAllLines();
        WeaponsMgr.inst?.DestroyAllWeaponsImmediately();
        FXMgr.inst?.ResetEffects();
        EntityMgr.inst?.Reset();
        // EnemyAIMgr.inst?.ResetLevel2State();
    }
}