using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using Whisper.Utils;
using TMPro;
public class AltMicRecord : MonoBehaviour
{
    [Tooltip("Max length of recorded audio from microphone in seconds")]
    public int maxLengthSec = 60;

    [Tooltip("After reaching max length microphone record will continue")]
    public bool loop;

    [Tooltip("Microphone sample rate")]
    public int frequency = 16000;

    [Tooltip("Length of audio chunks in seconds, useful for streaming")]
    public float chunksLengthSec = 0.5f;

    [Tooltip("Should microphone play echo when recording is complete?")]
    public bool echo = true;

    [Header("Voice Activity Detection (VAD)")]
    [Tooltip("Should microphone check if audio input has speech?")]
    public bool useVad = true;

    [Tooltip("How often VAD checks if current audio chunk has speech")]
    public float vadUpdateRateSec = 0.1f;

    [Tooltip("Seconds of audio record that VAD uses to check if chunk has speech")]
    public float vadContextSec = 30f;

    [Tooltip("Window size where VAD tries to detect speech")]
    public float vadLastSec = 1.25f;

    [Tooltip("Threshold of VAD energy activation")]
    public float vadThd = 1f;

    [Tooltip("Threshold of VAD filter frequency")]
    public float vadFreqThd = 100f;

    [Tooltip("Optional indicator that changes color when speech detected")]
    [CanBeNull]
    public Image vadIndicatorImage;

    [Header("VAD Stop")]
    [Tooltip("If true microphone will stop record when no speech detected")]
    public bool vadStop;

    [Tooltip("If true whisper transcription will drop last audio where silence was detected")]
    public bool dropVadPart = true;

    [Tooltip("After how many seconds of silence microphone will stop record")]
    public float vadStopTime = 3f;

    [Header("Microphone selection (optional)")]
    [Tooltip("Optional UI dropdown with all available microphone inputs")]
    [CanBeNull]
    public TMP_Dropdown microphoneDropdown;

    [Tooltip("The label of default microphone input in dropdown")]
    public string microphoneDefaultLabel = "Default microphone";

    private int _lastVadPos;

    private AudioClip _clip;

    private float _length;

    private int _lastChunkPos;

    private int _chunksLength;

    private float? _vadStopBegin;

    private int _lastMicPos;

    private bool _madeLoopLap;

    private string _selectedMicDevice;

    public string SelectedMicDevice
    {
        get
        {
            return _selectedMicDevice;
        }
        set
        {
            if (value != null && !AvailableMicDevices.Contains(value))
            {
                throw new ArgumentException("Microphone device not found");
            }

            _selectedMicDevice = value;
        }
    }

    public int ClipSamples => _clip.samples * _clip.channels;

    public string RecordStartMicDevice { get; private set; }

    public bool IsRecording { get; private set; }

    public bool IsVoiceDetected { get; private set; }

    public IEnumerable<string> AvailableMicDevices => Microphone.devices;

    public event OnVadChangedDelegate OnVadChanged;

    public event OnChunkReadyDelegate OnChunkReady;

    public event OnRecordStopDelegate OnRecordStop;
    public event OnRecordStopDelegate OnRecordTick;

    private void Awake()
    {
        if (microphoneDropdown != null)
        {
            microphoneDropdown.options = (from text in AvailableMicDevices.Prepend(microphoneDefaultLabel)
                                          select new TMP_Dropdown.OptionData(text)).ToList();
            microphoneDropdown.value = microphoneDropdown.options.FindIndex((TMP_Dropdown.OptionData op) => op.text == microphoneDefaultLabel);
            microphoneDropdown.onValueChanged.AddListener(OnMicrophoneChanged);
        }
    }

    private void Update()
    {
        if (!IsRecording)
        {
            return;
        }

        int position = Microphone.GetPosition(RecordStartMicDevice);
        if (position < _lastMicPos)
        {
            _madeLoopLap = true;
            if (!loop)
            {
                LogUtils.Verbose($"Stopping recording, mic pos returned back to {position}");
                StopRecord();
                return;
            }

            LogUtils.Verbose("Mic made a new loop lap, continue recording.");
        }

        _lastMicPos = position;
        UpdateChunks(position);
        UpdateVad(position);
    }

    private void UpdateChunks(int micPos)
    {
        if (this.OnChunkReady != null && _chunksLength > 0)
        {
            for (int micPosDist = GetMicPosDist(_lastChunkPos, micPos); micPosDist > _chunksLength; micPosDist = GetMicPosDist(_lastChunkPos, micPos))
            {
                float[] data = new float[_chunksLength];
                _clip.GetData(data, _lastChunkPos);
                AudioChunk audioChunk = default(AudioChunk);
                audioChunk.Data = data;
                audioChunk.Frequency = _clip.frequency;
                audioChunk.Channels = _clip.channels;
                audioChunk.Length = chunksLengthSec;
                audioChunk.IsVoiceDetected = IsVoiceDetected;
                AudioChunk chunk = audioChunk;
                this.OnChunkReady(chunk);
                _lastChunkPos = (_lastChunkPos + _chunksLength) % ClipSamples;
            }
        }
    }

