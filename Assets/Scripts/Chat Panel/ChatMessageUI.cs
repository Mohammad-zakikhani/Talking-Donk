using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChatMessageUI : MonoBehaviour
{
    public Image profileImage;
    public TMP_Text nameText;
    public TMP_Text messageText;

    public void SetMessage(Sprite profile, string displayName, string message)
    {
        profileImage.sprite = profile;
        nameText.text = displayName;
        messageText.text = message;
    }
}
