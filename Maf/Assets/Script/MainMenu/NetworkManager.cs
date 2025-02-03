using UnityEngine;
using Photon.Pun;
using TMPro;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public TMP_Text statusText;

    private void Start()
    {
        statusText.text = "Подключение к серверу...";
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        statusText.text = "Подключен к серверу!";
        PhotonNetwork.NickName = PlayerPrefs.GetString("PlayerNickname", "Игрок" + Random.Range(1000, 9999));
    }

    public void CreateGame()
    {
        PhotonNetwork.CreateRoom(null);
    }

    public void FindGame()
    {
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinedRoom()
    {
        statusText.text = "Вы в комнате!";
        PhotonNetwork.LoadLevel("GameScene");
    }
}

