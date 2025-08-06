using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System;

public class Brain : MonoBehaviour
{
    [TextArea(10, 20)]
    public string systemPrompt = "You're a donkey like from Shrek. Be wild and funny in Persian.";

    public string apiKey = "sk-...";
    public string chatModel = "gpt-3.5-turbo";

    public void ProcessUserInput(string userInput)
    {
        StartCoroutine(SendToChatGPT(userInput, (aiReply) =>
        {
            RoomManager.Instance.chatManager.AddAIMessage(aiReply);
            RoomManager.Instance.ttsController.Speak(aiReply, userInput);
        }));
    }

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
            Debug.LogError("GPT error: " + request.downloadHandler.text);
        }
        else
        {
            var result = JsonUtility.FromJson<ChatCompletionResponse>(request.downloadHandler.text);
            onReply?.Invoke(result.choices[0].message.content);
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
}
