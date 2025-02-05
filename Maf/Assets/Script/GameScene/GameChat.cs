using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

public class GameChat : MonoBehaviourPun
{
    public TMP_Text chatHistoryText; 
    public TMP_InputField chatInputField;
    public Button sendButton;
    public ScrollRect scrollRect;
    public VerticalLayoutGroup chatLayoutGroup;

    private void Start()
    {
        sendButton.onClick.AddListener(SendMessage);
    }

    public void SendMessage()
    {
        if (!string.IsNullOrWhiteSpace(chatInputField.text))
        {
            photonView.RPC("RPC_AddMessage", RpcTarget.All, PhotonNetwork.NickName + ": " + chatInputField.text);
            chatInputField.text = "";
        }
    }

    [PunRPC]
    public void RPC_AddMessage(string message)
    {
        chatHistoryText.text += message + "\n";
        LayoutRebuilder.ForceRebuildLayoutImmediate(chatHistoryText.rectTransform);
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f; // Автопрокрутка вниз
    }
}

