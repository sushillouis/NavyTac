using System.Collections.Generic;
using UnityEngine;

public class SpawnEntityMgr : MonoBehaviour
{
    // Start is called before the first frame update
    public static SpawnEntityMgr inst;
    void Awake()
    {
        inst = this;
    }
    
    public void SpawnEntitiesFromDictionary(Vector3 initPos, float initHeading, TactPlayer player)
    {
        List<EntityType> spawnQueue = new();
        foreach (EntityType type in priorityList) 
        {
            if (GameMgr.inst.entityDict.TryGetValue(type, out int count))
            {
                for (int j = 0; j < count; j++)
                {
                    spawnQueue.Add(type);
                }
            }
        }
        
        if (player == null)
        {
            return;
        }
        SpawnInFormation(spawnQueue, initPos, initHeading, player);
    }

    void SpawnInFormation(List<EntityType> queue, Vector3 center, float heading, TactPlayer player)
    {
        int index = 0; 
        int ring = 1;  

        if (queue.Count > 0)
        {
            EntityMgr.inst.CreateEntity(queue[index], center, new Vector3(0, heading, 0), player);
            index++;
        }
        
        while (index < queue.Count)
        {
            if (ring == 1) 
            {
                ring++; 
                continue; 
            }

            float radius = 500f * (ring - 1); 
            int numPositionsThisRing = 8 * (ring - 1); 
            
            float angleStep = 360f / numPositionsThisRing; 

            for (int pos = 0; pos < numPositionsThisRing && index < queue.Count; pos++)
            {
                float currentAngleRad = pos * angleStep * Mathf.Deg2Rad; 
                Vector3 offset = new Vector3(
                    Mathf.Cos(currentAngleRad),
                    0, 
                    Mathf.Sin(currentAngleRad)
                ) * radius;

                offset = Quaternion.Euler(0, heading, 0) * offset;

                EntityMgr.inst.CreateEntity(queue[index], center + offset,
                    new Vector3(0, heading, 0), player);
                index++;
            }
            ring++; 
        }
    }

    public List<EntityType> priorityList = new List<EntityType>()
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
