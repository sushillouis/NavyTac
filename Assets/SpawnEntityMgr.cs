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

        // Spawn front units (JARIUSV) - 500m directly in front
        SpawnInArc(frontList, center, heading, 500f, -45f, 45f, player);
        
        // Spawn middle units - 1000m in a wide front arc
        SpawnInArc(middleList, center, heading, 1000f, -90f, 90f, player);
        
        // Spawn back units (DDG51/SeaHunter) - 1500m directly behind
        SpawnInArc(backList, center, heading, 1000f, 135f, 225f, player);
        
        // Spawn other units in standard rings
        SpawnOtherEntities(otherList, center, heading, player);
    }

    void SpawnInArc(List<EntityType> entities, Vector3 center, float heading, float radius, 
                   float startAngle, float endAngle, TactPlayer player)
    {
        if (entities.Count == 0) return;
        
        // Convert heading to direction vector
        Vector3 headingDirection = Quaternion.Euler(0, heading, 0) * Vector3.forward;
        
        float angleRange = Mathf.Abs(endAngle - startAngle);
        float angleStep = angleRange / Mathf.Max(1, entities.Count - 1);
        
        for (int i = 0; i < entities.Count; i++)
        {
            float currentAngle = startAngle + i * angleStep;
            
            // Create rotation based on current angle
            Quaternion rotation = Quaternion.Euler(0, currentAngle, 0);
            
            // Create offset vector relative to heading
            Vector3 offset = rotation * headingDirection * radius;
            
            // Create entity with forward direction matching formation heading
            Vector3 position = center + offset;
            EntityMgr.inst.CreateEntity(
                entities[i],
                position,
                new Vector3(0, heading, 0),
                player
            );
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