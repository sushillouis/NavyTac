using UnityEngine;
using System.Collections.Generic;
using TMPro; 

public class ResetScene : MonoBehaviour
{
    
    public static ResetScene inst;

    private void Awake()
    {
        if (inst == null)
        {
            inst = this;
        }
        else
        {
            Debug.LogWarning("Multiple instances of ResetScene detected. Destroying duplicate.");
            Destroy(gameObject);
        }
    }
    public void ReloadScene()
    {
        

        ClearAllEntities();
        ResetGameState();
        GameMgr.inst.OpenOcean1x1();
    }

    // Your existing ClearAllEntities method.
    public void ClearAllEntities()
    {
        if (WeaponsMgr.inst != null)
        {
            WeaponsMgr.inst.StopAllWeapons();
            var weaponsCopy = new List<Entity>(WeaponsMgr.inst.weapons);
            foreach (Entity weapon in weaponsCopy)
            {
                if (weapon != null && weapon.gameObject != null)
                {
                    if (weapon.TryGetComponent<UnitAI>(out var unitAI))
                    {
                        unitAI.StopAndRemoveAllCommands();
                    }
                    if (weapon.gameObject != null)
                    {
                        DestroyImmediate(weapon.gameObject);
                    }
                }
            }
            WeaponsMgr.inst.weapons.Clear();
        }

        if (EntityMgr.inst != null)
        {
            var entitiesCopy = new List<Entity>(EntityMgr.inst.entities);
            foreach (Entity entity in entitiesCopy)
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
            }
            EntityMgr.inst.entities.Clear();
        }

        if (SelectionMgr.inst != null)
        {
            SelectionMgr.inst.selectedEntities.Clear();
            SelectionMgr.inst.selectedEntity = null;
        }

        // Consider the performance impact of GC.Collect and UnloadUnusedAssets.
        // These can cause noticeable hitches.
        System.GC.Collect();
        Resources.UnloadUnusedAssets();
        Physics.SyncTransforms(); 
        
    }

    // Your existing ResetGameState method.
    public void ResetGameState()
    {
        Time.timeScale = 1f;
        GameMgr.inst.BuildEntityDictionary(); // Ensure this method is defined below or accessible.
        if (AIMgr.inst != null) AIMgr.inst.StopAllCoroutines();
        if (DistanceMgr.inst != null) DistanceMgr.inst.Initialize();
        if (FogWarMgr.inst != null) FogWarMgr.inst.ResetFog();
        if (CameraMgr.inst != null) CameraMgr.inst.ResetCamera();
        if (ScoreMgr.inst != null) ScoreMgr.inst.ResetScores();
        if (OpenOceanMain.inst != null) OpenOceanMain.inst.ResetGameState();
        if (MinimapMgr.inst != null) MinimapMgr.inst.ResetMinimap();
        if (LineMgr.inst != null) LineMgr.inst.DestroyAllLines();
        if (WeaponsMgr.inst != null) WeaponsMgr.inst.DestroyAllWeaponsImmediately();
        if (FXMgr.inst != null) FXMgr.inst.ResetEffects(); 
        if (EntityMgr.inst != null) EntityMgr.inst.Reset();
        
    }
}