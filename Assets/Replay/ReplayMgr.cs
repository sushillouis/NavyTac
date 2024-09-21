using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.XR.OpenVR;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;

[System.Serializable]
public class ReplayMgr : MonoBehaviour
{
    public static ReplayMgr inst;
    private GameData gameData; //holds the data for teh current state when saving/loading
    private FileDataHandler dataHandler; //object that handles writing/reading data files
    public bool useEncryption = false; //if we want to encrypt the data files we can
    public int stateID; //the id of the last state we loaded
    public int maxStateID; //the id of the last chronological 
    public string selectedProfileID; //the file name of the current state
    public bool record; //sets whether or not we want to record a frame each second

    private bool add; //flag for setting commands after the data has been loaded in
    private List<UnitAI> uais; //used for identifying what entity a commnand belongs to 
    private List<Command> commands; //commands to be added after loading

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

        //this is the directory of where all the json files for the states are being saved.
        //unity has a predetermined path for game data and then we're saving the data in a
        //folder called "saves" in that directory
        string path = Path.Combine(Application.persistentDataPath, "saves");  
        dataHandler = new FileDataHandler(path, useEncryption);

        add = false;
        uais = new List<UnitAI>();
        commands = new List<Command>();
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

    private void LateUpdate()
    {
        if(add)
        {
            LoadCommands();
        }
    }

    //handles keeping track of where a state is in terms of chronological order when saving
    public void HandleSaveState()
    {
        //sets the name of the new file to be created
        selectedProfileID = "state" + (stateID + 1) + ".game";

        SaveData();

        //updates the current state as well as the max state
        //note: the way this is set up makes it so that if you're saving at a state
        //that isn't the latest chronological state, all states after the state that was
        //saved in (i.e. states where stateID is greater than what it is before this incrementation)
        //those states will be lost to the replay and eventually overwritten
        stateID++;
        maxStateID = stateID;
    }

    //loads the next state in chronological order from the last state that was loaded
    public void LoadForward()
    {
        //if we're at the most recent state, don't load anything
        if (stateID == maxStateID)
        {
            Debug.Log("At max state");
            return;
        }

        //updates the state to the next state
        stateID++;
        selectedProfileID = "state" + stateID + ".game";
        LoadData();

    }

    //loads the previous state in chronological order from the last state that was loaded
    public void LoadBackward()
    {
        //if we're at the first state, don't need to decrement stateID, just reload the first state
        if (stateID <= 0)
        {
            Debug.Log("At min state");
        }
        //otherwise decrement and set the file name
        else
        {
            stateID--;
            selectedProfileID = "state" + stateID + ".game";
        }

        LoadData();
    }

    //takes the data from a json file and recreates the scene from it
    public void LoadData()
    {
        //Loads in new data from the json file of the state to be loaded
        gameData = dataHandler.Load(selectedProfileID); 

        //resets the scene
        LineMgr.inst.DestroyAllLines();
        EntityMgr.inst.ResetEntities();
        EntityMgr.entityId = gameData.entityID - (gameData.entityIndex.Count);

        //sets camera to correct position/orientation
        CameraMgr.inst.YawNode.transform.position = gameData.camPosition;
        CameraMgr.inst.YawNode.transform.eulerAngles = new Vector3(0, gameData.yaw, 0);
        CameraMgr.inst.PitchNode.transform.eulerAngles = new Vector3(gameData.pitch, 0, 0);

        //loads in entities
        for (int i = 0; i < gameData.entityIndex.Count; i++)
        {
            Entity ent = EntityMgr.inst.CreateEntity((EntityType)gameData.entityType[i], gameData.position[i], new Vector3(0, gameData.heading[i], 0));
            ent.velocity = gameData.velocity[i];
            ent.speed = gameData.speed[i];
            ent.desiredSpeed = gameData.ds[i];
            ent.heading = gameData.heading[i];
            ent.desiredHeading = gameData.dh[i];
        }

        //loads in commands for each entity
        foreach (Entity ent in EntityMgr.inst.entities)
        {
            //finds all commands associated with the current entity in the loop
            List<int> dataIndexes = new List<int>();
            for (int j = 0; j < gameData.commandEntIndex.Count; j++)
            {
                if (gameData.commandEntIndex[j] == EntityMgr.inst.entities.IndexOf(ent))
                {
                    dataIndexes.Add(j);
                }
            }

            //recreates those commands 
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

                //commands and their corresponding UnitAI are stored in a list to be added after the scene is created
                uais.Add(uai);
                commands.Add(newCmd);
            }
        }

        //sets the flag to add commands
        add = true;

        //since we've recreated all entities we have to reinitialize the DistanceMgr
        DistanceMgr.inst.Initialize();
    }

    //loads commands into their respective entities after data loading is done
    //note: this is a seperate method from load data due to the fact that when trying to add commands
    //to an entity right when it's instantiated, the commands are added to the list in one frame but then
    //the next frame the commands list is empty. The workaround I found for this bug was to just add the commands
    //later in the frame via LateUpdate but I'm not sure if there's a solution where you can add commands at the
    //same time as the entites.
    public void LoadCommands()
    {
        //removes all lines from the scene as all the corresponding commands to those lines are now gone
        LineMgr.inst.DestroyAllLines();

        //adds all commands to their corresponding entites and initializes their lines
        for (int i = 0; i < uais.Count; i++)
        {
            uais[i].AddCommand(commands[i]);
            uais[i].DecorateAll();
        }

        //unsets flag to add commands
        add = false;

        //clears out the cache of commands to be added
        commands.Clear();
        uais.Clear();
    }

    //takes the data from the current state and saves it to a json file
    public void SaveData()
    {
        Debug.Log("saved");

        //clears the data object so new data can be stored
        gameData.Clear();

        //keeps track of the current entityId is EntityMgr so entities can be renamed the same on load
        gameData.entityID = EntityMgr.entityId;

        //save camera position/location
        gameData.camPosition = CameraMgr.inst.YawNode.transform.position;
        gameData.yaw = CameraMgr.inst.YawNode.transform.eulerAngles.y;
        gameData.pitch = CameraMgr.inst.PitchNode.transform.eulerAngles.x;

        //iterates through all entities in the scene
        foreach (Entity ent in EntityMgr.inst.entities)
        {
            //collects the data of the entity itself
            gameData.entityIndex.Add(EntityMgr.inst.entities.IndexOf(ent));
            gameData.entityType.Add((int)ent.entityType);
            gameData.position.Add(ent.position);
            gameData.velocity.Add(ent.velocity);
            gameData.speed.Add(ent.speed);
            gameData.ds.Add(ent.desiredSpeed);
            gameData.heading.Add(ent.heading);
            gameData.dh.Add(ent.desiredHeading);

            //gets the UnitAI so commands can be found
            UnitAI uai = ent.GetComponent<UnitAI>();

            //iterates through each command of the current entity
            foreach (Command cmd in uai.commands)
            {
                //determines whether a command is a move, follow, or intercept and sets commands accordingly
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

                //general data for commands
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
    //Parameter for the EntityMgr
    public int entityID;

    //Camera Parameters
    public Vector3 camPosition;
    public float yaw;
    public float pitch;

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
    public List<int> followTargetIndex; //for move this is -1
    public List<Vector3> followOffset; //for move and intercept this is Vector3.zero

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
