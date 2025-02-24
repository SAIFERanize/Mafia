using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class GameManager : MonoBehaviourPunCallbacks
{
    [Header("UI")]
    public TMP_Text roomInfoText;
    public TMP_Text playerCountText;
    public TMP_Text playerNameText;
    public TMP_Text roleInfoText;
    public Button exitButton;

    [Header("Ссылка на панель игроков (не забудь мудак)")]
    public PlayersPanelController playersPanelController;
    public RoleDescriptionPanel roleDescriptionPanel;
    public DeathWindow deathWindow;
    public VictoryManager victoryManager;

    private void Start()
    {   
        PhotonNetwork.AutomaticallySyncScene = true;
        UpdateRoomInfo();
        exitButton.onClick.AddListener(ExitGame);

        // Если мы мастер, назначаем роли и уникальные номера игрокам
        if (PhotonNetwork.IsMasterClient)
        {
            Player[] players = PhotonNetwork.PlayerList;

            // Выбираем случайного мафию и комиссара
            int mafiaIndex = Random.Range(0, players.Length);
            int commissionerIndex = mafiaIndex;
            while (commissionerIndex == mafiaIndex)
            {
                commissionerIndex = Random.Range(0, players.Length);
            }

            for (int i = 0; i < players.Length; i++)
            {
                string role = "civilian";
                if (i == mafiaIndex) role = "mafia";
                else if (i == commissionerIndex) role = "commissar";

                // Назначаем игроку роль, уникальный номер и дефолтное состояние "не мёртв"
                ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
                {
                    { "role", role },
                    { "playerNumber", i },
                    { "isDead", false }
                };
                players[i].SetCustomProperties(props);
                Debug.Log($"[Role Assignment] Игрок {players[i].NickName} получил роль: {role} с номером {i}");
            }
        }

        // Обновляем UI для локального игрока
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("role", out object roleObj))
        {
            string roleText = roleObj.ToString();
            playerNameText.text = "Вы: " + PhotonNetwork.NickName + " (" + roleText + ")";
            if (roleInfoText != null)
            {
                roleInfoText.text = "Ваша роль: " + roleText;
            }
        }
        else
        {
            playerNameText.text = "Вы: " + PhotonNetwork.NickName + " (civilian)";
            if (roleInfoText != null)
            {
                roleInfoText.text = "Ваша роль: civilian";
            }
        }
    }

    private void UpdateRoomInfo()
    {
        roomInfoText.text = "Комната: " + PhotonNetwork.CurrentRoom.Name;
        playerCountText.text = "Игроков: " + PhotonNetwork.CurrentRoom.PlayerCount;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdateRoomInfo();
        if (playersPanelController != null)
            playersPanelController.UpdatePlayersList();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdateRoomInfo();
        if (playersPanelController != null)
            playersPanelController.UpdatePlayersList();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        // Обработка изменения роли
        if (changedProps.ContainsKey("role"))
        {
            string newRole = targetPlayer.CustomProperties["role"].ToString();
            Debug.Log($"[Role Update] {targetPlayer.NickName} получил роль {newRole}");

            if (targetPlayer.IsLocal)
            {
                playerNameText.text = "Вы: " + PhotonNetwork.NickName + " (" + newRole + ")";
                if (roleInfoText != null)
                {
                    roleInfoText.text = "Ваша роль: " + newRole;
                }

                string description = GetRoleDescription(newRole);
                if (roleDescriptionPanel != null)
                {
                    roleDescriptionPanel.ShowPanel(description);
                }
            }
        }

        // Обработка изменения свойства "isDead"
        if (changedProps.ContainsKey("isDead"))
        {
            bool isDead = (bool)targetPlayer.CustomProperties["isDead"];
            Debug.Log($"[Death Update] {targetPlayer.NickName} isDead = {isDead}");

            if (targetPlayer.IsLocal && isDead)
            {
                if (deathWindow != null)
                {
                    deathWindow.ShowDeathMessage(PhotonNetwork.NickName);
                }
            }
        }
    }

    public string GetRoleDescription(string role)
    {
        switch (role.ToLower())
        {
            case "mafia":
                return "Вы - мафия. Ваша цель — устранить всех мирных жителей, оставаясь в тени. Будьте хитры и расчетливы.";
            case "commissar":
                return "Вы - комиссар. Используйте свои аналитические способности, чтобы выявить мафию и защитить невинных.";
            case "civilian":
            default:
                return "Вы - мирный житель. Ваша задача — выжить и помочь разоблачить мафию.";
        }
    }

    [PunRPC]
    public void RPC_KillPlayer_ByActorNumber(int targetActorNumber)
    {
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.ActorNumber == targetActorNumber)
            {
                Debug.Log($"[GameManager] Устанавливаем isDead для {player.NickName} (ActorNumber {targetActorNumber})");
                
                ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
                {
                    { "isDead", true }
                };
                player.SetCustomProperties(props);
                
                if (player.CustomProperties.TryGetValue("role", out object roleObj) &&
                    roleObj.ToString().ToLower() == "commissar")
                {
                    GameChat chat = FindAnyObjectByType<GameChat>();
                    if (chat != null)
                    {
                        chat.photonView.RPC("RPC_AddMessage", RpcTarget.All,
                            "Внимание! Комиссар мертв!", " ");
                    }
                }
                
                if (player.IsLocal)
                {
                    Debug.Log("[GameManager] Локальный игрок (хост) убит. Вызываем окно смерти.");
                    if (deathWindow != null)
                    {
                        deathWindow.ShowDeathMessage(player.NickName);
                    }
                }
                
                break;
            }
        }

        if (playersPanelController != null)
            playersPanelController.UpdatePlayersList();

        if (PhotonNetwork.IsMasterClient && victoryManager != null)
            victoryManager.CheckVictoryConditions();
    }

    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("role", out object roleObj))
        {
            string roleText = roleObj.ToString();
            Debug.Log($"[OnJoinedRoom] {PhotonNetwork.LocalPlayer.NickName} имеет роль: {roleText}");
            playerNameText.text = "Вы: " + PhotonNetwork.NickName + " (" + roleText + ")";
            if (roleInfoText != null)
            {
                roleInfoText.text = "Ваша роль: " + roleText;
            }

            string description = GetRoleDescription(roleText);
            if (roleDescriptionPanel != null)
            {
                roleDescriptionPanel.ShowPanel(description);
            }
        }
        else
        {
            Debug.LogWarning("[OnJoinedRoom] Роль не установлена, задаём civilian по умолчанию");
            playerNameText.text = "Вы: " + PhotonNetwork.NickName + " (civilian)";
            if (roleInfoText != null)
            {
                roleInfoText.text = "Ваша роль: civilian";
            }
        }
    }

    [PunRPC]
    public void RPC_SendVotingResult(int eliminatedActorNumber)
    {
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.ActorNumber == eliminatedActorNumber)
            {
                Debug.Log($"[GameManager] RPC_SendVotingResult: убиваем {player.NickName} (ActorNumber {eliminatedActorNumber})");
                
                ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
                {
                    { "isDead", true }
                };
                player.SetCustomProperties(props);
                
                if (player.CustomProperties.TryGetValue("role", out object roleObj) &&
                    roleObj.ToString().ToLower() == "commissar")
                {
                    GameChat chat = FindAnyObjectByType<GameChat>();
                    if (chat != null)
                    {
                        chat.photonView.RPC("RPC_AddMessage", RpcTarget.All,
                            "Внимание! Комиссар мертв!", " ");
                    }
                }
                
                if (player.IsLocal)
                {
                    Debug.Log("[GameManager] Локальный игрок убит. Вызываем окно смерти.");
                    if (deathWindow != null)
                    {
                        deathWindow.ShowDeathMessage(player.NickName);
                    }
                }
                
                break;
            }
        }
        
        if (playersPanelController != null)
            playersPanelController.UpdatePlayersList();
    }

    public void ExitGame()
    {
        PhotonNetwork.LeaveRoom();
        PhotonNetwork.LoadLevel("RoomList");
    }

    public override void OnLeftRoom()
    {
        Hashtable resetProps = new Hashtable();
        resetProps["isDead"] = false;
        PhotonNetwork.LocalPlayer.SetCustomProperties(resetProps);
    }
}
