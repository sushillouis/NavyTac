using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
using Whisper.Utils;
using Button = UnityEngine.UI.Button;
using Toggle = UnityEngine.UI.Toggle;
using Whisper;
using TMPro;
using System;

public class WhisperMic : MonoBehaviour
{
    public WhisperManager whisper;
    public MicrophoneRecord microphoneRecord;
    public StreamingToCommand streamingToCommand;
    public bool streamSegments = true;
    public bool printLanguage = false;
    // public TextToCommand textToCommand;

    [Header("UI Mandatory")] 
    public Button button;
    [Header("UI Optional")] 
    public Button clearButton;
    public TMP_Text buttonText;
    public Text outputText;
    public Text timeText;
    public Dropdown languageDropdown;
    public Toggle translateToggle;
    public ScrollRect scroll;

    private string _buffer;
    public bool debug = false;

    private void Awake()
    {
        button.image.color=Color.red;
        button.onClick.AddListener(OnButtonPressed);
        streamingToCommand.Init();
        if(clearButton) {
            clearButton.onClick.AddListener(ResetCommand);
            clearButton.gameObject.SetActive(true);
        }
        if (buttonText)
            buttonText.text = microphoneRecord.IsRecording ? "Stop" : "Record";


        if(languageDropdown)
        {
            languageDropdown.value = languageDropdown.options
                .FindIndex(op => op.text == whisper.language);
            languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        }
        if(translateToggle)
        {
            translateToggle.isOn = whisper.translateToEnglish;
            translateToggle.onValueChanged.AddListener(OnTranslateChanged);
        }
        microphoneRecord.OnRecordStop += Transcribe;
        
        if (streamSegments)
            whisper.OnNewSegment += WhisperOnOnNewSegment;
        whisper.OnProgress += OnProgressHandler;
    }

    private void ResetCommand() {
        streamingToCommand.Stop();
        streamingToCommand.Init();
    }

    private void OnButtonPressed()
    {
        if (!microphoneRecord.IsRecording)
        {
            button.image.color=Color.green;
            microphoneRecord.StartRecord();
            clearButton.gameObject.SetActive(false);
        }
        else
        {
            button.image.color=Color.red;
            microphoneRecord.StopRecord();
            clearButton.gameObject.SetActive(true);
            // streamingToCommand.Stop();
        }
        if (buttonText)
            buttonText.text = microphoneRecord.IsRecording ? "Stop" : "Record";
    }
    
    private void OnLanguageChanged(int ind)
    {
        var opt = languageDropdown.options[ind];
        whisper.language = opt.text;
    }
    
    private void OnTranslateChanged(bool translate)
    {
        whisper.translateToEnglish = translate;
    }

    public async void Transcribe(AudioChunk recordedAudio) {
        Transcribe(recordedAudio,-1);
    }

    public async void Transcribe(AudioChunk recordedAudio, int testID=-1)
    {
        _buffer = "";
        
        var sw = new Stopwatch();
        sw.Start();
        
        var res = await whisper.GetTextAsync(recordedAudio.Data, recordedAudio.Frequency, recordedAudio.Channels);

        var time = sw.ElapsedMilliseconds;
        var rate = recordedAudio.Length / (time * 0.001f);
        if(timeText)
            timeText.text = $"Time: {time} ms\nRate: {rate:F1}x";
        else if(debug){
            UnityEngine.Debug.Log($"Time: {time} ms\nRate: {rate:F1}x");
        }
        if (res == null)
            return;

        var text = res.Result;
        if (printLanguage)
            text += $"\n\nLanguage: {res.Language}";
        if(outputText)
            outputText.text = text;
        if(streamingToCommand) {
            streamingToCommand.ProcessResult(text,testID,time);
            // streamingToCommand.ProcessResult(text,testID,time);
        }
    }
    
    private void WhisperOnOnNewSegment(WhisperSegment segment)
    {
        _buffer += segment.Text;
        outputText.text = _buffer + "...";
    }
    
    private void OnProgressHandler(int progress)
    {
        if(timeText)
            timeText.text = $"Progress: {progress}%";
    }
}