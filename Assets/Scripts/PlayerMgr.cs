using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Player
{
    public string name;
    public PlayerID playerId;
    public PlayerSide playerSide;
    public Color playerColor;

    public Player(string name, PlayerID playerId, PlayerSide playerSide, Color playerColor) {
        this.name = name;
        this.playerId = playerId;
        this.playerSide = playerSide;
        this.playerColor = playerColor;
    }
}

public class PlayerMgr : MonoBehaviour
{
    public static PlayerMgr inst;
    private void Awake() {
        inst = this;
        players.Clear();
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public Player adminPlayer;
    public Player player1;
    public Player player2;
    public Player observer;

    public List<Color> playerColors = new List<Color>();
    public List<Player> players = new List<Player>();

    // Update is called once per frame
    void Update()
    {
        
    }

    public Player CreatePlayer(string pname, PlayerID pid, PlayerSide side, Color c) {
        Player player = new Player(pname, pid, side, c);
        players.Add(player);
        return player;
    }

    public void DestroyPlayer(PlayerID playerID) {
        Player player = players.Find(x=>x.playerId == playerID);
        if(player != null) {
            players.Remove(player);
        }
    }

    public void DestroySide(PlayerSide side) {
        players.RemoveAll(x=>x.playerSide == side);
    }

    [ContextMenu("CreateAllPlayers")]
    public void CreateAllPlayers() {
        Player tmp;
        players.Clear();
        List<PlayerSide> sides = new List<PlayerSide>();
        foreach(PlayerSide ps in Enum.GetValues(typeof(PlayerSide))) {
            sides.Add(ps);
        }
        int i = 0;
        foreach(PlayerID pid in Enum.GetValues(typeof(PlayerID))) {
            tmp = CreatePlayer(pid.ToString(), pid, sides[i], playerColors[i]);
            i++;
        }
        player1 = players[(int) PlayerID.PlayerOne];
        player2 = players[(int) PlayerID.PlayerTwo];
        observer = players[(int) PlayerID.Observer];
        adminPlayer = players[(int) PlayerID.Admin];
    }
}
