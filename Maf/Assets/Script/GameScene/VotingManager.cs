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

    // Ссылка на VictoryManager для проверки победы после убийства
    public VictoryManager victoryManager;

    private bool hasVoted = false;

    // Словарь для хранения голосов: ключ – ник игрока, значение – число голосов
    private Dictionary<string, int> votes = new Dictionary<string, int>();
    // Словарь для хранения ссылок на созданные элементы голосования
    private Dictionary<string, VotingEntry> votingEntries = new Dictionary<string, VotingEntry>();

    // Метод для отображения панели голосования
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
        foreach (Transform child in votePanel.transform)
        {
            Destroy(child.gameObject);
        }
        votingEntries.Clear();
        votes.Clear();

        // Добавляем всех игроков в словарь голосов, чтобы учесть даже локального игрока
        foreach (Player player in PhotonNetwork.PlayerList) 
        {
            votes[player.NickName] = 0;
            // Для UI создаём элемент голосования только для других игроков
            if (player.NickName != PhotonNetwork.LocalPlayer.NickName)
            {
                CreateVotingEntry(player.NickName);
            }
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
    public void EndVoting()
    {
        // Если мы мастер-клиент, то выполняем логику выбора и убийства
        if (PhotonNetwork.IsMasterClient)
        {
            // 1. Находим максимальное число голосов
            int maxVotes = 0;
            foreach (var vote in votes.Values)
            {
                if (vote > maxVotes)
                    maxVotes = vote;
            }

            // 2. Если никто не получил голосов, отправляем сообщение (только если не ночное голосование)
            if (maxVotes == 0)
            {
                if (phaseManager.CurrentPhase != GamePhase.NightVoting)
                {
                    gameChat.photonView.RPC("RPC_AddMessage", RpcTarget.All,
                        "Голосование завершилось без голосов.", "");
                }
                photonView.RPC("RPC_HideVotingPanel", RpcTarget.All);
                return;
            }

            // 3. Собираем всех игроков, набравших maxVotes
            List<string> topVotedPlayers = new List<string>();
            foreach (var pair in votes)
            {
                if (pair.Value == maxVotes)
                    topVotedPlayers.Add(pair.Key);
            }

            // 4. Если несколько игроков набрали максимальное число голосов, выбираем случайного
            int randomIndex = Random.Range(0, topVotedPlayers.Count);
            string eliminatedPlayer = topVotedPlayers[randomIndex];

            // 5. Отправляем сообщение в чат
            gameChat.photonView.RPC("RPC_AddMessage", RpcTarget.All,
                $"{eliminatedPlayer} покинул наш мир", "");

            // 6. Вызываем RPC для убийства выбранного игрока
            if (gameManager != null)
            {
                gameManager.photonView.RPC("RPC_KillPlayer", RpcTarget.All, eliminatedPlayer);
                // После убийства проверяем условия победы
                if (victoryManager != null)
                {
                    victoryManager.CheckVictoryConditions();
                }
            }
            else
            {
                Debug.LogWarning("Ссылка на GameManager не установлена в VotingManager!");
            }
        }

        // Независимо от того, мастер мы или нет – скрываем панель голосования локально у всех
        HideVotingPanel();
        photonView.RPC("RPC_HideVotingPanel", RpcTarget.Others);
    }

    [PunRPC]
    public void RPC_HideVotingPanel()
    {
        HideVotingPanel();
    }
}
