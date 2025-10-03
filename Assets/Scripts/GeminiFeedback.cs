using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public class GeminiRtsFeedback : MonoBehaviour
{
    public string apiKey;
    public TextAsset jsonLog;
    public TextAsset csvStats;

    public string promptTemplate = @"You are an AI assistant tasked with analyzing a naval real-time strategy (RTS) game scenario to provide targeted feedback for a player, but only if valid JSON and CSV data are provided. If the JSON or CSV data is missing, empty, or invalid, respond with: ""Error: Cannot generate feedback due to missing or invalid JSON or CSV data.""
The game involves commanding a fleet of naval units—JariUSV (fast, nimble vessels for scouting and harassment), SeaHunter (balanced units with moderate speed and firepower), DDG51 (heavily armed and armored destroyers for sustained combat), and Rig Balder (a stationary base)—to achieve victory by destroying the enemy’s base or eliminating all their units (red-colored), while protecting your own base and units (blue-colored). The battlefield is a 2D map divided into four quadrants, with the player’s base randomly placed in one quadrant and the enemy’s base hidden in another, obscured by a fog of war that requires scouting to reveal enemy positions and movements.
Damage taken is the total damage received by the player’s units (JariUSV, SeaHunter, DDG51) and base (Rig Balder), while damage dealt is the total damage inflicted on the opponent’s units and base. The score is calculated as: `score = (0.3 * (playerWon ? 1 : 0)) * 100 + 0.7 * (damageDealt / (damageDealt + damageTaken)) * 100`.
Players use selection controls and unit action commands (Move, AttackMoveToPosition, AttackMoveToEntity). Do not include camera controls in feedback.

You are provided with:
1) A JSON command log (timestamp, timeScale, commandType, entityIds, targetPosition, targetEntityId, targetEntityName, targetOwnerName, add).
2) A CSV with scenario stats (DateTime, StudentID, Group, GameType, Result, DamageTaken, DamageDealt, ScorePercent, TimeTaken, AILevel, AIDifficulty, WinCondition, PlayerBaseLocation, AIBaseLocation, unit counts).

Rules:
- Output format: Return exactly three lines numbered as a list: ""1. ...\n2. ...\n3. ..."" with no extra text before or after.
- Be concise; each point is a single sentence.
- Use rounded time references (early/mid/late game), not raw timestamps or coordinates.
- Don’t surface specific JSON coordinates/timestamps or raw data values.
- Base strategy on mechanics, CSV outcome/metrics, and JSON command patterns.
- Emphasize minimizing DamageTaken, maximizing DamageDealt, protecting Rig Balder, improving ScorePercent.
- Reference actionable inputs (F1/F2/F3/F4, A+Right Click, CTRL+0–9) when helpful.
- Include strategies: JariUSV scouting, DDG51 anchoring for damage mitigation, SeaHunter support, optimized Attack-Move usage, control-group discipline.
- Consider AI behavior (Adaptive, AIDifficulty/AILevel).
- If the player won, start with one positive reinforcement.
- Do NOT give camera controls in feedback.
- Treat a player command as any JSON item with commandType in {Move, AttackMoveToPosition, AttackMoveToEntity} and entityIds length > 0.
- If there are zero such player commands, do not describe player actions. Instead, acknowledge that no unit commands were issued and provide
";

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
