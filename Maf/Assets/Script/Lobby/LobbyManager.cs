
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    public TMP_Text roomInfoText;
    public TMP_Text playersListText;
    public Button startGameButton;

    private void Start()
    {
        // Обновляем информацию о комнате
        UpdateRoomInfo();
        UpdatePlayersList();

        // Кнопка "Начать игру" доступна только создателю комнаты
        startGameButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
        startGameButton.onClick.AddListener(StartGame);
    }

    private void UpdateRoomInfo()
    {
        roomInfoText.text = "Комната: " + PhotonNetwork.CurrentRoom.Name +
                            (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("password") ? " (Приватная)" : "");
    }

    private void UpdatePlayersList()
    {
        playersListText.text = "Игроки:\n";
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            playersListText.text += player.NickName + "\n";
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
        PhotonNetwork.LoadLevel("GameScene"); // Загружаем игровую сцену
    }
}
