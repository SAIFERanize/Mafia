using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

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

    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        UpdateRoomInfo();

        exitButton.onClick.AddListener(ExitGame);

        // Назначаем роли при старте (если мы мастер)
        if (PhotonNetwork.IsMasterClient)
        {
            Player[] players = PhotonNetwork.PlayerList;

            // Случайный индекс мафии
            int mafiaIndex = Random.Range(0, players.Length);

            // Случайный индекс комиссара (не совпадающий с мафией)
            int commissionerIndex = mafiaIndex;
            while (commissionerIndex == mafiaIndex)
            {
                commissionerIndex = Random.Range(0, players.Length);
            }

            for (int i = 0; i < players.Length; i++)
            {
                string role = "civilian";
                if (i == mafiaIndex) role = "mafia";
                else if (i == commissionerIndex) role = "commissar"; // новая роль

                ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
                {
                    { "role", role }
                };
                players[i].SetCustomProperties(props);
                Debug.Log($"[Role Assignment] Игрок {players[i].NickName} получил роль: {role}");
            }
        }

        // Обновляем имя и роль для локального игрока
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
            // Если роль еще не назначена, задаем дефолтное значение (например, civilian)
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

    // Обновляем панель игроков при входе/выходе
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

    // Обработка обновления свойств игрока
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (changedProps.ContainsKey("role"))
        {
            string newRole = targetPlayer.CustomProperties["role"].ToString();
            Debug.Log($"[Role Update] {targetPlayer.NickName} получил роль {newRole}");

            if (targetPlayer.IsLocal)
            {
                // Обновляем UI для локального игрока
                playerNameText.text = "Вы: " + PhotonNetwork.NickName + " (" + newRole + ")";
                if (roleInfoText != null)
                    roleInfoText.text = "Ваша роль: " + newRole;
            }
        }
    }

   [PunRPC]
    [System.Obsolete]
    public void RPC_KillPlayer(string playerName)
{
    foreach (Player player in PhotonNetwork.PlayerList)
    {
        if (player.NickName == playerName)
        {
            Debug.Log($"[GameManager] Устанавливаем isDead для {player.NickName}");
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
            {
                { "isDead", true }
            };
            player.SetCustomProperties(props);

            // Проверяем, не комиссар ли
            if (player.CustomProperties.TryGetValue("role", out object roleObj))
            {
                if (roleObj.ToString().ToLower() == "commissar")
                {
                    // Пишем в чат
                    GameChat chat = FindObjectOfType<GameChat>();
                    if (chat != null)
                    {
                        chat.photonView.RPC("RPC_AddMessage", RpcTarget.All, "Внимание! комиссар мертв!", " ");
                    }
                }
            }
            break;
        }
    }

    if (playersPanelController != null)
        playersPanelController.UpdatePlayersList();
}


    public override void OnJoinedRoom()
    {
        // Если роль уже установлена, обновляем UI локального игрока
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("role", out object roleObj))
        {
            string roleText = roleObj.ToString();
            Debug.Log($"[OnJoinedRoom] {PhotonNetwork.LocalPlayer.NickName} имеет роль: {roleText}");
            playerNameText.text = "Вы: " + PhotonNetwork.NickName + " (" + roleText + ")";
            if (roleInfoText != null)
            {
                roleInfoText.text = "Ваша роль: " + roleText;
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

    public void ExitGame()
    {
        PhotonNetwork.LeaveRoom();
        PhotonNetwork.LoadLevel("RoomList");
    }
}