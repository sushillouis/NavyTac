using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.XR.OpenVR;
using UnityEngine;
using UnityEngine.Playables;

[System.Serializable]
public class ReplayMgr : MonoBehaviour
{
    public static ReplayMgr inst;
    private GameData gameData;
    private FileDataHandler dataHandler;
    public bool useEncryption = false;
    public int stateID;
    public int maxStateID;
    public string selectedProfileID;
    public bool record;

    private bool add;
    private List<UnitAI> uais;
    private List<Command> commands;

    private void Awake()
    {
        inst = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        stateID = -1;
        maxStateID = -1;

        gameData = new GameData();
        string path = Path.Combine(Application.persistentDataPath, "saves");
        dataHandler = new FileDataHandler(path, useEncryption);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Alpha1))
        {
            HandleSaveState();
        }

        if (Input.GetKeyUp(KeyCode.RightBracket))
        {
            //Debug.Log(Application.persistentDataPath);
            LoadForward();

        }

        if (Input.GetKeyUp(KeyCode.LeftBracket))
        {
            LoadBackward();
        }
    }

    public void HandleSaveState()
    {
        selectedProfileID = "state" + (stateID + 1) + ".game";

        SaveData();

        stateID++;
        maxStateID = stateID;
    }

    public void LoadForward()
    {
        if (stateID == maxStateID)
        {
            Debug.Log("At max state");
            return;
        }

        stateID++;
        selectedProfileID = "state" + stateID + ".game";
        LoadData();

    }

    public void LoadBackward()
    {
        if (stateID <= 0)
        {
            Debug.Log("At min state");
        }
        else
        {
            stateID--;
            selectedProfileID = "state" + stateID + ".game";
        }

        LoadData();
    }

    public void LoadData()
    {
        //Loads in new data from the 
        gameData = dataHandler.Load(selectedProfileID); 

        //resets the scene
        LineMgr.inst.DestroyAllLines();
        EntityMgr.inst.ResetEntities();
        EntityMgr.entityId = gameData.entityID - (gameData.entityIndex.Count);

        //Loads in entities
        for (int i = 0; i < gameData.entityIndex.Count; i++)
        {
            Entity ent = EntityMgr.inst.CreateEntity((EntityType)gameData.entityType[i], gameData.position[i], new Vector3(0, gameData.heading[i], 0));
            ent.velocity = gameData.velocity[i];
            ent.speed = gameData.speed[i];
            ent.desiredSpeed = gameData.ds[i];
            ent.heading = gameData.heading[i];
            ent.desiredHeading = gameData.dh[i];
        }

        //Loads in commands for each entity
        foreach (Entity ent in EntityMgr.inst.entities)
        {
            //Finds all commands associated with the current entity in the loop
            List<int> dataIndexes = new List<int>();
            for (int j = 0; j < gameData.commandEntIndex.Count; j++)
            {
                if (gameData.commandEntIndex[j] == EntityMgr.inst.entities.IndexOf(ent))
                {
                    dataIndexes.Add(j);
                }
            }


            foreach (int dataIndex in dataIndexes)
            {
                Command newCmd;

                if (gameData.commandType[dataIndex] == 0)
                    newCmd = new Move(EntityMgr.inst.entities[gameData.commandEntIndex[dataIndex]], gameData.movePosition[dataIndex]);
                else if (gameData.commandType[dataIndex] == 1)
                    newCmd = new Follow(EntityMgr.inst.entities[gameData.commandEntIndex[dataIndex]], EntityMgr.inst.entities[gameData.followTargetIndex[dataIndex]], gameData.followOffset[dataIndex]);
                else
                    newCmd = new Intercept(EntityMgr.inst.entities[gameData.commandEntIndex[dataIndex]], EntityMgr.inst.entities[gameData.followTargetIndex[dataIndex]]);

                newCmd.isRunning = gameData.isRunning[dataIndex];

                UnitAI uai = ent.GetComponent<UnitAI>();

                uais.Add(uai);
                commands.Add(newCmd);
            }
        }

        DistanceMgr.inst.Initialize();
    }

    public void SaveData()
    {
        Debug.Log("saved");

        gameData.Clear();

        gameData.entityID = EntityMgr.entityId;

        foreach (Entity ent in EntityMgr.inst.entities)
        {
            gameData.entityIndex.Add(EntityMgr.inst.entities.IndexOf(ent));
            gameData.entityType.Add((int)ent.entityType);
            gameData.position.Add(ent.position);
            gameData.velocity.Add(ent.velocity);
            gameData.speed.Add(ent.speed);
            gameData.ds.Add(ent.desiredSpeed);
            gameData.heading.Add(ent.heading);
            gameData.dh.Add(ent.desiredHeading);

            UnitAI uai = ent.GetComponent<UnitAI>();

            foreach (Command cmd in uai.commands)
            {
                if (cmd is Intercept)
                {
                    Intercept inter = (Intercept)cmd;
                    gameData.commandType.Add(2);
                    gameData.followOffset.Add(Vector3.zero);
                    gameData.followTargetIndex.Add(EntityMgr.inst.entities.IndexOf(inter.targetEntity));
                    gameData.movePosition.Add(inter.movePosition);
                }
                else if (cmd is Follow)
                {
                    Follow f = (Follow)cmd;
                    gameData.commandType.Add(1);
                    gameData.followOffset.Add(f.relativeOffset);
                    gameData.followTargetIndex.Add(EntityMgr.inst.entities.IndexOf(f.targetEntity));
                    gameData.movePosition.Add(f.movePosition);
                }
                else
                {
                    Move m = (Move)cmd;
                    gameData.commandType.Add(0);
                    gameData.followOffset.Add(Vector3.zero);
                    gameData.followTargetIndex.Add(-1);
                    gameData.movePosition.Add(m.movePosition);
                }

                gameData.commandEntIndex.Add(EntityMgr.inst.entities.IndexOf(ent));
                gameData.commandIndex.Add(uai.commands.IndexOf(cmd));
                gameData.isRunning.Add(cmd.isRunning);
            }
        }
        dataHandler.Save(gameData, selectedProfileID);

    }
}

