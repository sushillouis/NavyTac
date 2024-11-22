using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public class Player
{
    public string name;
    public ulong playerId;
    public PlayerSide playerSide;
    public Color playerColor;
    public bool isObserver;

    public Player(string name, ulong playerId, PlayerSide playerSide, Color playerColor) {
        Init(name, playerId, playerSide, playerColor, false);
    }

    public Player(string name, ulong playerId, PlayerSide playerSide, Color playerColor, bool isObserver) {
        Init(name, playerId, playerSide, playerColor, isObserver);
    }
    void Init(string name, ulong playerId, PlayerSide playerSide, Color playerColor, bool isObserver) {
        this.name = name;
        this.playerId = playerId;
        this.playerSide = playerSide;
        this.playerColor = playerColor;
        this.isObserver = isObserver;
    }
}

public class PlayerMgr : MonoBehaviour
{
    public static PlayerMgr inst;
    private void Awake() {
        inst = this;
        
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    [SerializeField] private int maxPlayers = 10; //includes observer and admin

    public Player player1;
    public Player player2;

    public List<Color> playerColors = new List<Color>();
    public List<Player> players = new List<Player>();
    List<PlayerSide> sides = new List<PlayerSide>();

    // Update is called once per frame
    void Update()
    {
        
    }

    public Player CreateAddPlayer(string pname, ulong pid, PlayerSide side, Color c) {
        Player player = new Player(pname, pid, side, c);
        players.Add(player);
        return player;
    }

    public void DestroyPlayer(ulong playerID) {
        Player player = players.Find(x => x.playerId == playerID);
        if(player != null) {
            players.Remove(player);
        }
    }

    public void DestroySide(PlayerSide side) {
        players.RemoveAll(x => x.playerSide == side);
    }

    [ContextMenu("CreateAllPlayers")]
    public void CreateAllPlayers() {
        Player tmp;
        players.Clear();

        foreach(PlayerSide ps in Enum.GetValues(typeof(PlayerSide))) {
            sides.Add(ps);
        }
        int i = 0;
        for(ulong pid  = 0; pid < (ulong) maxPlayers; pid++) {
            tmp = CreateAddPlayer("AI" + pid, 999, sides[i], playerColors[i]);
            i++;
        }
        SetupSpecialPlayers();
    }

    public void SetupSpecialPlayers() {
        Debug.Log("Count: " + players.Count);
        if(players.Count <= 8 && players.Count > 0) {
            player1 = players[0];
            player2 = players[1];
        }
    }


    public void AddNetClientPlayer(ulong clientID) {
        Player tmp = players.Find(x => x.playerId == 999);
        if(tmp != null) {
            tmp.playerId = clientID;
        }
    }

}
