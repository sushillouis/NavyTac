using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

[System.Serializable]
public struct RewindTestEntity
{
    public int entityIndex;
    public List<Vector3> runPos;
    public List<Vector3> testPos;
    public List<float> distanceDiffs;

    public RewindTestEntity(int index)
    {
        entityIndex = index;
        runPos = new List<Vector3>();
        testPos = new List<Vector3>();
        distanceDiffs = new List<float>();
    }
}

public class ReplayTest : MonoBehaviour
{
    public static ReplayTest inst;
    public List<float> timesBetweenStates;
    public float timeSinceLastState;

    public List<RewindTestEntity> entities;
    public List<Entity> runEntites;

    public int numRows;
    public int numCols;

    bool started;

    public float replayTimer = 0;
    bool loadNext;
    bool startReplay;
    bool initReplay;
    bool lastCommand;
    bool replayRunning;

    public string filename;
    
    // Start is called before the first frame update
    void Start()
    {
        inst = this;
        timesBetweenStates = new List<float>();
        timeSinceLastState = 0;
        started = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Alpha0))
            CreateRewindTestEntities();

            
        if (started)
            timeSinceLastState += Time.deltaTime;
        if (Input.GetKeyUp(KeyCode.Alpha1))
        {
            SaveTestState();
        }
        if (Input.GetKeyUp(KeyCode.Alpha2))
        {
            SaveTestState();
            started = false;
        }
        if (Input.GetKeyUp(KeyCode.Alpha3))
        {
            initReplay = true;
        }
        if(Input.GetKeyUp(KeyCode.Alpha4))
        {
            StartCoroutine(RunAddMoves());
            StartCoroutine(RunSaves());
        }
            
    }

    int save = 0;
    private void LateUpdate()
    {
        if (startReplay) 
        {
            foreach (RewindTestEntity ent in entities)
            {
                ent.testPos.Add(EntityMgr.inst.entities[ent.entityIndex].position);
            }
            replayRunning = true;
            startReplay = false;
        }
        
        if (initReplay)
        {
            EntityMgr.inst.ResetEntities();
            LineMgr.inst.DestroyAllLines();
            ReplayMgr.inst.stateID = -1;
            ReplayMgr.inst.LoadForward();

            initReplay = false;
            startReplay = true;
            
        }

        if (loadNext)
        {
            ReplayMgr.inst.LoadForward();
            loadNext = false;
        }

        if (replayRunning && save < timesBetweenStates.Count)
        {
            replayTimer += Time.deltaTime;
            if(replayTimer > timesBetweenStates[save])
            {
                replayTimer = 0;
                foreach (RewindTestEntity ent in entities)
                {
                    ent.testPos.Add(EntityMgr.inst.entities[ent.entityIndex].position);
                }
                ReplayMgr.inst.LoadForward();
                save++;
                if (save == timesBetweenStates.Count)
                {
                    lastCommand = true;
                }
            }
        }
        if (lastCommand)
        {
            foreach (RewindTestEntity ent in entities)
            {
                for (int i = 0; i < ent.testPos.Count; i++)
                {
                    float diff = Vector3.Distance(ent.testPos[i], ent.runPos[i]);
                    ent.distanceDiffs.Add(diff);
                }
            }
            lastCommand = false;
            replayRunning = false;
            timesBetweenStates.Add(0);
            SaveResultsToCSV();
        }

        
    }

    public void SaveTestState()
    {
        ReplayMgr.inst.HandleSaveState();
        foreach(RewindTestEntity ent in entities)
        {
            ent.runPos.Add(EntityMgr.inst.entities[ent.entityIndex].position);
        }
        if (started)
        {
            timesBetweenStates.Add(timeSinceLastState);
            timeSinceLastState = 0;
        }
        started = true;
    }

    public void CreateRewindTestEntities()
    {
        Vector3 position = Vector3.zero;
        float initZ = position.z;
        float spread = 20;

        EntityMgr.inst.ResetEntities();
        entities.Clear();

        for (int i = 0; i < numRows; i++)
        {
            for (int j = 0; j < numCols; j++)
            {
                Entity ent = EntityMgr.inst.CreateEntity(EntityType.PilotVessel, position, Vector3.zero);
                position.z += spread;
                RewindTestEntity entStruct = new RewindTestEntity(EntityMgr.inst.entities.IndexOf(ent));
                entities.Add(entStruct);
                runEntites.Add(ent);
            }
            position.x += spread;
            position.z = initZ;
        }
        DistanceMgr.inst.Initialize();
    }

    public void SaveResultsToCSV()
    {
        string path = Path.Combine(Application.persistentDataPath, filename);
        TextWriter tw = new StreamWriter(path, false);
        tw.WriteLine("Save #, Run Pos X, Run Pos Y, Run Pos Z, Test Pos X, Test Pos Y, Test Pos Z, Time Til Next State, Distance Diff");
        tw.Close();

        tw = new StreamWriter(path, true);
        foreach(RewindTestEntity ent in entities)
        {
            tw.WriteLine("Entity: " + EntityMgr.inst.entities[ent.entityIndex].name);
            for (int i = 0; i < ent.testPos.Count; i++)
            {
                tw.WriteLine(i + ","
                    + ent.runPos[i].x + "," + ent.runPos[i].y + "," + ent.runPos[i].z + ","
                    + ent.testPos[i].x + "," + ent.testPos[i].y + "," + ent.testPos[i].z + ","
                    + timesBetweenStates[i] + "," + ent.distanceDiffs[i]);
            }

            float totalDiff = 0;
            for(int i = 0; i < ent.distanceDiffs.Count; i++)
                totalDiff += ent.distanceDiffs[i];

            float mean = totalDiff / ent.distanceDiffs.Count;

            float sqDiff = 0;

            for (int i = 0; i < ent.distanceDiffs.Count; i++)
                sqDiff += (ent.distanceDiffs[i] - mean) * (ent.distanceDiffs[i] - mean);

            float stdDev = Mathf.Sqrt(sqDiff/ent.distanceDiffs.Count);

            tw.WriteLine("Mean:," + mean + ",Standard Dev:," + stdDev);
        }
        
        for(int i = 0; i < entities[0].distanceDiffs.Count; i++)
        {
            float stateTotalDiff = 0;
            foreach (RewindTestEntity ent in entities)
                stateTotalDiff += ent.distanceDiffs[i];

            float mean = stateTotalDiff / entities.Count;

            float sqDiff = 0;
            foreach (RewindTestEntity ent in entities)
                sqDiff += (ent.distanceDiffs[i] - mean) * (ent.distanceDiffs[i] - mean);

            float stdDev = Mathf.Sqrt(sqDiff / entities.Count);

            tw.WriteLine("State " + i + " Mean:," + mean + ",State " + i + " Standard Dev:," + stdDev);
        }

        tw.Close();
    }

    public List<Vector3> movePositions;
    public float timeBetweenCommands;

    IEnumerator RunAddMoves()
    {
        
        for (int i = 0; i < movePositions.Count; i++)
        {
            foreach (Entity entity in runEntites)
            {
                Move m = new Move(entity, movePositions[i]);
                UnitAI uai = entity.GetComponent<UnitAI>();
                uai.AddCommand(m);
            }
            SaveTestState();
            yield return new WaitForSeconds(timeBetweenCommands);
        }
    }

    public int numSaves;
    public float timeBetweenSaves;

    IEnumerator RunSaves()
    {
        for (int i = 0; i < numSaves; i++)
        {
            SaveTestState();
            yield return new WaitForSeconds(timeBetweenSaves);
        }
        started = false;
    }
}
