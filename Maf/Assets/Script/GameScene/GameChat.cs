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

            // Отправляем сообщение всем игрокам, но фильтруем его в зависимости от фазы и роли
            if (phaseManager.CurrentPhase == GamePhase.NightDiscussion)
            {
                // Если мафия, то фильтруем только для мафии
                if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("role", out object role) &&
                    role.ToString().ToLower() == "mafia")
                {
                    photonView.RPC("RPC_AddMafiaMessage", RpcTarget.All, PhotonNetwork.NickName, message);
                }
            }
            else
            {
                // Если не ночь, просто отправляем в общий чат
                photonView.RPC("RPC_AddMessage", RpcTarget.All, message, " ");
            }

            // Очищаем поле ввода
            chatInputField.text = "";
        }
    }

    [PunRPC]
    public void RPC_AddMafiaMessage(string sender, string message, PhotonMessageInfo info)
    {
        // Если локальный игрок не мафия, то не видит сообщение
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("role", out object role) && role.ToString().ToLower() != "mafia")
        {
            return;
        }

        // Мафия видит сообщение
        Debug.Log($"[Mafia Chat] {sender}: {message}");
        chatHistoryText.text += $"{sender}: {message}\n";
    }

    [PunRPC]
    public void RPC_AddMessage(string message, string senderRole, PhotonMessageInfo info)
    {
        // Если текущая фаза — ночь (обсуждение или голосование)
        if (phaseManager.CurrentPhase == GamePhase.NightDiscussion ||
            phaseManager.CurrentPhase == GamePhase.NightVoting)
        {
            // Получаем роль локального игрока (по умолчанию "civilian")
            string localRole = "civilian";
            if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("role", out object localRoleObj))
                localRole = localRoleObj.ToString().ToLower();

            // Если отправитель — мафия, а локальный игрок не мафия, скрываем сообщение
            if (senderRole == "mafia" && localRole != "mafia")
            {
                Debug.Log($"[Mafia Message Hidden] {message}");
                return;
            }
        }

        // Если сообщение уже содержится в истории, не добавляем его повторно
        if (chatHistoryText.text.Contains(message))
            return;

        // Добавляем сообщение в историю чата
        chatHistoryText.text += message + "\n";

        // Обновляем UI чата
        LayoutRebuilder.ForceRebuildLayoutImmediate(chatHistoryText.rectTransform);
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}
