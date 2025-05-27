using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ReplayMgr : MonoBehaviour
{
    public static ReplayMgr inst;
    private StreamWriter writer;
    private string filePath;
    private float snapshotInterval = 0.1f; // Capture snapshots every 0.1 seconds
    private float lastSnapshotTime = 0f;

    private void Awake()
    {
        if (inst == null)
        {
            inst = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartRecording(string fileName)
    {
        filePath = Path.Combine(Application.persistentDataPath, fileName);
        writer = new StreamWriter(filePath, false);
        lastSnapshotTime = Time.time;
        Debug.Log($"Replay recording started: {filePath}");
    }

    public void StopRecording()
    {
        if (writer != null)
        {
            writer.Flush();
            writer.Close();
            writer = null;
            Debug.Log($"Replay recording stopped: {filePath}");
        }
    }

    private void FixedUpdate()
    {
        if (writer == null) return;
        float currentTime = Time.time;
        if (currentTime - lastSnapshotTime >= snapshotInterval)
        {
            CaptureSnapshot(currentTime);
            lastSnapshotTime = currentTime;
        }
    }

    private void CaptureSnapshot(float timestamp)
    {
        List<EntityState> entityStates = new List<EntityState>();
        foreach (Entity ent in EntityMgr.inst.entities)
        {
            entityStates.Add(new EntityState
            {
                id = ent.entityId,
                position = ent.position,
                rotation = ent.transform.rotation,
                velocity = ent.velocity,
                speed = ent.speed,
                heading = ent.heading,
                desiredSpeed = ent.desiredSpeed,
                desiredHeading = ent.desiredHeading,
                health = ent.health,
                fuel = ent.fuel,
                range = ent.range,
                entityType = ent.entityType,
                entityClass = ent.entityClass,
                ownerId = ent.owner != null ? ent.owner.playerId : 0
            });
        }

        string json = JsonUtility.ToJson(new Snapshot
        {
            type = "snapshot",
            timestamp = timestamp,
            entities = entityStates
        });

        writer.WriteLine(json);
    }

    public void RecordEvent(float timestamp, string eventType, string eventDataJson)
    {
        if (writer == null) return;
        string json = JsonUtility.ToJson(new ReplayEvent
        {
            type = "event",
            timestamp = timestamp,
            eventType = eventType,
            data = eventDataJson
        });
        writer.WriteLine(json);
    }
}

[Serializable]
public class Snapshot
{
    public string type;
    public float timestamp;
    public List<EntityState> entities;
}

[Serializable]
public class ReplayEvent
{
    public string type;
    public float timestamp;
    public string eventType;
    public string data;
}

[Serializable]
public class EntityState
{
    public int id;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 velocity;
    public float speed;
    public float heading;
    public float desiredSpeed;
    public float desiredHeading;
    public float health;
    public float fuel;
    public float range;
    public EntityType entityType;
    public EntityClass entityClass;
    public ulong ownerId;
}

[Serializable]
public class EntityCreationData
{
    public EntityType entityType;
    public int entityId;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 velocity;
    public float health;
    public float fuel;
    public ulong ownerId;
}