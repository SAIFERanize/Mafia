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

        // Ссылка на PhaseManager, чтобы узнать текущую фазу игры
        public PhaseManager phaseManager;

        private void Start()
        {
            sendButton.onClick.AddListener(SendMessage);
        }

        private void Update()
        {
            // Обновляем возможность ввода в чат в зависимости от фазы
            if (phaseManager != null)
            {
                bool canChat = IsChatAllowed();
                chatInputField.interactable = canChat;
                sendButton.interactable = canChat;
            }
        }

        // Метод возвращает true, если чат разрешён (только в дневном обсуждении)
  private bool IsChatAllowed()
{
    if (phaseManager == null)
        return false;

    // Игрок, помеченный как "мертвый", не может писать
    if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("isDead", out object isDead) && (bool)isDead)
        return false;

    // Если дневное обсуждение — разрешаем всем
    if (phaseManager.CurrentPhase == GamePhase.DayDiscussion)
        return true;

    // Ночное обсуждение: разрешаем только мафии
    if (phaseManager.CurrentPhase == GamePhase.NightDiscussion)
    {
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("role", out object role))
        {
            return role.ToString().ToLower() == "mafia";
        }
        return false;
    }

    return false;
}
    public void SendMessage()
    {
        if (!IsChatAllowed())
        {
            Debug.Log("Чат недоступен в текущей фазе!");
            return;
        }

        if (!string.IsNullOrWhiteSpace(chatInputField.text))
        {
            string message = PhotonNetwork.NickName + ": " + chatInputField.text;
            photonView.RPC("RPC_AddMessage", RpcTarget.All, message);
            chatInputField.text = "";
        }
    }

      [PunRPC]
    public void RPC_AddMessage(string message, PhotonMessageInfo info)
    {
       // Если сообщение уже есть в истории, не добавляем его повторно
       if (chatHistoryText.text.Contains(message))
          return;

      chatHistoryText.text += message + "\n";
      LayoutRebuilder.ForceRebuildLayoutImmediate(chatHistoryText.rectTransform);
      Canvas.ForceUpdateCanvases();
      scrollRect.verticalNormalizedPosition = 0f; // Автопрокрутка вниз
    }
}