    private void UpdateVad(int micPos)
    {
        if (!useVad)
        {
            return;
        }

        int micBufferLength = GetMicBufferLength(micPos);
        if (micBufferLength <= 0)
        {
            return;
        }

        float num = vadUpdateRateSec * (float)_clip.frequency;
        int micPosDist = GetMicPosDist(_lastVadPos, micPos);
        if (!((float)micPosDist < num))
        {
            _lastVadPos = micBufferLength;
            float[] micBufferLast = GetMicBufferLast(micPos, vadContextSec);
            bool flag = AudioUtils.SimpleVad(micBufferLast, _clip.frequency, vadLastSec, vadThd, vadFreqThd);
            if (flag != IsVoiceDetected)
            {
                _vadStopBegin = ((!flag) ? new float?(Time.realtimeSinceStartup) : null);
                IsVoiceDetected = flag;
                this.OnVadChanged?.Invoke(flag);
            }

            if ((bool)vadIndicatorImage)
            {
                Color color = (flag ? Color.green : Color.red);
                vadIndicatorImage.color = color;
            }

            UpdateVadStop();
        }
    }

    private void UpdateVadStop()
    {
        if (vadStop && _vadStopBegin.HasValue && Time.realtimeSinceStartup - _vadStopBegin > vadStopTime)
        {
            float dropTimeSec = (dropVadPart ? vadStopTime : 0f);
            StopRecord(dropTimeSec);
        }
    }

    private void OnMicrophoneChanged(int ind)
    {
        if (!(microphoneDropdown == null))
        {
            TMP_Dropdown.OptionData optionData = microphoneDropdown.options[ind];
            SelectedMicDevice = ((optionData.text == microphoneDefaultLabel) ? null : optionData.text);
        }
    }

    public void StartRecord()
    {
        if (!IsRecording)
        {
            RecordStartMicDevice = SelectedMicDevice;
            _clip = Microphone.Start(RecordStartMicDevice, loop, maxLengthSec, frequency);
            IsRecording = true;
            _lastMicPos = 0;
            _madeLoopLap = false;
            _lastChunkPos = 0;
            _lastVadPos = 0;
            _vadStopBegin = null;
            _chunksLength = (int)((float)(_clip.frequency * _clip.channels) * chunksLengthSec);
        }
    }

    public void StopRecord(float dropTimeSec = 0f)
    {
        if (IsRecording)
        {
            Debug.Log("Stopping Recording");
            float[] micBuffer = GetMicBuffer(dropTimeSec);
            AudioChunk audioChunk = default;
            audioChunk.Data = micBuffer;
            audioChunk.Channels = _clip.channels;
            audioChunk.Frequency = _clip.frequency;
            audioChunk.IsVoiceDetected = IsVoiceDetected;
            audioChunk.Length = (float)micBuffer.Length / (float)(_clip.frequency * _clip.channels);
            AudioChunk recordedAudio = audioChunk;
            Microphone.End(RecordStartMicDevice);
            IsRecording = false;
            UnityEngine.Object.Destroy(_clip);
            LogUtils.Verbose("Stopped microphone recording. Final audio length " + $"{recordedAudio.Length} ({recordedAudio.Data.Length} samples)");
            if (IsVoiceDetected)
            {
                IsVoiceDetected = false;
                this.OnVadChanged?.Invoke(isSpeechDetected: false);
            }

            if (echo)
            {
                AudioClip audioClip = AudioClip.Create("echo", micBuffer.Length, _clip.channels, _clip.frequency, stream: false);
                audioClip.SetData(micBuffer, 0);
                PlayAudioAndDestroy.Play(audioClip, Vector3.zero);
            }

            this.OnRecordStop?.Invoke(recordedAudio);
        }
    }

    public void RealTimeRecord(float dropTimeSec = 0f)
    {
        if (IsRecording)
        {
            float[] micBuffer = GetMicBuffer(dropTimeSec);
            AudioChunk audioChunk = default(AudioChunk);
            audioChunk.Data = micBuffer;
            audioChunk.Channels = _clip.channels;
            audioChunk.Frequency = _clip.frequency;
            audioChunk.IsVoiceDetected = IsVoiceDetected;
            audioChunk.Length = (float)micBuffer.Length / (float)(_clip.frequency * _clip.channels);
            AudioChunk recordedAudio = audioChunk;
            // LogUtils.Verbose("Stopped microphone recording. Final audio length " + $"{recordedAudio.Length} ({recordedAudio.Data.Length} samples)");
            if (IsVoiceDetected)
            {
                IsVoiceDetected = false;
                this.OnVadChanged?.Invoke(isSpeechDetected: false);
            } else {
                Microphone.End(RecordStartMicDevice);
                IsRecording = false;
                UnityEngine.Object.Destroy(_clip);
                this.OnRecordTick?.Invoke(recordedAudio);
                StartRecord();
            }

            if (echo)
            {
                AudioClip audioClip = AudioClip.Create("echo", micBuffer.Length, _clip.channels, _clip.frequency, stream: false);
                audioClip.SetData(micBuffer, 0);
                PlayAudioAndDestroy.Play(audioClip, Vector3.zero);
            }

        }
    }

