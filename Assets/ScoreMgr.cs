using UnityEngine;
using TMPro;

public class ScoreMgr : MonoBehaviour
{
    public static ScoreMgr inst;
    public float damageDealt;
    public float damageTaken;
    public bool playerWon;
    public bool aiWon;
    private void Awake()
    {
        if (inst != null && inst != this)
            Destroy(gameObject);
        else
            inst = this;
    }

    public void CheckVictory()
    {
        if (!playerWon && !aiWon) return;
        OpenOceanMain.inst.lobbyState = OpenOceanMain.LobbyState.ScorePanel;
        UpdateScoreDisplay();
        LogVictoryMessage();
    }

    private void UpdateScoreDisplay()
    {
        if (OpenOceanMain.inst.damageDealtText != null)
            OpenOceanMain.inst.damageDealtText.text = $"{damageDealt:0}";

        if (OpenOceanMain.inst.damageTakenText != null)
            OpenOceanMain.inst.damageTakenText.text = $"{damageTaken:0}";

        if (OpenOceanMain.inst.winnerText != null)
            OpenOceanMain.inst.winnerText.text = playerWon ? "Player Victory!" : "AI Victory!";
    }



    private void LogVictoryMessage()
    {
        string message = playerWon ?
            $"PLAYER VICTORY! Damage Dealt: {damageDealt} | Taken: {damageTaken}" :
            $"AI VICTORY! Damage Dealt: {damageDealt} | Taken: {damageTaken}";

        Debug.Log(message);
    }
    
    public void ResetScores()
    {
        damageDealt = 0;
        damageTaken = 0;
        playerWon = false;
        aiWon = false;
    }
}