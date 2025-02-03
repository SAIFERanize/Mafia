using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class LobbyChat : MonoBehaviourPun
{
    public TMP_Text chatHistoryText; // Текст истории чата
    public TMP_InputField chatInputField; // Поле ввода
    public GameObject sendButton; // Кнопка отправки
    public GameObject exitButton; // Кнопка выхода
    public ScrollRect scrollRect; // Прокрутка чата
    public VerticalLayoutGroup chatLayoutGroup; // Layout для корректного роста вниз

    private void Start()
    {
        // Проверка и назначение обработчика кнопки отправки
        if (sendButton != null && sendButton.GetComponent<Button>() != null)
        {
            sendButton.GetComponent<Button>().onClick.AddListener(SendMessage);
        }
        else
        {
            Debug.LogError("SendButton не назначен в инспекторе!");
        }

        // Проверка и назначение обработчика кнопки выхода
        if (exitButton != null && exitButton.GetComponent<Button>() != null)
        {
            exitButton.GetComponent<Button>().onClick.AddListener(ExitLobby);
        }
        else
        {
            Debug.LogError("ExitButton не назначен в инспекторе!");
        }
    }

    public void SendMessage()
    {
        if (!string.IsNullOrWhiteSpace(chatInputField.text))
        {
            photonView.RPC("RPC_AddMessage", RpcTarget.All, PhotonNetwork.NickName + ": " + chatInputField.text);
            chatInputField.text = ""; // Очищаем поле после отправки
        }
    }

    [PunRPC]
    public void RPC_AddMessage(string message)
    {
        chatHistoryText.text += message + "\n";

        //  Форсим обновление LayoutGroup, чтобы текст чата корректно расширялся вниз
        LayoutRebuilder.ForceRebuildLayoutImmediate(chatHistoryText.rectTransform);
        
        //  Форсим обновление Canvas перед изменением позиции скролла
        Canvas.ForceUpdateCanvases();
        
        //  Ограничиваем прокрутку, чтобы не уходить слишком далеко вверх или вниз
        LimitScrollPosition();
    }

    private void LimitScrollPosition()
    {
        float minScroll = 0f;   // Нижняя граница (не уходит слишком вниз)
        float maxScroll = 1f;   // Верхняя граница (не уходит слишком вверх)

        // Ограничиваем значение прокрутки в заданных пределах
        scrollRect.verticalNormalizedPosition = Mathf.Clamp(scrollRect.verticalNormalizedPosition, minScroll, maxScroll);
    }

    public void ExitLobby()
    {
        PhotonNetwork.LeaveRoom();
        PhotonNetwork.LoadLevel("MainMenu");
    }
}
