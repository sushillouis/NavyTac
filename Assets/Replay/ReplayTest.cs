using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReplayTest : MonoBehaviour
{
    public static ReplayTest inst;
    public List<Vector3> runPos;
    public List<Vector3> testPos;
    public List<float> timesBetweenStates;
    public List<float> distanceDiffs;
    public bool doneAdding;
    public float timeSinceLastState;
    int runID;
    bool started;

    public float replayTimer = 0;
    bool loadNext;
    bool startReplay;
    bool lastCommand;
    bool replayRunning;
    
    // Start is called before the first frame update
    void Start()
    {
        inst = this;
        runPos = new List<Vector3>();
        testPos = new List<Vector3>();
        timesBetweenStates = new List<float>();
        distanceDiffs = new List<float>();
        timeSinceLastState = 0;
        runID = 0;
        started = false;
        doneAdding = false;
    }

    // Update is called once per frame
    void Update()
    {
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
            startReplay = true;
        }
    }

    int save = 0;
    private void LateUpdate()
    {
        if (startReplay)
        {
            EntityMgr.inst.ResetEntities();
            LineMgr.inst.DestroyAllLines();
            ReplayMgr.inst.stateID = -1;
            ReplayMgr.inst.LoadForward();
            SelectionMgr.inst.selectedEntity = EntityMgr.inst.entities[0];
            testPos.Add(EntityMgr.inst.entities[0].position);
            replayRunning = true;
            startReplay = false;
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
                testPos.Add(EntityMgr.inst.entities[0].position);
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
            for (int i = 0; i < testPos.Count; i++)
            {
                float diff = Vector3.Distance(testPos[i], runPos[i]);
                distanceDiffs.Add(diff);
            }
            lastCommand = false;
            replayRunning = false;
        }

        
    }

    public void SaveTestState()
    {
        ReplayMgr.inst.HandleSaveState();
        runPos.Add(EntityMgr.inst.entities[0].position);
        if (started)
        {
            timesBetweenStates.Add(timeSinceLastState);
            timeSinceLastState = 0;
        }
        started = true;
    }
}
