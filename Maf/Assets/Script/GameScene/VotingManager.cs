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
        private bool hasVoted = false;

        // Словарь для хранения голосов: ключ – ник игрока, значение – число голосов
        private Dictionary<string, int> votes = new Dictionary<string, int>();
        // Словарь для хранения ссылок на созданные элементы голосования
        private Dictionary<string, VotingEntry> votingEntries = new Dictionary<string, VotingEntry>();

        
        // Метод, который вызывается для отображения панели голосования.
        // Он очищает панель, создаёт UI-элементы для каждого игрока и инициализирует словари.
      public void ShowVotingPanel()
{   
        hasVoted = false;
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
      // Очищаем предыдущие элементы
    foreach (Transform child in votePanel.transform)
    {
        Destroy(child.gameObject);
    }
    votingEntries.Clear();
    votes.Clear();

    foreach (Player player in PhotonNetwork.PlayerList) 
    {
        if (player.NickName == PhotonNetwork.LocalPlayer.NickName)
            continue; // Пропускаем самого себя

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
        bool isDead = false;
        // Ищем игрока по имени, чтобы узнать его статус
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.NickName == playerName)
            {
                isDead = player.CustomProperties.ContainsKey("isDead") && (bool)player.CustomProperties["isDead"];
                break;
            }
        }
        // Передаём флаг isDead в Setup
        entry.Setup(playerName, isDead, OnVoteButtonClicked);
        votingEntries[playerName] = entry;
    }
    votes[playerName] = 0;
}

        
        // Метод, вызываемый при нажатии кнопки голосования.
private void OnVoteButtonClicked(string votedPlayer)
{
    // Проверяем, если игрок уже голосовал, то больше голосовать нельзя
    if (hasVoted)
    {
        Debug.Log("Вы уже проголосовали!");
        return;
    }

    // Если игрок мёртв, не даём голосовать
    if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("isDead", out object isDeadObj) && (bool)isDeadObj)
    {
        Debug.Log("[VotingManager] Мёртвые игроки не могут голосовать.");
        return;
    }

    // Если это ночное голосование – разрешено голосовать только мафии
    if (phaseManager.CurrentPhase == GamePhase.NightVoting)
    {
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("role", out object role))
        {
            if (role.ToString().ToLower() != "mafia")
            {
                Debug.Log("[VotingManager] Мирный игрок не может голосовать ночью.");
                return;
            }
        }
        else
        {
            Debug.Log("[VotingManager] Роль не задана, предполагается мирный – голосование невозможно ночью.");
            return;
        }
    }

    // Проверка доступности голосования по фазе
    if (phaseManager.CurrentPhase != GamePhase.DayVoting && phaseManager.CurrentPhase != GamePhase.NightVoting)
    {
        Debug.Log("Голосование сейчас недоступно!");
        return;
    }

    // Получаем локальную роль отправителя (по умолчанию "civilian")
    string localRole = "civilian";
    if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("role", out object localRoleObj))
        localRole = localRoleObj.ToString().ToLower();

    // Отмечаем, что игрок проголосовал
    hasVoted = true;

    // Регистрируем голос через RPC, передавая также роль отправителя
    photonView.RPC("RPC_RegisterVote", RpcTarget.All, PhotonNetwork.NickName, votedPlayer, localRole);
}

        // RPC-метод для регистрации голоса.
        // Вызывается на всех клиентах.
[PunRPC]
public void RPC_RegisterVote(string votingPlayer, string votedPlayer, string votingPlayerRole)
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

    // Отправляем сообщение в чат вместе с ролью отправителя
    gameChat.photonView.RPC("RPC_AddMessage", RpcTarget.All, $"{votingPlayer} проголосовал за {votedPlayer}", votingPlayerRole);
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
    // 1. Находим максимальное число голосов, набранных за какого-либо игрока.
    int maxVotes = 0;
    foreach (var vote in votes.Values)
    {
        if (vote > maxVotes)
            maxVotes = vote;
    }

    // 2. Если максимальное число голосов равно 0, значит никто не получил голосов.
    if (maxVotes == 0)
    {
        // Сообщаем в чат, что голосование завершилось без результатов.
        gameChat.photonView.RPC("RPC_AddMessage", RpcTarget.All,
            "Голосование завершилось без голосов.", "");
        
        // Скрываем панель голосования.
        HideVotingPanel();
        return;
    }

    // 3. Собираем всех игроков, у которых число голосов равно maxVotes.
    List<string> topVotedPlayers = new List<string>();
    foreach (var pair in votes)
    {
        if (pair.Value == maxVotes)
            topVotedPlayers.Add(pair.Key);
    }

    // 4. Если несколько игроков набрали максимальное количество голосов,
    // выбираем случайного кандидата на устранение.
    int randomIndex = Random.Range(0, topVotedPlayers.Count);
    string eliminatedPlayer = topVotedPlayers[randomIndex];

    // 5. Отправляем сообщение в чат о том, что выбранный игрок покидает наш мир.
    gameChat.photonView.RPC("RPC_AddMessage", RpcTarget.All,
        $"{eliminatedPlayer} покинул наш мир", "");

    // 6. Вызываем RPC-метод RPC_KillPlayer из GameManager, чтобы пометить игрока как убитого.
    // Здесь используется ссылка на менеджер игры, чтобы не искать его заново.
    if (gameManager != null)
    {
        gameManager.photonView.RPC("RPC_KillPlayer", RpcTarget.All, eliminatedPlayer);
    }
    else
    {
        Debug.LogWarning("Ссылка на GameManager не установлена в VotingManager!");
    }

    // 7. Скрываем панель голосования после завершения процедуры.
    HideVotingPanel();
}


}
