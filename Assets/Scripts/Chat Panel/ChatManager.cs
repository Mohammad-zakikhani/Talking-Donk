using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Michsky.MUIP;

public class ChatManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField inputField;
    public ButtonManager sendButton;
    public ScrollRect scrollRect;
    public Transform chatContent;

    [Header("Prefabs")]
    public ChatMessageUI userMessagePrefab;
    public ChatMessageUI aiMessagePrefab;

    [Header("User Info")]
    public Sprite userProfilePic;
    public string userName = "You";

    [Header("AI Info")]
    public Sprite aiProfilePic;
    public string aiName = "AI";

    private Queue<ChatMessageUI> userPool = new Queue<ChatMessageUI>();
    private Queue<ChatMessageUI> aiPool = new Queue<ChatMessageUI>();
    private int poolSize = 20;

    void Start()
    {
        sendButton.onClick.AddListener(OnUserSend);
        PrewarmPool(userMessagePrefab, userPool);
        PrewarmPool(aiMessagePrefab, aiPool);
    }

    void PrewarmPool(ChatMessageUI prefab, Queue<ChatMessageUI> pool)
    {
        for (int i = 0; i < poolSize; i++)
        {
            ChatMessageUI msg = Instantiate(prefab, chatContent);
            msg.gameObject.SetActive(false);
            pool.Enqueue(msg);
        }
    }

    void OnUserSend()
    {
        string msg = inputField.text;
        if (string.IsNullOrWhiteSpace(msg)) return;

        AddUserMessage(msg);
        inputField.text = "";

        Invoke(nameof(SimulateAIReply), 1f);
    }

    public void AddUserMessage(string msg)
    {
        ChatMessageUI chatMsg = GetFromPool(userPool, userMessagePrefab);
        chatMsg.transform.SetParent(chatContent, false);
        chatMsg.transform.SetAsLastSibling(); // 🔥 important
        chatMsg.gameObject.SetActive(true);
        chatMsg.SetMessage(userProfilePic, userName, msg);
        ScrollToBottom();
    }


    public void AddAIMessage(string msg)
    {
        ChatMessageUI chatMsg = GetFromPool(aiPool, aiMessagePrefab);
        chatMsg.transform.SetParent(chatContent, false);
        chatMsg.transform.SetAsLastSibling(); // 🔥 keeps correct order
        chatMsg.gameObject.SetActive(true);
        chatMsg.SetMessage(aiProfilePic, aiName, msg);
        ScrollToBottom();
    }


    ChatMessageUI GetFromPool(Queue<ChatMessageUI> pool, ChatMessageUI prefab)
    {
        if (pool.Count > 0)
            return pool.Dequeue();

        return Instantiate(prefab, chatContent);
    }

    void ScrollToBottom()
    {
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
        Canvas.ForceUpdateCanvases();
    }

    void SimulateAIReply()
    {
        AddAIMessage("این جواب خودکار از طرف AI هست 🤖");
    }
}
