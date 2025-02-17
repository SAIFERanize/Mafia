    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;
    using Photon.Pun;
    using Photon.Realtime;

    public class GameManager : MonoBehaviourPunCallbacks
    {
        public TMP_Text roomInfoText;
        public TMP_Text playerCountText;
        public TMP_Text playerNameText;
        public TMP_Text roleInfoText;
        public TMP_Text playersColumn1; // Левый столбец
        public TMP_Text playersColumn2; // Правый столбец
        public Button exitButton;

       private void Start()
{
    PhotonNetwork.AutomaticallySyncScene = true;
    UpdateRoomInfo();
    UpdatePlayersList();
    
    exitButton.onClick.AddListener(ExitGame);
    
    if (PhotonNetwork.IsMasterClient)
    {
        Player[] players = PhotonNetwork.PlayerList;
        // Выбираем случайного игрока для мафии
        int mafiaIndex = Random.Range(0, players.Length);
    
        foreach (Player player in players)
        {
            string role = (player == players[mafiaIndex]) ? "mafia" : "civilian";
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
            {
                { "role", role }
            };
            player.SetCustomProperties(props);
            Debug.Log($"[Role Assignment] Игрок {player.NickName} получил роль: {role}");
        }
    }
    
    // Обновляем текст с именем и ролью для локального игрока
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
         playerNameText.text = "Вы: " + PhotonNetwork.NickName;
         if (roleInfoText != null)
         {
             roleInfoText.text = "Роль не назначена";
         }
    }
}
        private void UpdateRoomInfo()
        {
            roomInfoText.text = "Комната: " + PhotonNetwork.CurrentRoom.Name;
            playerCountText.text = "Игроков: " + PhotonNetwork.CurrentRoom.PlayerCount;
        }


        // Обновляем список игроков
private void UpdatePlayersList()
{
    playersColumn1.text = "";
    playersColumn2.text = "";

    foreach (Player player in PhotonNetwork.PlayerList)
    {
        bool isDead = player.CustomProperties.ContainsKey("isDead") && (bool)player.CustomProperties["isDead"];
        string colorTag = isDead ? "<color=red>" : "<color=white>";
        string playerText = $"{colorTag}{player.NickName}</color>\n";

        if (playersColumn1.text.Length <= playersColumn2.text.Length)
            playersColumn1.text += playerText;
        else
            playersColumn2.text += playerText;

        // --- ЛОГИ О ПЕРЕКРАШИВАНИИ ---
        if (isDead)
        {
            Debug.Log($"[GameManager] Игрок {player.NickName} помечен как мёртвый => цвет красный");
        }
        else
        {
            Debug.Log($"[GameManager] Игрок {player.NickName} жив => цвет белый");
        }
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
            break;
        }
    }
    UpdatePlayersList();
}

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            UpdateRoomInfo();
            UpdatePlayersList();
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            UpdateRoomInfo();
            UpdatePlayersList();
        }

        public void ExitGame()
        {
            PhotonNetwork.LeaveRoom();
            PhotonNetwork.LoadLevel("RoomList");
        }
    }
