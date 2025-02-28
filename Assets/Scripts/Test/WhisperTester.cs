using System;
using System.Collections;
using System.Collections.Generic;
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
        
        for (int i =0;i<testAudios.Count;i++) {
            streamingToCommand.Stop();
            streamingToCommand.Init();
            testInfo.Add(new(0,0,0));
            AudioChunk chunk = new()
            {
                Frequency = testAudios[i].frequency,
                Channels = testAudios[i].channels,
                Length = testAudios[i].length,
                IsVoiceDetected = true
            };
            chunk.Data = new float[testAudios[i].samples*10];
            testAudios[i].GetData(chunk.Data,0);
            Debug.Log(chunk.Data);
            mic.Transcribe(chunk,i);
            yield return new WaitForSeconds(1);
        }
    }

    public void ValidateResult(string result, int testID) {
        float accuracy = 0f;
        if(result.Contains(testAnswers[testID]) || testAnswers[testID].Contains(result)) {
            accuracy=1f;
        } else {
            Debug.Log($"Incorrect {result} is not {testAnswers[testID]}");
        }
        testInfo[testID] = new TestInfo(accuracy,testInfo[testID].time,testInfo[testID].frameRate);
    }

    public void LogTime(float time, float framerate, int testID) {
        testInfo[testID] = new TestInfo(testInfo[testID].accuracy,time,framerate);
    }

    [Serializable]
    public struct TestInfo {
        public float accuracy;
        public float time;
        public float frameRate;
        public TestInfo(float accuracy,float time,float frameRate) {
            this.accuracy = accuracy;
            this.time = time;
            this.frameRate = frameRate;
        }
    }
}
