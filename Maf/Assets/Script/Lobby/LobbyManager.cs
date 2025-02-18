using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    public TMP_Text roomInfoText;
    public TMP_Text playersColumn1; // Левый столбец
    public TMP_Text playersColumn2; // Правый столбец
    public Button startGameButton;

    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        UpdateRoomInfo();
        UpdatePlayersList();
        startGameButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
        startGameButton.onClick.AddListener(StartGame);
    }

    private void UpdateRoomInfo()
    {
        roomInfoText.text = "" + PhotonNetwork.CurrentRoom.Name +
                            (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("password") ? " (Приватная)" : "");
    }

    private void UpdatePlayersList()
    {
        playersColumn1.text = "";
        playersColumn2.text = "";

        Player[] players = PhotonNetwork.PlayerList;
        for (int i = 0; i < players.Length; i++)
        {
            if (i % 2 == 0)
                playersColumn1.text += players[i].NickName + "\n"; // В левый столбец
            else
                playersColumn2.text += players[i].NickName + "\n"; // В правый столбец
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayersList();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayersList();
    }

    public void StartGame()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("GameScene");
        }
    }
}
