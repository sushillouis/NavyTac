using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Whisper.Utils;

public class WhisperTester : MonoBehaviour
{
    [SerializeField] List<AudioClip> testAudios;
    [SerializeField] List<string> testAnswers;
    [SerializeField] List<TestInfo> testInfo;
    [SerializeField] WhisperMic mic;
    [SerializeField] StreamingToCommand streamingToCommand;
    [SerializeField] string finalOutput;
    public Vector2Int testRange;
    public int repeats = 10;
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(nameof(TestWhisper),0);
    }

    // Update is called once per frame
    void Update()
    {
        
        
    }

    private IEnumerator TestWhisper() {
        mic.debug=true;

        yield return new WaitForSeconds(1);
        
        for (int i =0;i<testAnswers.Count*repeats;i++) {
            int j = i%testAnswers.Count;
            int k = (i%(testRange.y-testRange.x))+testRange.x;
            Debug.Log(j);
            streamingToCommand.Stop();
            streamingToCommand.Init();
            if(i<testAnswers.Count) {
                testInfo.Add(new(0,0,0,0));
            }
            AudioChunk chunk = new()
            {
                Frequency = testAudios[k].frequency,
                Channels = testAudios[k].channels,
                Length = testAudios[k].length,
                IsVoiceDetected = true,
                Data = new float[testAudios[k].samples*testAudios[k].channels]
            };
            testAudios[k].GetData(chunk.Data,0);
            // Debug.Log(chunk.Data);
            mic.Transcribe(chunk,j);
            yield return new WaitForSeconds(1);
        }
        string output = "";
        foreach (TestInfo test in testInfo)
        {
            output+=$"{test.accuracy},{test.time},{test.frameRate}\n";
        }
        finalOutput=output;
    }

    public void ValidateResult(string result,float time, float frameRate, int testID) {
        float accuracy = 0f;
        if(result.Contains(testAnswers[testID])) {
            accuracy=1f;
        } else {
            Debug.Log($"Incorrect {result} is not {testAnswers[testID]}");
            // string[] vs = result.Split(new char[] { ' ', '-', '/', '(', ')' },StringSplitOptions.RemoveEmptyEntries);
            // string[] vs1 = testAnswers[testID].Split(new char[] { ' ', '-', '/', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
            // accuracy = vs.Intersect(vs1, StringComparer.OrdinalIgnoreCase).Count()/ vs.Count();
            accuracy = (float)(testAnswers[testID].Length - LevenshteinDistance.Compute(result, testAnswers[testID]))/testAnswers[testID].Length;
        }
        
        time = Mathf.Clamp(time,0.5f,float.MaxValue);

        if(testInfo[testID].averagingCount>0) {
            Debug.Log($"Update! with{testID}");
            TestInfo temp = testInfo[testID];
            temp.AddNewData(accuracy,time,frameRate);
            testInfo[testID]  = temp;
        } else {
            testInfo[testID] = new(accuracy,time,frameRate);
        }
    }


    [Serializable]
    public struct TestInfo {
        public float accuracy;
        public float time;
        public float frameRate;
        public float averagingCount;
        public TestInfo(float accuracy=0,float time=0,float frameRate=0,float averagingCount=1) {
            this.accuracy = accuracy;
            this.time = time;
            this.frameRate = frameRate;
            this.averagingCount=averagingCount;
        }

        public void AddNewData(float n_accuracy,float n_time, float n_frameRate) {
            this.accuracy = (this.accuracy * (this.averagingCount/(this.averagingCount+1))) 
                + (1/(this.averagingCount+1)*n_accuracy);
            this.time = (this.time * (this.averagingCount/(this.averagingCount+1)))
                + (1/(this.averagingCount+1)*n_time);
            this.frameRate = (this.frameRate * (this.averagingCount/(this.averagingCount+1)))
                + (1/(this.averagingCount+1)*n_frameRate);
            this.averagingCount = averagingCount+1;
        }
    }
}

static class LevenshteinDistance
{
    public static int Compute(string s, string t)
    {
        if (string.IsNullOrEmpty(s))
        {
            if (string.IsNullOrEmpty(t))
                return 0;
            return t.Length;
        }

        if (string.IsNullOrEmpty(t))
        {
            return s.Length;
        }

        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];

        // initialize the top and right of the table to 0, 1, 2, ...
        for (int i = 0; i <= n; d[i, 0] = i++);
        for (int j = 1; j <= m; d[0, j] = j++);

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                int min1 = d[i - 1, j] + 1;
                int min2 = d[i, j - 1] + 1;
                int min3 = d[i - 1, j - 1] + cost;
                d[i, j] = Math.Min(Math.Min(min1, min2), min3);
            }
        }
        return d[n, m];
    }
}