    private float[] GetMicBuffer(float dropTimeSec = 0f)
    {
        int position = Microphone.GetPosition(RecordStartMicDevice);
        int micBufferLength = GetMicBufferLength(position);
        if (micBufferLength == 0)
        {
            return Array.Empty<float>();
        }

        int num = (int)((float)_clip.frequency * dropTimeSec);
        micBufferLength = Math.Max(0, micBufferLength - num);
        float[] array = new float[micBufferLength];
        int offsetSamples = (_madeLoopLap ? position : 0);
        _clip.GetData(array, offsetSamples);
        return array;
    }

    private float[] GetMicBufferLast(int micPos, float lastSec)
    {
        int micBufferLength = GetMicBufferLength(micPos);
        if (micBufferLength == 0)
        {
            return Array.Empty<float>();
        }

        int val = (int)((float)_clip.frequency * lastSec);
        int num = Math.Min(val, micBufferLength);
        int num2 = micPos - num;
        if (num2 < 0)
        {
            num2 = micBufferLength + num2;
        }

        float[] array = new float[num];
        _clip.GetData(array, num2);
        return array;
    }

    private int GetMicBufferLength(int micPos)
    {
        if (micPos == 0 && !_madeLoopLap)
        {
            return 0;
        }

        return _madeLoopLap ? ClipSamples : micPos;
    }

    private int GetMicPosDist(int prevPos, int newPos)
    {
        if (newPos >= prevPos)
        {
            return newPos - prevPos;
        }

        return ClipSamples - prevPos + newPos;
    }
}
// #if false // Decompilation log
// '244' items in cache
// ------------------
// Resolve: 'netstandard, Version=2.1.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
// Found single assembly: 'netstandard, Version=2.1.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
// Load from: 'C:\Program Files\Unity\Hub\Editor\2022.3.20f1\Editor\Data\NetStandard\ref\2.1.0\netstandard.dll'
// ------------------
// Resolve: 'UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
// Found single assembly: 'UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
// Load from: 'C:\Program Files\Unity\Hub\Editor\2022.3.20f1\Editor\Data\Managed\UnityEngine\UnityEngine.CoreModule.dll'
// ------------------
// Resolve: 'UnityEngine.AudioModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
// Found single assembly: 'UnityEngine.AudioModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
// Load from: 'C:\Program Files\Unity\Hub\Editor\2022.3.20f1\Editor\Data\Managed\UnityEngine\UnityEngine.AudioModule.dll'
// ------------------
// Resolve: 'UnityEngine.UnityWebRequestModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
// Found single assembly: 'UnityEngine.UnityWebRequestModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
// Load from: 'C:\Program Files\Unity\Hub\Editor\2022.3.20f1\Editor\Data\Managed\UnityEngine\UnityEngine.UnityWebRequestModule.dll'
// ------------------
// Resolve: 'UnityEngine.UI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
// Found single assembly: 'UnityEngine.UI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
// Load from: 'C:\Users\judeb\Unity Proj\NavyTac\Library\ScriptAssemblies\UnityEngine.UI.dll'
// ------------------
// Resolve: 'UnityEngine.UIModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
// Found single assembly: 'UnityEngine.UIModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
// Load from: 'C:\Program Files\Unity\Hub\Editor\2022.3.20f1\Editor\Data\Managed\UnityEngine\UnityEngine.UIModule.dll'
// ------------------
// Resolve: 'System.Runtime.InteropServices, Version=2.1.0.0, Culture=neutral, PublicKeyToken=null'
// Found single assembly: 'System.Runtime.InteropServices, Version=4.1.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
// WARN: Version mismatch. Expected: '2.1.0.0', Got: '4.1.2.0'
// Load from: 'C:\Program Files\Unity\Hub\Editor\2022.3.20f1\Editor\Data\NetStandard\compat\2.1.0\shims\netstandard\System.Runtime.InteropServices.dll'
// ------------------
// Resolve: 'System.Runtime.CompilerServices.Unsafe, Version=2.1.0.0, Culture=neutral, PublicKeyToken=null'
// Could not find by name: 'System.Runtime.CompilerServices.Unsafe, Version=2.1.0.0, Culture=neutral, PublicKeyToken=null'
// #endif
