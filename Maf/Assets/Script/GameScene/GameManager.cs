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
    // Проверка изменения свойства "role" (уже было)
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

    // Проверка изменения свойства "isDead"
    if (changedProps.ContainsKey("isDead"))
    {
        // Получаем значение свойства. Предполагаем, что значение хранится как bool.
        bool isDead = (bool)targetPlayer.CustomProperties["isDead"];
        Debug.Log($"[Death Update] {targetPlayer.NickName} isDead = {isDead}");

        // Если обновление касается локального игрока и флаг установлен в true
        if (targetPlayer.IsLocal && isDead)
        {
            // Вызываем окно смерти, передавая имя игрока для отображения сообщения
            if (deathWindow != null)
            {
                deathWindow.ShowDeathMessage(PhotonNetwork.NickName);
            }
        }
    }
}

public string GetRoleDescription(string role)
{
    // Приводим роль к нижнему регистру для универсальности.
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

            // Если убит комиссар, объявляем это в чате
            if (player.CustomProperties.TryGetValue("role", out object roleObj))
            {
                if (roleObj.ToString().ToLower() == "commissar")
                {
                    GameChat chat = FindAnyObjectByType<GameChat>();
                    if (chat != null)
                    {
                        chat.photonView.RPC("RPC_AddMessage", RpcTarget.All,
                            "Внимание! Комиссар мертв!", " ");
                    }
                }
            }
            break; // прекращаем цикл после нахождения игрока
        }
    }

    if (playersPanelController != null)
        playersPanelController.UpdatePlayersList();

    // Если мы мастер, вызываем проверку победы (чтобы не было повторных вызовов)
    if (PhotonNetwork.IsMasterClient && victoryManager != null)
        victoryManager.CheckVictoryConditions();
}




   public override void OnJoinedRoom()
{
    // Если роль уже назначена, получаем её из кастомных свойств.
    if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("role", out object roleObj))
    {
        string roleText = roleObj.ToString();
        Debug.Log($"[OnJoinedRoom] {PhotonNetwork.LocalPlayer.NickName} имеет роль: {roleText}");

        // Обновляем UI с именем и ролью.
        playerNameText.text = "Вы: " + PhotonNetwork.NickName + " (" + roleText + ")";
        if (roleInfoText != null)
        {
            roleInfoText.text = "Ваша роль: " + roleText;
        }

        // Получаем интересное описание для роли.
        string description = GetRoleDescription(roleText);

        // Если панель описания роли подключена, отображаем описание.
        if (roleDescriptionPanel != null)
        {
            roleDescriptionPanel.ShowPanel(description);
        }
    }
    else
    {
        // Если роль не установлена, назначаем значение по умолчанию.
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
     public override void OnLeftRoom()
    {
        // Сбрасываем настройки, связанные с игровой логикой.
        Hashtable resetProps = new Hashtable();
        resetProps["isDead"] = false;
        PhotonNetwork.LocalPlayer.SetCustomProperties(resetProps);
    }
} 