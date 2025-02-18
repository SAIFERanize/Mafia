using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

public class GameChat : MonoBehaviourPun
{
    [Header("UI Elements")]
    public TMP_Text chatHistoryText;
    public TMP_InputField chatInputField;
    public Button sendButton;
    public ScrollRect scrollRect;
    public VerticalLayoutGroup chatLayoutGroup;

    [Header("References")]
    public PhaseManager phaseManager; // Ссылка на PhaseManager для проверки текущей фазы игры

    private void Start()
    {
        sendButton.onClick.AddListener(SendMessage);
    }

    private void Update()
    {
        if (phaseManager != null)
        {
            bool canChat = IsChatAllowed();
            chatInputField.interactable = canChat;
            sendButton.interactable = canChat;
        }
    }

    /// <summary>
    /// Проверяет, разрешён ли чат в текущей фазе игры.
    /// </summary>
    /// <returns>True, если чат разрешён, иначе False.</returns>
    private bool IsChatAllowed()
    {
        if (phaseManager == null)
            return false;

        // Если игрок мёртв, чат недоступен
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("isDead", out object isDead) && (bool)isDead)
            return false;

        // В дневном обсуждении чат доступен всем
        if (phaseManager.CurrentPhase == GamePhase.DayDiscussion)
            return true;

        // В ночном обсуждении чат доступен только мафии
        if (phaseManager.CurrentPhase == GamePhase.NightDiscussion)
        {
            if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("role", out object role))
                return role.ToString().ToLower() == "mafia";
            return false;
        }

        return false;
    }

    /// <summary>
    /// Отправляет сообщение в чат.
    /// </summary>
    public void SendMessage()
    {
        if (!IsChatAllowed())
        {
            Debug.Log("Чат недоступен в текущей фазе!");
            return;
        }

        if (!string.IsNullOrWhiteSpace(chatInputField.text))
        {
            // Формируем сообщение
            string message = PhotonNetwork.NickName + ": " + chatInputField.text;

            // Отправляем сообщение всем игрокам
            photonView.RPC("RPC_AddMessage", RpcTarget.All, message, " ");

            // Очищаем поле ввода
            chatInputField.text = "";
        }
    }

    /// <summary>
    /// RPC-метод для добавления сообщения в чат.
    /// </summary>
    /// <param name="message">Текст сообщения.</param>
    /// <param name="senderRole">Роль отправителя.</param>
    /// <param name="info">Информация о сообщении (автоматически передаётся Photon).</param>
    [PunRPC]
    public void RPC_AddMessage(string message, string senderRole, PhotonMessageInfo info)
    {
        // Если роль отправителя не передана, пытаемся получить её из CustomProperties
        if (string.IsNullOrEmpty(senderRole))
        {
            senderRole = "civilian";
            if (info.Sender.CustomProperties.TryGetValue("role", out object roleObj))
                senderRole = roleObj.ToString().ToLower();
        }

        // Если ночь, скрываем сообщения мафии от мирных
        if (phaseManager.CurrentPhase == GamePhase.NightDiscussion 
            || phaseManager.CurrentPhase == GamePhase.NightVoting)
        {
            // Роль локального игрока (по умолчанию "civilian")
            string localRole = "civilian";
            if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("role", out object localRoleObj))
                localRole = localRoleObj.ToString().ToLower();

            // Если сообщение – о голосовании
            if (message.Contains("проголосовал за"))
            {
                // Мафия → скрываем от мирных
                if (senderRole == "mafia" && localRole != "mafia")
                {
                    Debug.Log($"[Mafia Vote Hidden] {message}");
                    return;
                }
            }
            // Иначе просто ночной чат мафии
            else
            {
                if (senderRole == "mafia" && localRole != "mafia")
                {
                    Debug.Log($"[Mafia Message Hidden] {message}");
                    return;
                }
            }
        }

        // Если сообщение уже есть в истории, не добавляем его повторно
        if (chatHistoryText.text.Contains(message))
            return;

        // Добавляем сообщение в чат
        chatHistoryText.text += message + "\n";

        // Обновляем UI чата
        LayoutRebuilder.ForceRebuildLayoutImmediate(chatHistoryText.rectTransform);
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}