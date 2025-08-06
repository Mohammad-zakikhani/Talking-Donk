using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Michsky.MUIP;

public class ChatManager : MonoBehaviour
{
    public static ChatManager Instance { get; private set; }

    [Header("UI References")]
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
    private const int poolSize = 20;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        sendButton.onClick.AddListener(OnUserSend);
        PrewarmPool(userMessagePrefab, userPool);
        PrewarmPool(aiMessagePrefab, aiPool);
    }

    void OnUserSend()
    {
        string msg = inputField.text;
        if (string.IsNullOrWhiteSpace(msg)) return;

        AddUserMessage(msg);
        inputField.text = "";

        // Send message to Brain through RoomManager
        RoomManager.Instance.brain.ProcessUserInput(msg);
    }

    public void AddUserMessage(string msg)
    {
        ChatMessageUI chatMsg = GetFromPool(userPool, userMessagePrefab);
        SetupMessage(chatMsg, userProfilePic, userName, msg);
    }

    public void AddAIMessage(string msg)
    {
        ChatMessageUI chatMsg = GetFromPool(aiPool, aiMessagePrefab);
        SetupMessage(chatMsg, aiProfilePic, aiName, msg);
    }

    void SetupMessage(ChatMessageUI chatMsg, Sprite avatar, string senderName, string message)
    {
        chatMsg.transform.SetParent(chatContent, false);
        chatMsg.transform.SetAsLastSibling();
        chatMsg.gameObject.SetActive(true);
        chatMsg.SetMessage(avatar, senderName, message);
        ScrollToBottom();
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
}
