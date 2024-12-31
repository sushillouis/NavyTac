using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameMgr : MonoBehaviour
{
    public static GameMgr inst;

    private void Awake()
    {
        inst = this;
    }

    // Start is called before the first frame update
    void Start()
    {

        EntityMgr.inst.movableEntitiesRoot.SetActive(false);

        plusButton.onClick.RemoveAllListeners();
        plusButton.onClick.AddListener(() => DeltaScale(1));
        minusButton.onClick.RemoveAllListeners();
        minusButton.onClick.AddListener(() => DeltaScale(-1));

    }

    public Vector3 position;
    public float spread = 20;
    public float colNum = 10;
    public float initZ;

    [SerializeField]
    private Button plusButton;
    [SerializeField]
    private Button minusButton;
    [SerializeField]
    private TextMeshProUGUI simSpeedButtonText;

    public float timeScale = 1;

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyUp(KeyCode.Equals)) {
            DeltaScale(1);
        }
        if(Input.GetKeyUp(KeyCode.Minus)) {
            DeltaScale(-1);
        }

    }

    public void DeltaScale(float delta) {
        if(Time.timeScale + delta >= 0) {
            Time.timeScale += delta;
            Time.timeScale = Mathf.Clamp(Time.timeScale, 0, 16);
            simSpeedButtonText.text = Time.timeScale.ToString("0");
        }
    }
    public void Create100()
    {
        initZ = position.z;
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                Entity ent = EntityMgr.inst.CreateEntity(EntityType.PilotVessel, position, Vector3.zero);
                position.z += spread;
            }
            position.x += spread;
            position.z = initZ;
        }
        DistanceMgr.inst.Initialize();
    }



    public void InitMapMenu() {
        List<Entity> allEntities = new List<Entity>();
        Vector3 position = Vector3.zero;
        Vector3 offset = new Vector3(100, 0, -50);

        foreach(GameObject go in EntityMgr.inst.entityPrefabs) {
            Entity ent = go.GetComponent<Entity>();
            ent = EntityMgr.inst.CreateEntity(ent.entityType, position + offset, new Vector3(0, 270, 0));
            allEntities.Add(ent);
            position.x += 400;
        }
        Vector3 pos = new Vector3(-3000, 0, 0);
        bool add = false;
        StartCoroutine(AddMoveCommandsToEnt(allEntities, pos, add, 270));
        pos.x = 3000;
        add = true;
        StartCoroutine(AddMoveCommandsToEnt(allEntities, pos, add));
    }


    IEnumerator AddMoveCommandsToEnt(List<Entity> allEntities, Vector3 movePos, bool shouldAdd, float heading = -1) {
        yield return new WaitForSeconds(0.1f);
        foreach(Entity ent in allEntities) {
            ent.heading = (heading == -1 ? ent.heading : heading);
            //ent.isSelected = true;
        }
        AIMgr.inst.HandleMove(allEntities, movePos, shouldAdd);
        //AIMgr.inst.HandleMove(allEntities, new Vector3(3000, 0, 0), true);
    }



    public void MakeMapEntities() {
        Vector3 pos = new Vector3(0, 0, 0);
        Entity ent;
        foreach(TactPlayer player in PlayerMgr.inst.players) {
            for(int i = 0; i < 5; i++) {
                ent = EntityMgr.inst.CreateEntity(EntityType.SeaHunter, pos, new Vector3(0, 0, 0), player);
                pos.x += 50;
            }
            pos.z += 100;
            pos.x = 0;
        }

    }

    public void OpenOcean1x1() {
        Vector3 posPlayer1 = new Vector3(0, 0, 0);
        // Vector3 posPlayer2 = new Vector3(0, 0, 1 * Utils.FromNauticalMiles);
        Vector3 posAiPlayer3 = new Vector3(0,0,1 * Utils.FromNauticalMiles);
        // TactPlayer aiP3 = PlayerMgr.inst.CreatePlayer("AI1",404,PlayerSide.SideThree,Color.green,true);
        MakeEntsForPlayer(posPlayer1, 0, PlayerMgr.inst.player1);
        // MakeEntsForPlayer(posPlayer2, 180, PlayerMgr.inst.player2);
        MakeEntsForPlayer(posAiPlayer3, 180, PlayerMgr.inst.player2);
    }

    public void MakeEntsForPlayer(Vector3 initPos, float initHeading, TactPlayer player) {
        Vector3 eulerAngles = new Vector3(0, initHeading, 0);
        Entity initEnt = EntityMgr.inst.CreateEntity(EntityType.CVN75, initPos, eulerAngles, player);
        Entity tmpEnt;

        //Escort on right
        Vector3 offset = initEnt.transform.right * 1000;
        tmpEnt = EntityMgr.inst.CreateEntity(EntityType.DDG51, initPos + offset, eulerAngles, player);


        //USV on right
        offset = tmpEnt.transform.right * 500;
        tmpEnt = EntityMgr.inst.CreateEntity(EntityType.SeaHunter, initPos + offset, eulerAngles, player);

        //USV in front
        offset = initEnt.transform.forward * 1000;
        tmpEnt = EntityMgr.inst.CreateEntity(EntityType.SeaHunter, initPos + offset, eulerAngles, player);

        //USV in behind
        offset = -initEnt.transform.forward * 1000;
        tmpEnt = EntityMgr.inst.CreateEntity(EntityType.SeaHunter, initPos + offset, eulerAngles, player);

        //Escort on left
        offset = -initEnt.transform.right * 1000;
        tmpEnt = EntityMgr.inst.CreateEntity(EntityType.DDG51, initPos + offset, eulerAngles, player);
        /*
        //USV on left
        offset = -initEnt.transform.right * 500;
        tmpEnt = EntityMgr.inst.CreateEntity(EntityType.SeaHunter, initPos + offset, eulerAngles, player);

        offset = initEnt.transform.forward * 500;
        offset.x -= 250;
        for(int i = 0; i < 5; i++) {
            tmpEnt = EntityMgr.inst.CreateEntity(EntityType.Mykola, initPos + offset, eulerAngles, player);
            offset.x += 100;
        }
        offset = -initEnt.transform.forward * 500;
        offset.x -= 200;
        for(int i = 0; i < 5; i++) {
            tmpEnt = EntityMgr.inst.CreateEntity(EntityType.Mykola, initPos + offset, eulerAngles, player);
            offset.x += 100;
        }
        */
    }

}