//class to hold all the data in the game
public class GameData
{
    public int entityID;

    //Entity Data Lists
    public List<int> entityIndex; //index of the entity in the EntityMgr
    public List<int> entityType;
    public List<Vector3> position;
    public List<Vector3> velocity;
    public List<float> speed;
    public List<float> ds;
    public List<float> heading;
    public List<float> dh;

    //Command Data Lists
    public List<int> commandType; //move = 0, follow = 1, intercepts = 2
    public List<int> commandEntIndex; //index of the entity of a command in the EntityMgr
    public List<int> commandIndex; //index of the command in the commands array in UnitAI
    public List<bool> isRunning;
    public List<Vector3> movePosition;

    //Follow Command Specific Data
    public List<int> followTargetIndex;
    public List<Vector3> followOffset;

    public GameData()
    {
        entityIndex = new List<int>();
        entityType = new List<int>();
        position = new List<Vector3>();
        velocity = new List<Vector3>();
        speed = new List<float>();
        ds = new List<float>();
        heading = new List<float>();
        dh = new List<float>();

        commandType = new List<int>();
        commandEntIndex = new List<int>();
        commandIndex = new List<int>();
        isRunning = new List<bool>();
        movePosition = new List<Vector3>();

        followTargetIndex = new List<int>();
        followOffset = new List<Vector3>();
    }

    public void Clear()
    {
        entityIndex.Clear();
        entityType.Clear();
        position.Clear();
        velocity.Clear();
        speed.Clear();
        ds.Clear();
        heading.Clear();
        dh.Clear();

        commandType.Clear();
        commandEntIndex.Clear();
        commandIndex.Clear();
        isRunning.Clear();
        movePosition.Clear();

        followOffset.Clear();
        followTargetIndex.Clear();
    }
}


public class FileDataHandler
{
    private string dataDirPath = "";

    private bool useEncryption = false;
    private readonly string encryptionCodeWord = "ECSL";

    public FileDataHandler(string dataDirPath, bool useEncryption)
    {
        this.dataDirPath = dataDirPath;
        this.useEncryption = useEncryption;
    }

    public GameData Load(string profileID)
    {
        string fullPath = Path.Combine(dataDirPath, profileID);
        GameData loadedData = null;
        if (File.Exists(fullPath))
        {
            try
            {
                string dataToLoad = "";

                using (FileStream stream = new FileStream(fullPath, FileMode.Open))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        dataToLoad = reader.ReadToEnd();
                    }
                }

                if (useEncryption)
                {
                    dataToLoad = EncryptDecrypt(dataToLoad);
                }

                loadedData = JsonUtility.FromJson<GameData>(dataToLoad);
            }
            catch (Exception e)
            {
                Debug.LogError("Error occured when trying to load file: " + fullPath + "\n" + e);
            }
        }

        return loadedData;
    }

    public void Save(GameData data, string profileID)
    {
        string fullPath = Path.Combine(dataDirPath, profileID);
        
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

            string dataToStore = JsonUtility.ToJson(data, true);

            if (useEncryption)
            {
                dataToStore = EncryptDecrypt(dataToStore);
            }

            using (FileStream stream = new FileStream(fullPath, FileMode.Create))
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.Write(dataToStore);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error occured when trying to save file: " + fullPath + "\n" + e);
        }
    }

    private string EncryptDecrypt(string data)
    {
        string modifiedData = "";
        for (int i = 0; i < data.Length; i++)
        {
            modifiedData += (char)(data[i] ^ encryptionCodeWord[i % encryptionCodeWord.Length]);
        }
        return modifiedData;
    }
}
