using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public class GeminiRtsFeedback : MonoBehaviour
{
    public string apiKey;
    public TextAsset jsonLog;
    public TextAsset csvStats;

    public string promptTemplate = @"You are an AI assistant tasked with analyzing a naval real-time strategy (RTS) game scenario and generating an After Action Review (AAR) for the player, but only if valid JSON and CSV data are provided.
If the JSON or CSV data is missing, empty, or invalid, respond only with:
Error: Cannot generate feedback due to missing or invalid JSON or CSV data.

Scenario Context

The game involves commanding naval units—JariUSV (fast scouts), SeaHunter (balanced support), DDG51 (armored destroyers), and a stationary command center/oil rig—to destroy the enemy base or eliminate all enemy units (red) while protecting your own (blue). The battlefield is a 2D map of four quadrants with fog of war hiding the enemy base.

A neutral base may also exist. Capturing it before the enemy increases your chances to win by granting control of additional entities.

Damage Taken: total damage received by player’s units and command center/oil rig.

Damage Dealt: total damage inflicted on enemy units and base.

Score:

score = (0.3 * (playerWon ? 1 : 0)) * 100 
      + 0.7 * (damageDealt / (damageDealt + damageTaken)) * 100


Players issue commands (Move, AttackMoveToPosition, AttackMoveToEntity) to units via selection controls; camera controls must never be mentioned.

Inputs Provided:

JSON command log: (timestamp, timeScale, commandType, entityIds, targetPosition, targetEntityId, targetEntityName, targetOwnerName, add).

CSV stats: (DateTime, StudentID, Group, GameType, Result, DamageTaken, DamageDealt, ScorePercent, TimeTaken, AILevel, AIDifficulty, WinCondition, PlayerBaseLocation, AIBaseLocation, unit counts).

Output Rules

Structure output as four Q&A items exactly matching the Navy AAR questions:

What did we expect to happen?

One clear sentence describing the intended win condition or plan.

What actually happened?

One clear sentence stating whether the player won or lost and what occurred.

If no unit commands (commandType in {Move, AttackMoveToPosition, AttackMoveToEntity} with entityIds > 0), explicitly acknowledge that.

Why did it happen?

Provide 2–4 numbered reasons, each ≈10 words maximum.

Reasons should tie to strategy, unit usage, command discipline, scouting, AI behavior, and neutral base control.

Never mention “adaptive” or “non-adaptive” AI.

What will we do to improve?

Provide 2–4 numbered improvements, each ≈10 words maximum.

Emphasize minimizing DamageTaken, maximizing DamageDealt, capturing the neutral base early, protecting the command center/oil rig, and improving ScorePercent.

When relevant, mention actionable inputs (F1/F2/F3/F4, A+Right Click, CTRL+0–9) but not camera controls.

Use rounded phases (“early/mid/late game”), not raw data, timestamps, or coordinates.

Do not show raw JSON/CSV values.

If the player won, the first section should still recognize the expectation of a win condition.

Keep responses concise, objective, and actionable.";

    const string Model = "gemini-2.5-flash";
    string Endpoint(string key) => $"https://generativelanguage.googleapis.com/v1beta/models/{Model}:generateContent?key={key}";

    private float requestStartTime;
    private float responseTime;

    [ContextMenu("Run ▶ Generate Feedback (Inspector data)")]
    public void GenerateFeedback() => _ = GenerateFeedbackAsync(
        jsonLog != null ? jsonLog.text : string.Empty,
        csvStats != null ? csvStats.text : string.Empty,
        (text) => Debug.Log("Feedback:\n" + text)
    );

    private void Start() { }

    public async Task<string> GenerateFeedbackAsync(string jsonData, string csvData, System.Action<string> onFeedbackReady = null)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Debug.LogError("API key missing.");
            return "Error: API key missing.";
        }
        if (string.IsNullOrWhiteSpace(jsonData) || string.IsNullOrWhiteSpace(csvData))
        {
            Debug.LogError("JSON or CSV data is missing or empty.");
            return "Error: Cannot generate feedback due to missing or invalid JSON or CSV data.";
        }

        string finalPrompt = $"{promptTemplate}\n\nJSON Command Log:\n{jsonData}\n\nCSV Stats:\n{csvData}";

        var body = new JObject {
            ["contents"] = new JArray {
                new JObject {
                    ["parts"] = new JArray {
                        new JObject { ["text"] = finalPrompt }
                    }
                }
            }
        };

        string url = Endpoint(apiKey);
        using var req = new UnityWebRequest(url, "POST");
        byte[] payload = Encoding.UTF8.GetBytes(body.ToString());
        req.uploadHandler   = new UploadHandlerRaw(payload);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        requestStartTime = Time.time;
        Debug.Log($"Sending Gemini request at time: {requestStartTime}");

        var op = req.SendWebRequest();
        while (!op.isDone) { await Task.Yield(); }

        responseTime = Time.time;
        float totalTime = responseTime - requestStartTime;
        Debug.Log($"Gemini request completed at time: {responseTime}, Total time: {totalTime} seconds");

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Gemini request failed: {req.responseCode} {req.error}\n{req.downloadHandler.text}");
            return $"Error: Gemini request failed ({req.responseCode}).";
        }

        try
        {
            var resp = JObject.Parse(req.downloadHandler.text);
            string text = (string)resp["candidates"]?[0]?["content"]?["parts"]?[0]?["text"];

            if (string.IsNullOrWhiteSpace(text))
            {
                Debug.LogWarning("No text returned. Full response:\n" + resp.ToString());
                return "Error: No feedback generated.";
            }
            else
            {
                Debug.Log("Feedback:\n" + text);
                onFeedbackReady?.Invoke(text);
                return text;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to parse Gemini response: " + e);
            return "Error: Failed to parse Gemini response.";
        }
    }
}
