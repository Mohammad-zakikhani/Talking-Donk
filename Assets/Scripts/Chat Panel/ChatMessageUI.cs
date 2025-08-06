using UnityEngine;
using UnityEngine.UI;
using RTLTMPro;
using TMPro;

public class ChatMessageUI : MonoBehaviour
{
    public Image profileImage;
    public TextMeshProUGUI nameText;
    public RTLTextMeshPro messageText;

    public void SetMessage(Sprite profile, string displayName, string message)
    {
        profileImage.sprite = profile;
        nameText.text = displayName;
        messageText.isRightToLeftText = true;
        messageText.text = ( message);
    }
}
