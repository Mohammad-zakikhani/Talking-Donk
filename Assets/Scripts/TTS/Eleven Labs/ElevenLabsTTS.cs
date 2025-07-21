using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using Newtonsoft.Json;

public class ElevenLabsTTS : MonoBehaviour
{
    public string apiKey = "YOUR_API_KEY";
    public string voiceId = "58RjhD2A0x7OvZbxRPNJ";
    public AudioSource audioSource;

    public void Speak(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            Debug.LogWarning("Empty text");
            return;
        }
        StartCoroutine(SendTTSRequest(text));
    }

    IEnumerator SendTTSRequest(string inputText)
    {
        string url = $"https://api.elevenlabs.io/v1/text-to-speech/{voiceId}";

        var requestBody = new
        {
            text = inputText,
            voice_settings = new
            {
                stability = 0.75f,
                similarity_boost = 0.75f
            }
        };

        string json = JsonConvert.SerializeObject(requestBody);
        Debug.Log("Sending JSON: " + json);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("xi-api-key", apiKey);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                byte[] audioData = request.downloadHandler.data;
                var audioClip = WavUtility.ToAudioClip(audioData, 0, "DonkeyTTS");
                audioSource.clip = audioClip;
                audioSource.Play();
            }
            else
            {
                Debug.LogError("TTS request failed: " + request.downloadHandler.text);
            }
        }
    }
}
