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

    // Метод для инициализации словаря голосов (вызывается у всех клиентов)
    private void InitVotingDictionary()
    {
        votes.Clear();
        // Заполняем словарь всеми игроками, даже если для UI панель не показывается
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            votes[player.NickName] = 0;
        }
    }

    // Метод для отображения панели голосования
    public void ShowVotingPanel()
    {   
        hasVoted = false;
        // Инициализируем словарь голосов у всех клиентов
        InitVotingDictionary();

        // Определяем, нужно ли показывать UI
        bool showUI = true;
        if (phaseManager.CurrentPhase == GamePhase.NightVoting)
        {
            object role;
            if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("role", out role))
            {
                // Если роль не mafia, не показываем UI (но словарь уже инициализирован)
                if (role.ToString().ToLower() != "mafia")
                {
                    Debug.Log("[VotingManager] Ночная фаза: мирный игрок не видит панель голосования.");
                    showUI = false;
                }
            }
            else
            {
                Debug.Log("[VotingManager] Роль не задана, предполагается мирный — голосование невозможно ночью.");
                showUI = false;
            }
        }

        if (showUI)
        {
            votePanel.SetActive(true);
            // Очистка предыдущих UI-элементов
            foreach (Transform child in votePanel.transform)
            {
                Destroy(child.gameObject);
            }
            votingEntries.Clear();

            // Для UI создаём элемент голосования только для других игроков
            foreach (Player player in PhotonNetwork.PlayerList) 
            {
                if (player.NickName != PhotonNetwork.LocalPlayer.NickName)
                {
                    CreateVotingEntry(player.NickName);
                }
            }
        }
        else
        {
            votePanel.SetActive(false);
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
            // Проверяем статус игрока
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                if (player.NickName == playerName)
                {
                    isDead = player.CustomProperties.ContainsKey("isDead") && (bool)player.CustomProperties["isDead"];
                    break;
                }
            }
            entry.Setup(playerName, isDead, OnVoteButtonClicked);
            votingEntries[playerName] = entry;
        }
    }
    
    // Метод, вызываемый при нажатии кнопки голосования.
    private void OnVoteButtonClicked(string votedPlayer)
    {
        if (hasVoted)
        {
            Debug.Log("Вы уже проголосовали!");
            return;
        }

        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("isDead", out object isDeadObj) && (bool)isDeadObj)
        {
            Debug.Log("[VotingManager] Мёртвые игроки не могут голосовать.");
            return;
        }

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

        if (phaseManager.CurrentPhase != GamePhase.DayVoting && phaseManager.CurrentPhase != GamePhase.NightVoting)
        {
            Debug.Log("Голосование сейчас недоступно!");
            return;
        }

        string localRole = "civilian";
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("role", out object localRoleObj))
            localRole = localRoleObj.ToString().ToLower();

        hasVoted = true;
        photonView.RPC("RPC_RegisterVote", RpcTarget.All, PhotonNetwork.NickName, votedPlayer, localRole);
    }

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

        gameChat.photonView.RPC("RPC_AddMessage", RpcTarget.All, $"{votingPlayer} проголосовал за {votedPlayer}", votingPlayerRole);
    }

    private void UpdateVotingUI(string playerName)
    {
        if (votingEntries.ContainsKey(playerName))
        {
            votingEntries[playerName].SetVoteCount(votes[playerName]);
        }
    }

    public void EndVoting()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            int maxVotes = 0;
            foreach (var vote in votes.Values)
            {
                if (vote > maxVotes)
                    maxVotes = vote;
            }

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

            List<string> topVotedPlayers = new List<string>();
            foreach (var pair in votes)
            {
                if (pair.Value == maxVotes)
                    topVotedPlayers.Add(pair.Key);
            }

            int randomIndex = Random.Range(0, topVotedPlayers.Count);
            string eliminatedPlayerName = topVotedPlayers[randomIndex];

            int eliminatedActorNumber = -1;
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                if (player.NickName == eliminatedPlayerName)
                {
                    eliminatedActorNumber = player.ActorNumber;
                    break;
                }
            }

            if (eliminatedActorNumber != -1)
            {
                gameChat.photonView.RPC("RPC_AddMessage", RpcTarget.All,
                    $"{eliminatedPlayerName} покинул наш мир", "");

                gameManager.photonView.RPC("RPC_SendVotingResult", RpcTarget.All, eliminatedActorNumber);

                if (victoryManager != null)
                {
                    victoryManager.CheckVictoryConditions();
                }
            }
            else
            {
                Debug.LogWarning("Не удалось найти ActorNumber для игрока: " + eliminatedPlayerName);
            }
        }
        HideVotingPanel();
        photonView.RPC("RPC_HideVotingPanel", RpcTarget.Others);
    }

    [PunRPC]
    public void RPC_HideVotingPanel()
    {
        HideVotingPanel();
    }
}
