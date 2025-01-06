using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[Serializable]
public class TactPlayer
{
    public string name;
    public ulong playerId;
    public PlayerSide playerSide;
    public Color playerColor;
    public bool isObserver;
    public bool isAdmin;
    public bool isBot;
    public BotAI botAI;

    public TactPlayer(string name, ulong playerId, PlayerSide playerSide, Color playerColor, bool isObserver = false, bool isBot = false) {
        Init(name, playerId, playerSide, playerColor, isObserver, isBot);
    }
    void Init(string name, ulong playerId, PlayerSide playerSide, Color playerColor, bool isObserver, bool isBot) {
        this.name = name;
        this.playerId = playerId;
        this.playerSide = playerSide;
        this.playerColor = playerColor;
        this.isObserver = isObserver;
        this.isAdmin = true; //while developing
        this.isBot=isBot;
        if(isBot) {
            this.botAI = new BotAI(this);
        }
    }

    public override string ToString() {
        return "Name: " + name + ", id: " + playerId + ", Side: " + playerSide;
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
    //For testing;
    public TactPlayer player1;
    public TactPlayer player2;
    public TactPlayer localPlayer;


    //Not for testing
    [SerializeField] private int maxPlayers = 10; //includes observer and admin
    [SerializeField] private int playerCount = 0;
    public List<TactPlayer> players = new List<TactPlayer>();
    [SerializeField] private List<Color> playerColors = new List<Color>();
    [SerializeField] private List<PlayerSide> sides = new List<PlayerSide>();


    // Update is called once per frame
    void Update()
    {
        foreach (TactPlayer player in players)
        {
            if(player.isBot) {
                player.botAI.Tick();
            }
        }
    }

    public TactPlayer CreateAddPlayer(string pname, ulong pid, PlayerSide side, Color c) {
        TactPlayer player = new TactPlayer(pname, pid, side, c);
        players.Add(player);
        return player;
    }

    public TactPlayer CreatePlayer(string pname, ulong pid, PlayerSide side, Color c,bool isBot = false) {
        TactPlayer player = new TactPlayer(pname, pid, side, c, false, isBot);
        return player;
    }

    public void AddPlayer(TactPlayer player) {
        players.Add(player);
    }
    public void DestroyPlayer(ulong playerID) {
        TactPlayer player = players.Find(x => x.playerId == playerID);
        if(player != null) {
            players.Remove(player);
        }
    }

    public void DestroySide(PlayerSide side) {
        players.RemoveAll(x => x.playerSide == side);
    }

    [ContextMenu("CreateSides")]
    public void CreateSides() {
        players.Clear();
        sides.Clear();
        foreach(PlayerSide ps in Enum.GetValues(typeof(PlayerSide))) {
            sides.Add(ps);
        }
    }

    public TactPlayer CreateNetClientPlayer(ulong clientID) {
        if(playerCount < maxPlayers) {
            TactPlayer player = CreatePlayer(OpenOceanMain.inst.playerName, clientID, sides[playerCount], playerColors[playerCount]);
            AddTestPlayer1And2(player, playerCount); //For testing
            playerCount++;
            NetDebugConsole.inst.Log("Added player: \n" + player.ToString());
            return player;
        }
        return null;
    }

    public TactPlayer CreateSinglePlayer(string name) {
        if(playerCount < maxPlayers) {
            TactPlayer player = CreatePlayer(name, (ulong) playerCount, sides[playerCount], playerColors[playerCount]);
            AddTestPlayer1And2(player, playerCount);
            playerCount++;
            Debug.Log("Added player: " + player.ToString());
            return player;
        }
        return null;
    }

    public TactPlayer CreateAIPlayer(string name) {
        if(playerCount < maxPlayers) {
            TactPlayer player = CreatePlayer(name, (ulong) playerCount, sides[playerCount], playerColors[playerCount],true);
            AddTestPlayer1And2(player, playerCount);
            playerCount++;
            Debug.Log("Added player: " + player.ToString());
            return player;
        }
        return null;
    }



    public TactPlayer GetPlayer(ulong clientID) {
        if(players.Exists(x => x.playerId == clientID)) {
            return players.Find(x => x.playerId == clientID);
        }
        return null;
    }


    private void AddTestPlayer1And2(TactPlayer player, int count) {
        if(count == 0)
            player1 = player;
        if(count == 1)
            player2 = player;
    }

    public void RenamePlayer(string name, ulong cid) {
        TactPlayer player = players.Find(x=> x.playerId == cid);
        player.name = name;
    }

    public string StringAllPlayers() {
        StringBuilder sb = new StringBuilder();
        foreach(TactPlayer player in players) {
            sb.Append(player.ToString());
            sb.AppendLine();
        }
        return sb.ToString();
        }
}
/*

[Serializable]
public struct PlayerStruct : INetworkSerializable, IEquatable<PlayerStruct>
{
    public FixedString64Bytes playerName;
    public ulong playerId;
    public PlayerSide playerSide;
    public bool isObserver;
    public bool isAdmin;

    public bool Equals(PlayerStruct other) {
        return (playerId == other.playerId
            &&  playerName.Equals(other.playerName)
            && playerSide == other.playerSide
            && isAdmin == other.isAdmin
            && isObserver == other.isObserver);
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        serializer.SerializeValue(ref playerName);
        serializer.SerializeValue(ref playerId);
        serializer.SerializeValue(ref playerSide);
        serializer.SerializeValue(ref isObserver);
        serializer.SerializeValue(ref isAdmin);
    }

    public override string ToString() {
        return playerName.ToString() + ", " + playerId;
    }
} 

    public PlayerStruct CreateNetClientPlayerStruct(ulong clientID) {
        if(playerCount < 7) {
            PlayerStruct player = new PlayerStruct
            {
                playerName = "NoName",
                playerId = clientID,
                playerSide = sides[playerCount],
                isAdmin = false,
                isObserver = false,
            };
            playerCount++;
            NetDebugConsole.inst.Log("Added player: " + player.playerId);
            return player;
        }
        return new PlayerStruct { };
    }

 
 */