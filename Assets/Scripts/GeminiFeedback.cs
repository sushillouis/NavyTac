using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public class GeminiRtsFeedback : MonoBehaviour
{
    [Header("AWS API Gateway URL (your existing Lambda endpoint)")]
    public string apiGatewayUrl = "https://1nht1d5r0h.execute-api.us-east-2.amazonaws.com/default/UnityS3Upload";

    public TextAsset jsonLog;
    public TextAsset csvStats;

    public string promptTemplate = @"You are an AI assistant tasked with analyzing a naval real-time strategy (RTS) game scenario and generating an After Action Review (AAR) for **you**, but only if valid JSON and CSV data are provided.
If the JSON or CSV data is missing, empty, or invalid, respond only with:
Error: Cannot generate feedback due to missing or invalid JSON or CSV data.

Scenario Context

The game involves commanding naval units—JariUSV (fast scouts), SeaHunter (balanced support), DDG51 (armored destroyers), and a stationary **command center**—to destroy the enemy base or eliminate all enemy units (red) while protecting your own (blue). The battlefield is a 2D map of four quadrants with fog of war hiding the enemy base.

A neutral base may also exist. Capturing it before the enemy increases your chances to win by granting control of additional entities.

Damage Taken: total damage received by **your** units and **command center**.

Damage Dealt: total damage inflicted on enemy units and base.

Score:

score = (0.3 * (playerWon ? 1 : 0)) * 100 
      + 0.7 * (damageDealt / (damageDealt + damageTaken)) * 100


**You** issue commands (Move, AttackMoveToPosition, AttackMoveToEntity) to units via selection controls; camera controls must never be mentioned.

Inputs Provided:

JSON command log: (timestamp, timeScale, commandType, entityIds, targetPosition, targetEntityId, targetEntityName, targetOwnerName, add).

CSV stats: (DateTime, StudentID, Group, GameType, Result, DamageTaken, DamageDealt, ScorePercent, TimeTaken, AILevel, AIDifficulty, WinCondition, PlayerBaseLocation, AIBaseLocation, unit counts).

Output Rules

Structure output as four Q&A items exactly matching the Navy AAR questions:

What did we expect to happen?

One clear sentence describing the intended win condition or plan.

What actually happened?

One clear sentence stating whether **you** won or lost and what occurred.

If no unit commands (commandType in {Move, AttackMoveToPosition, AttackMoveToEntity} with entityIds > 0), explicitly acknowledge that.

Why did it happen?

Provide 2–4 numbered reasons, each ≈10 words maximum.

Reasons should tie to strategy, unit usage, command discipline, scouting, AI behavior, and neutral base control.

Never mention ""adaptive"" or ""non-adaptive"" AI.

What will we do to improve?

Provide 2–4 numbered improvements, each ≈10 words maximum.

**Improvements MUST directly correct the mistakes identified in 'Why did it happen?'.**

For example, if the analysis showed 'Attack-Move only', suggest 'Use focused fire (A+Right Click) on key targets'.
If the analysis showed 'one large fleet', suggest 'Use unit groups (CTRL+0-9) for flanks or scouting'.

Emphasize minimizing DamageTaken, maximizing DamageDealt, protecting the **command center**, and improving ScorePercent.

When relevant, mention actionable inputs (F1/F2/F3/F4, A+Right Click, CTRL+0–9) but not camera controls.

Use rounded phases (""early/mid/late game""), not raw data, timestamps, or coordinates.

Do not show raw JSON/CSV values.

If **you** won, the first section should still recognize the expectation of a win condition.

Keep responses concise, objective, and actionable.

**Scenario-Specific Logic Rules:**

1.  **Neutral Base:** Only mention capturing the neutral base if the CSV `unit counts` or `WinCondition` data indicates it was present in the scenario. If it was not present, do not mention it.
2.  **AI Difficulty (Attack-Retreat):** If the CSV `AIDifficulty` is greater than 0.25, your analysis in 'Why did it happen?' should consider that the AI uses 'attack-retreat' tactics.
3.  **AI Difficulty (Scouting):** If `AIDifficulty` is greater than 0.33 AND a neutral base was present, 'What will we do to improve?' should suggest sending scouts to the neutral base early to contest it before the AI arrives.
";

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
        if (string.IsNullOrWhiteSpace(apiGatewayUrl))
        {
            Debug.LogError("API Gateway URL missing.");
            return "Error: API Gateway URL missing.";
        }
        if (string.IsNullOrWhiteSpace(jsonData) || string.IsNullOrWhiteSpace(csvData))
        {
            Debug.LogError("JSON or CSV data is missing or empty.");
            return "Error: Cannot generate feedback due to missing or invalid JSON or CSV data.";
        }

        // Build the request body matching the Lambda's generateFeedback action
        var body = new JObject {
            ["action"] = "generateFeedback",
            ["systemPrompt"] = promptTemplate,
            ["jsonData"] = jsonData,
            ["csvData"] = csvData
        };

        string url = apiGatewayUrl;
        using var req = new UnityWebRequest(url, "POST");
        byte[] payload = Encoding.UTF8.GetBytes(body.ToString());
        req.uploadHandler   = new UploadHandlerRaw(payload);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        requestStartTime = Time.time;
        Debug.Log($"Sending Bedrock request at time: {requestStartTime}");

        var op = req.SendWebRequest();
        while (!op.isDone) { await Task.Yield(); }

        responseTime = Time.time;
        float totalTime = responseTime - requestStartTime;
        Debug.Log($"Bedrock request completed at time: {responseTime}, Total time: {totalTime} seconds");

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Bedrock request failed: {req.responseCode} {req.error}\n{req.downloadHandler.text}");
            return $"Error: Bedrock request failed ({req.responseCode}).";
        }

        try
        {
            var resp = JObject.Parse(req.downloadHandler.text);

            // Check if Lambda returned an error
            bool ok = resp["ok"]?.Value<bool>() ?? false;
            if (!ok)
            {
                string error = (string)resp["error"] ?? "Unknown error";
                Debug.LogError($"Lambda error: {error}");
                return $"Error: {error}";
            }

            string text = (string)resp["feedback"];

            if (string.IsNullOrWhiteSpace(text))
            {
                Debug.LogWarning("No feedback text returned. Full response:\n" + resp.ToString());
                return "Error: No feedback generated.";
            }
            else
            {
                // Log token usage if available
                var usage = resp["usage"];
                if (usage != null)
                {
                    int inputTokens = usage["inputTokens"]?.Value<int>() ?? 0;
                    int outputTokens = usage["outputTokens"]?.Value<int>() ?? 0;
                    Debug.Log($"Token usage - Input: {inputTokens}, Output: {outputTokens}");
                }

                Debug.Log("Feedback:\n" + text);
                onFeedbackReady?.Invoke(text);
                return text;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to parse Bedrock response: " + e);
            return "Error: Failed to parse Bedrock response.";
        }
    }
}