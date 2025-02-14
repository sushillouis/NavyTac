using UnityEngine;
using UnityEngine.UI;
using Whisper.Utils;
using Button = UnityEngine.UI.Button;
using Toggle = UnityEngine.UI.Toggle;
using Whisper;
using TMPro;
using System;
using Unity.Jobs.LowLevel.Unsafe;

public class StreamingMic : MonoBehaviour
{
    public WhisperManager whisper;
    public MicrophoneRecord microphoneRecord;

    [Header("UI")] 
    public Button button;
    public TMP_Text buttonText;
    [SerializeField] StreamingToCommand streamingToCommand;
    [Header("Optional")]
    public Text text;
    public ScrollRect scroll;
    private WhisperStream _stream;

    private async void Start() {
        _stream = await whisper.CreateStream(microphoneRecord);
        _stream.OnResultUpdated += OnResult;
        _stream.OnSegmentUpdated += OnSegmentUpdated;
        _stream.OnSegmentFinished += OnSegmentFinished;
        _stream.OnStreamFinished += OnFinished;

        microphoneRecord.OnRecordStop += OnRecordStop;
        button.onClick.AddListener(OnButtonPressed);
        buttonText.text = "Record";
        // button.set = Color.red;
    }

    private void OnButtonPressed() {
        if (!microphoneRecord.IsRecording)
        {
            streamingToCommand.Init();
            _stream.StartStream();
            microphoneRecord.StartRecord();
        }
        else {
            streamingToCommand.Stop();
            microphoneRecord.StopRecord();
        }
    
        buttonText.text = microphoneRecord.IsRecording ? "Stop" : "Record";
    }

    private void OnRecordStop(AudioChunk recordedAudio) {
        buttonText.text = "Record";
    }

    private void OnResult(string result) {
        if(text) {
            text.text = result;
        } else if (whisper.logLevel == LogLevel.Verbose){
            Debug.Log("On Result "+result);
        }
        if(scroll) {
            UiUtils.ScrollDown(scroll);
        }
    }
    
    private void OnSegmentUpdated(WhisperResult segment) {
        print($"Segment updated: {segment.Result}");
    }
    
    private void OnSegmentFinished(WhisperResult segment) {
        print($"Segment finished: {segment.Result}");
        streamingToCommand.ProcessResult(segment.Result);
        if(segment.Result.Length>2000) {
            microphoneRecord.StopRecord();
            microphoneRecord.StartRecord();
        }
    }
    
    private void OnFinished(string finalResult) {
        print("Stream finished!");
    }
}