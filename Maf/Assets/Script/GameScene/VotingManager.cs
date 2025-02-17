    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    using Photon.Pun;
    using TMPro;
    using Photon.Realtime;

    public class VotingManager : MonoBehaviourPun
    {
        [Header("Ссылки на UI и менеджеры")]
        [Tooltip("Панель, которая появляется во время голосования")]
        public GameObject votePanel;
        
        [Tooltip("Префаб элемента голосования (VotingEntry)")]
        public GameObject votingEntryPrefab;
        
        [Tooltip("Ссылка на менеджер фаз")]
        public PhaseManager phaseManager;
        
        [Tooltip("Ссылка на компонент чата для отправки сообщений")]
        public GameChat gameChat;

        [Tooltip("Ссылка на менеджер игры")]
        public GameManager gameManager;

        // Словарь для хранения голосов: ключ – ник игрока, значение – число голосов
        private Dictionary<string, int> votes = new Dictionary<string, int>();
        // Словарь для хранения ссылок на созданные элементы голосования
        private Dictionary<string, VotingEntry> votingEntries = new Dictionary<string, VotingEntry>();

        
        // Метод, который вызывается для отображения панели голосования.
        // Он очищает панель, создаёт UI-элементы для каждого игрока и инициализирует словари.
      public void ShowVotingPanel()
{
    // Если это ночное голосование, проверяем роль игрока
    if (phaseManager.CurrentPhase == GamePhase.NightVoting)
    {
        object role;
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("role", out role))
        {
            // Если роль не mafia, не показываем панель
            if (role.ToString().ToLower() != "mafia")
            {
                Debug.Log("[VotingManager] Ночная фаза: мирный игрок не видит панель голосования.");
                votePanel.SetActive(false);
                return;
            }
        }
        else
        {
            Debug.Log("[VotingManager] Роль не задана, предполагается мирный — скрываем панель голосования.");
            votePanel.SetActive(false);
            return;
        }
    }

    // Если день или игрок mafia, показываем панель
    votePanel.SetActive(true);

    // Очистка предыдущих элементов
    foreach (Transform child in votePanel.transform)
    {
        Destroy(child.gameObject);
    }
    votingEntries.Clear();
    votes.Clear();

    // Создаем элемент голосования для каждого игрока
  foreach (Player player in PhotonNetwork.PlayerList)
{
    if (player.NickName == PhotonNetwork.LocalPlayer.NickName)
        continue; // Пропускаем создание элемента для самого себя

    CreateVotingEntry(player.NickName);
}   
}

        // Метод для скрытия панели голосования.
        public void HideVotingPanel()
        {
            votePanel.SetActive(false);
        }
    
        // Создает UI-элемент голосования для игрока с именем playerName.
        private void CreateVotingEntry(string playerName)
        {
            GameObject entryObj = Instantiate(votingEntryPrefab, votePanel.transform);
            VotingEntry entry = entryObj.GetComponent<VotingEntry>();
            if (entry != null)
            {
                // Передаём имя игрока и callback на нажатие кнопки голосования
                entry.Setup(playerName, OnVoteButtonClicked);
                votingEntries[playerName] = entry;
            }
            // Инициализируем голос как 0
            votes[playerName] = 0;
        }

        
        // Метод, вызываемый при нажатии кнопки голосования.
     private void OnVoteButtonClicked(string votedPlayer)
{
    // Если это ночное голосование, проверяем роль голосующего
    if (phaseManager.CurrentPhase == GamePhase.NightVoting)
    {
        object role;
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("role", out role))
        {
            if (role.ToString().ToLower() != "mafia")
            {
                Debug.Log("[VotingManager] Мирный игрок не может голосовать ночью.");
                return;
            }
        }
        else
        {
            Debug.Log("[VotingManager] Роль не задана, предполагается мирный — голосование невозможно ночью.");
            return;
        }
    }

    // Дополнительная проверка фазы (если нужно)
    if (phaseManager.CurrentPhase != GamePhase.DayVoting && phaseManager.CurrentPhase != GamePhase.NightVoting)
    {
        Debug.Log("Голосование сейчас недоступно!");
        return;
    }

    photonView.RPC("RPC_RegisterVote", RpcTarget.All, PhotonNetwork.NickName, votedPlayer);
}
        // RPC-метод для регистрации голоса.
        // Вызывается на всех клиентах.
        [PunRPC]
        private void RPC_RegisterVote(string votingPlayer, string votedPlayer)
        {
            if (votes.ContainsKey(votedPlayer))
            {
                votes[votedPlayer]++;
                UpdateVotingUI(votedPlayer);
            }
            else
            {
                Debug.LogWarning("Попытка голосовать за неизвестного игрока: " + votedPlayer);
            }

            // Отправляем сообщение в чат
            gameChat.photonView.RPC("RPC_AddMessage", RpcTarget.All, $"{votingPlayer} проголосовал за {votedPlayer}");
        }


        // Обновляет UI-элемент для игрока с новым количеством голосов.
        private void UpdateVotingUI(string playerName)
        {
            if (votingEntries.ContainsKey(playerName))
            {
                votingEntries[playerName].SetVoteCount(votes[playerName]);
            }
        }

    
        // Метод, который вызывается при завершении фазы голосования.
        // Здесь определяется игрок с максимальным числом голосов, выводится сообщение об исключении и панель скрывается.
       public void EndVoting()
{
    // Определяем игрока с максимальным числом голосов
    int maxVotes = -1;
    string eliminatedPlayer = "";
    foreach (KeyValuePair<string, int> pair in votes)
    {
        if (pair.Value > maxVotes)
        {
            maxVotes = pair.Value;
            eliminatedPlayer = pair.Key;
        }
    }

    if (maxVotes <= 0)
    {
        gameChat.photonView.RPC("RPC_AddMessage", RpcTarget.All, "Голосование завершилось без голосов.");
    }
    else
    {
        gameChat.photonView.RPC("RPC_AddMessage", RpcTarget.All, $"{eliminatedPlayer} покинул наш мир");
        
        // Вызов RPC_KillPlayer в GameManager для пометки игрока как убитого
       GameManager gm = UnityEngine.Object.FindAnyObjectByType<GameManager>();
        if (gm != null)
        {
          gm.photonView.RPC("RPC_KillPlayer", RpcTarget.All, eliminatedPlayer);
        }
    }

    HideVotingPanel();
}

}
