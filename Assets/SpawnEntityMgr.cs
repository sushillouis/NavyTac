using System.Collections.Generic;
using UnityEngine;

public class SpawnEntityMgr : MonoBehaviour
{
    public static SpawnEntityMgr inst;
    void Awake() => inst = this;

    public void SpawnEntitiesFromDictionary(Vector3 initPos, float initHeading, TactPlayer player)
    {
        if (player == null) return;

        // Build spawn queue from entity dictionary
        List<EntityType> spawnQueue = new();
        foreach (EntityType type in priorityList)
            if (GameMgr.inst.entityDict.TryGetValue(type, out int count))
                for (int j = 0; j < count; j++)
                    spawnQueue.Add(type);
        
        SpawnPriorityFormation(spawnQueue, initPos, initHeading, player);
    }

    void SpawnPriorityFormation(List<EntityType> queue, Vector3 center, float heading, TactPlayer player)
    {
        // Separate entities into priority groups
        List<EntityType> centerList = new();      // Rig Balder (first only)
        List<EntityType> frontList = new();       // JARIUSV
        List<EntityType> middleList = new();      // Other combat units
        List<EntityType> backList = new();        // SeaHunter and DDG51
        List<EntityType> otherList = new();       // All other entities

        bool centerSpawned = false;
        foreach (EntityType type in queue)
        {
            if (!centerSpawned && type == EntityType.Rig_Balder)
            {
                centerList.Add(type);
                centerSpawned = true;
            }
            else if (type == EntityType.JARIUSV)
            {
                frontList.Add(type);
            }
            else if (type == EntityType.SeaHunter || type == EntityType.DDG51)
            {
                backList.Add(type);
            }
            else if (type == EntityType.CVN75 || type == EntityType.Submarine || 
                     type == EntityType.OrientExplorer || type == EntityType.MineSweeper)
            {
                middleList.Add(type);
            }
            else
            {
                otherList.Add(type);
            }
        }

        // Spawn center unit (Rig Balder) if exists
        foreach (EntityType entity in centerList)
        {
            EntityMgr.inst.CreateEntity(entity, center, new Vector3(0, heading, 0), player);
        }

        // Spawn front units (JARIUSV) with multi-row support
        SpawnMultiRowFormation(frontList, center, heading, 1000f, -45f, 45f, player, 10, 200f);
        
        // Spawn middle units with multi-row support
        SpawnMultiRowFormation(middleList, center, heading, 1000f, -90f, 90f, player, 10, 200f);
        
        // Spawn back units (DDG51/SeaHunter) with multi-row support
        SpawnMultiRowFormation(backList, center, heading, 1000f, 135f, 225f, player, 10, 200f);
        
        // Spawn other units in standard rings
        SpawnOtherEntities(otherList, center, heading, player);
    }

    void SpawnMultiRowFormation(List<EntityType> entities, Vector3 center, float heading, 
                               float baseRadius, float startAngle, float endAngle, 
                               TactPlayer player, int maxPerRow = 10, float rowSpacing = 500f)
    {
        if (entities.Count == 0) return;

        // Reorder so that SeaHunter spawns first, then DDG51
        List<EntityType> seahunters = new();
        List<EntityType> ddg51s = new();
        List<EntityType> others = new();
        foreach (var e in entities)
        {
            if (e == EntityType.SeaHunter) seahunters.Add(e);
            else if (e == EntityType.DDG51) ddg51s.Add(e);
            else others.Add(e);
        }
        List<EntityType> reordered = new();
        reordered.AddRange(seahunters);
        reordered.AddRange(ddg51s);
        reordered.AddRange(others);

        Vector3 headingDirection = Quaternion.Euler(0, heading, 0) * Vector3.forward;
        int rowCount = Mathf.CeilToInt((float)reordered.Count / maxPerRow);
        float angleRange = Mathf.Abs(endAngle - startAngle);

        for (int row = 0; row < rowCount; row++)
        {
            int rowStartIndex = row * maxPerRow;
            int unitsInThisRow = Mathf.Min(maxPerRow, reordered.Count - rowStartIndex);
            float currentRadius = baseRadius + row * rowSpacing;
            
            if (unitsInThisRow == 1)
            {
                Vector3 offset = Quaternion.Euler(0, (startAngle + endAngle) / 2, 0) * headingDirection * currentRadius;
                EntityMgr.inst.CreateEntity(
                    reordered[rowStartIndex],
                    center + offset,
                    new Vector3(0, heading, 0),
                    player
                );
                continue;
            }

            float angleStep = angleRange / (unitsInThisRow - 1);
            for (int i = 0; i < unitsInThisRow; i++)
            {
                int entityIndex = rowStartIndex + i;
                float currentAngle = startAngle + i * angleStep;
                Quaternion rotation = Quaternion.Euler(0, currentAngle, 0);
                Vector3 offset = rotation * headingDirection * currentRadius;
                
                EntityMgr.inst.CreateEntity(
                    reordered[entityIndex],
                    center + offset,
                    new Vector3(0, heading, 0),
                    player
                );
            }
        }
    }

    void SpawnOtherEntities(List<EntityType> entities, Vector3 center, float heading, TactPlayer player)
    {
        if (entities.Count == 0) return;

        int index = 0;
        int ring = 2;  // Start from ring 2 (500m already used)
        float ringRadius = 1000f;  // Start at 1000m

        while (index < entities.Count)
        {
            int positionsInRing = 8 * ring;
            float angleStep = 360f / positionsInRing;
            
            for (int pos = 0; pos < positionsInRing && index < entities.Count; pos++)
            {
                float angle = pos * angleStep;
                Quaternion rotation = Quaternion.Euler(0, angle, 0);
                Vector3 offset = rotation * (Quaternion.Euler(0, heading, 0) * Vector3.forward) * ringRadius;
                
                EntityMgr.inst.CreateEntity(
                    entities[index],
                    center + offset,
                    new Vector3(0, heading, 0),
                    player
                );
                index++;
            }
            
            ring++;
            ringRadius += 500f;  // Increase radius by 500m per ring
        }
    }

    // Priority list (highest to lowest)
    public List<EntityType> priorityList = new List<EntityType>
    {
        EntityType.Rig_Balder,
        EntityType.CVN75,
        EntityType.Submarine,
        EntityType.DDG51,
        EntityType.SeaHunter,
        EntityType.JARIUSV,
        EntityType.OrientExplorer,
        EntityType.MineSweeper,
        EntityType.PilotVessel,
        EntityType.Mykola,
        EntityType.Container,
        EntityType.OilServiceVessel,
        EntityType.Tanker,
        EntityType.TugBoat,
        EntityType.SeaBaby
    };
}