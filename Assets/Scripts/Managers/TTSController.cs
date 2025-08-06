using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class TTSController : MonoBehaviour
{
    [Header("TTS Settings")]
    public string apiKey = "sk-...";
    public string ttsModel = "tts-1";
    public string voice = "echo";

    private string responseAudioPath => Path.Combine(Application.persistentDataPath, "response.mp3");
    private Dictionary<string, string> audioCache = new Dictionary<string, string>();

    public void Speak(string text, string cacheKey)
    {
        StartCoroutine(SendToTTS(text, cacheKey));
    }

    IEnumerator SendToTTS(string text, string cacheKey)
    {
        if (audioCache.TryGetValue(cacheKey, out string cachedPath))
        {
            yield return PlayMP3(cachedPath);
            yield break;
        }

        string json = $"{{\"model\":\"{ttsModel}\",\"input\":\"{EscapeJson(text)}\",\"voice\":\"{voice}\"}}";
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        UnityWebRequest request = new UnityWebRequest("https://api.openai.com/v1/audio/speech", "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("TTS Error: " + request.downloadHandler.text);
        }
        else
        {
            File.WriteAllBytes(responseAudioPath, request.downloadHandler.data);
            audioCache[cacheKey] = responseAudioPath;
            yield return PlayMP3(responseAudioPath);
        }
    }

    IEnumerator PlayMP3(string path)
    {
        var www = new WWW("file://" + path);
        yield return www;

        var clip = www.GetAudioClip(false, false, AudioType.MPEG);
        RoomManager.Instance.donkeyController.PlayAudio(clip);
    }


    string EscapeJson(string text) => text.Replace("\"", "\\\"");
}
