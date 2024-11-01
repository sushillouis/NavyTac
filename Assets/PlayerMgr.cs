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
        Debug.Log("Starting player mgr");
        ReadyPlayerAdmin();
        ReadyPlayer1();
        ReadyPlayer2();
        observer = CreateObserver();
    }

    public Player adminPlayer;
    void ReadyPlayerAdmin() {
        adminPlayer = CreatePlayer("Administrator", PlayerID.Admin, PlayerSide.Admin, Color.black);
    }
    public Player player1;
    public Player player2;
    void ReadyPlayer1() {
        player1 = CreatePlayer("One", PlayerID.PlayerOne, PlayerSide.SideOne, Color.blue);
    }

    void ReadyPlayer2() {
        player2 = CreatePlayer("Two", PlayerID.PlayerTwo, PlayerSide.SideTwo, Color.red);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public List<Player> players = new List<Player>();

    public Player CreatePlayer(string pname, PlayerID pid, PlayerSide side, Color c) {
        Player player = new Player(pname, pid, side, c);
        players.Add(player);
        return player;
    }

    public Player observer;
    public Player CreateObserver() {
        Player player = new Player("Observer", PlayerID.Observer, PlayerSide.Observer, Color.grey);
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

}
