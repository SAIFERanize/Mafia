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
    public TMP_Text playersColumn1; // Левый столбец
    public TMP_Text playersColumn2; // Правый столбец
    public Button exitButton;

    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        UpdateRoomInfo();
        UpdatePlayersList();
        playerNameText.text = "Вы: " + PhotonNetwork.NickName;
        
        exitButton.onClick.AddListener(ExitGame);
    }

    private void UpdateRoomInfo()
    {
        roomInfoText.text = "Комната: " + PhotonNetwork.CurrentRoom.Name;
        playerCountText.text = "Игроков: " + PhotonNetwork.CurrentRoom.PlayerCount;
    }

    private void UpdatePlayersList()
    {
        playersColumn1.text = "";
        playersColumn2.text = "";

        Player[] players = PhotonNetwork.PlayerList;
        for (int i = 0; i < players.Length; i++)
        {
            if (i % 2 == 0)
                playersColumn1.text += players[i].NickName + "\n";
            else
                playersColumn2.text += players[i].NickName + "\n";
        }
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
