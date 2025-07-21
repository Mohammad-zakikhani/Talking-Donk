// OpenAITTS_LocalWhisper.cs

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Whisper.Utils;  // whisper.unity namespace
using System.Threading.Tasks;
using Whisper;

public class OpenAITTS : MonoBehaviour
{
    [Header("OpenAI Settings")]
    public string apiKey = "sk-..."; // Your OpenAI API key
    public string chatModel = "gpt-3.5-turbo";
    public string voice = "echo"; // Change if you find better Persian voice
    public string ttsModel = "tts-1";

    [TextArea(10, 20)]
    public string systemPrompt = @"تو نقش یک الاغ باحال و پرحرف به سبک خر شرک هستی.
فقط به زبان فارسی جواب بده و در تمام جملاتت حتماً از اعراب کامل و دقیق استفاده کن (یعنی حتماً حروف َ ِ ُ و آ رو به درستی بنویس).
این اعراب باید توی همه کلمات باشن تا تلفظ توسط سیستم گفتار طبیعی و دقیق باشه.
اگر کلمه‌ای به اعراب نیاز داره و معمولاً در نوشتار حذف می‌شه، تو حتما اعراب رو اضافه کن.
همیشه اعراب‌گذاری کن حتی اگه باعث می‌شه جمله رسمی‌تر یا عجیب‌تر به نظر بیاد.
سعی کن جمله‌ها با انرژی، شوخ‌طبعی و کمی بی‌منطقی مثل خر شرک باشن.
خیلی خودمونی و صمیمی باش و با لحن خنده‌دار جواب بده.";

    [Header("Whisper.Unity Components")]
    public WhisperManager whisper;               // Assign whisper.unity WhisperManager in Inspector
    public MicrophoneRecord microphoneRecord;    // Assign MicrophoneRecord in Inspector (optional, can keep your own recording)

    private AudioClip recordedClip;
    private const int sampleRate = 44100;
    private float silenceThreshold = 0.01f;
    private float silenceDuration = 1.5f;

    private string responseAudioPath => Path.Combine(Application.persistentDataPath, "response.mp3");
    private Dictionary<string, string> audioCache = new Dictionary<string, string>();

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(RunConversation());
        }
    }

    IEnumerator RunConversation()
    {
        yield return StartCoroutine(RecordWithAutoStop());

        // Local whisper transcription instead of online API
        var task = TranscribeLocal(recordedClip);
        while (!task.IsCompleted) yield return null;

        string transcript = task.Result?.Trim();
        if (string.IsNullOrEmpty(transcript))
        {
            Debug.LogWarning("❌ Transcription empty or failed.");
            yield break;
        }

        Debug.Log("🗣 کاربر گفت: " + transcript);

        if (audioCache.TryGetValue(transcript, out string cachedPath))
        {
            yield return StartCoroutine(PlayMP3(cachedPath));
            yield break;
        }

        // Continue with ChatGPT and TTS as before
        yield return StartCoroutine(SendToChatGPT(transcript, (chatReply) =>
        {
            Debug.Log("🤖 پاسخ: " + chatReply);
            StartCoroutine(SendToTTS(chatReply, transcript));
        }));
    }

    // Your existing RecordWithAutoStop() method remains unchanged:
    IEnumerator RecordWithAutoStop()
    {
        Debug.Log("🎙️ در حال ضبط...");
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
        Debug.Log("✅ ضبط تمام شد.");
    }

    // Local transcription using whisper.unity:
    private async Task<string> TranscribeLocal(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogError("No audio clip recorded!");
            return null;
        }

        // Get float samples from AudioClip
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        // Call whisper.unity's async transcription method
        var result = await whisper.GetTextAsync(samples, clip.frequency, clip.channels);
        if (result == null)
        {
            Debug.LogError("Whisper transcription failed!");
            return null;
        }

        if (!string.IsNullOrEmpty(result.Language))
            Debug.Log($"Detected language: {result.Language}");

        return result.Result;
    }

    // Your existing ChatGPT coroutine, unchanged:
    IEnumerator SendToChatGPT(string userText, Action<string> onReply)
    {
        var messages = new List<ChatMessage>()
    {
        new ChatMessage("system", systemPrompt),
        new ChatMessage("user", userText)
    };

        var chatRequest = new ChatRequest(chatModel, messages);

        string jsonPayload = JsonUtility.ToJson(chatRequest);

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
        var request = new UnityWebRequest("https://api.openai.com/v1/chat/completions", "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("❌ ChatGPT error: " + request.downloadHandler.text);
        }
        else
        {
            string json = request.downloadHandler.text;
            var result = JsonUtility.FromJson<ChatCompletionResponse>(json);
            onReply(result.choices[0].message.content);
        }
    }


    // Your existing TTS coroutine, unchanged:
    IEnumerator SendToTTS(string text, string cacheKey)
    {
        string json = $"{{\"model\":\"{ttsModel}\",\"input\":\"{EscapeJson(text)}\",\"voice\":\"{voice}\"}}";
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        var request = new UnityWebRequest("https://api.openai.com/v1/audio/speech", "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("❌ TTS error: " + request.downloadHandler.text);
        }
        else
        {
            File.WriteAllBytes(responseAudioPath, request.downloadHandler.data);
            audioCache[cacheKey] = responseAudioPath;
            StartCoroutine(PlayMP3(responseAudioPath));
        }
    }

    IEnumerator PlayMP3(string path)
    {
        var www = new WWW("file://" + path);
        yield return www;

        var audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        audioSource.clip = www.GetAudioClip(false, false, AudioType.MPEG);
        audioSource.Play();
    }

    string EscapeJson(string text) => text.Replace("\"", "\\\"");

    void SaveWav(string path, AudioClip clip)
    {
        var bytes = WavUtility.FromAudioClip(clip);
        File.WriteAllBytes(path, bytes);
        Debug.Log("📁 WAV saved to: " + path);
    }
}

[Serializable]
public class ChatCompletionResponse
{
    public Choice[] choices;
}

[Serializable]
public class Choice
{
    public Message message;
}

[Serializable]
public class Message
{
    public string role;
    public string content;
}

[Serializable]
public class ChatMessage
{
    public string role;
    public string content;

    public ChatMessage(string role, string content)
    {
        this.role = role;
        this.content = content;
    }
}

[Serializable]
public class ChatRequest
{
    public string model;
    public List<ChatMessage> messages;

    public ChatRequest(string model, List<ChatMessage> messages)
    {
        this.model = model;
        this.messages = messages;
    }
}
