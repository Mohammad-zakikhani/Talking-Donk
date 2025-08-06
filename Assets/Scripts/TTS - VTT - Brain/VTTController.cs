using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using Whisper;
using Whisper.Utils;

public class VTTController : MonoBehaviour
{
    public WhisperManager whisper;
    public MicrophoneRecord micRecord;
    private AudioClip recordedClip;

    [Header("Settings")]
    private const int sampleRate = 44100;
    private float silenceThreshold = 0.01f;
    private float silenceDuration = 1.5f;

    public void StartTranscription()
    {
        StartCoroutine(RecordAndTranscribe());
    }

    IEnumerator RecordAndTranscribe()
    {
        string micDevice = Microphone.devices[0];
        recordedClip = Microphone.Start(micDevice, true, 10, sampleRate);

        float lastSoundTime = Time.time;
        while (true)
        {
            if (Microphone.GetPosition(micDevice) < 1024)
            {
                yield return null;
                continue;
            }

            float[] samples = new float[1024];
            int pos = Microphone.GetPosition(micDevice) - samples.Length;
            if (pos < 0) pos = 0;
            recordedClip.GetData(samples, pos);

            float maxVol = 0f;
            foreach (var sample in samples)
                if (Mathf.Abs(sample) > maxVol)
                    maxVol = Mathf.Abs(sample);

            if (maxVol > silenceThreshold)
                lastSoundTime = Time.time;

            if (Time.time - lastSoundTime > silenceDuration)
                break;

            yield return null;
        }

        Microphone.End(micDevice);

        var task = Transcribe(recordedClip);
        while (!task.IsCompleted) yield return null;

        string transcript = task.Result?.Trim();
        if (string.IsNullOrEmpty(transcript))
        {
            Debug.LogWarning("Empty transcript.");
            yield break;
        }

        RoomManager.Instance.chatManager.AddUserMessage(transcript);
        RoomManager.Instance.brain.ProcessUserInput(transcript);
    }

    async Task<string> Transcribe(AudioClip clip)
    {
        if (clip == null) return null;

        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        var result = await whisper.GetTextAsync(samples, clip.frequency, clip.channels);
        return result?.Result;
    }
}
