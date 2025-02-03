using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;

public class MainMenu : MonoBehaviourPunCallbacks
{
    public TMP_InputField nicknameInput;
    public Button saveNicknameButton;
    public Button exitButton;
    public Button createGameButton;
    public Button findGameButton;
    public TMP_Text statusText; // Добавляем текстовый объект для статуса подключения

    private void Start()
    {
        // Загружаем ник при старте
        if (PlayerPrefs.HasKey("PlayerNickname"))
        {
            nicknameInput.text = PlayerPrefs.GetString("PlayerNickname");
        }

        // Добавляем обработчики на кнопки
        saveNicknameButton.onClick.AddListener(SaveNickname);
        exitButton.onClick.AddListener(ExitGame);
        createGameButton.onClick.AddListener(CreateGame);
        findGameButton.onClick.AddListener(FindGame);

        // Подключаемся к Photon
        if (!PhotonNetwork.IsConnected)
        {
            statusText.text = "Подключение к серверу...";
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnConnectedToMaster()
    {
        statusText.text = "Подключено к серверу!";
        PhotonNetwork.NickName = PlayerPrefs.GetString("PlayerNickname", "Игрок" + Random.Range(1000, 9999));
    }

    private void SaveNickname()
    {
        if (!string.IsNullOrEmpty(nicknameInput.text))
        {
            PlayerPrefs.SetString("PlayerNickname", nicknameInput.text);
            PlayerPrefs.Save();
            PhotonNetwork.NickName = nicknameInput.text;
            Debug.Log("Ник сохранен: " + nicknameInput.text);
        }
    }

    private void ExitGame()
    {
        Debug.Log("Выход из игры");
        Application.Quit();
    }

    private void CreateGame()
    {
        if (!PhotonNetwork.IsConnected)
        {
            statusText.text = "Нет подключения к серверу!";
            return;
        }

        Debug.Log("Переход в сцену настройки лобби...");
        SceneManager.LoadScene("LobbySetup");
    }

    private void FindGame()
{
    if (!PhotonNetwork.IsConnected)
    {
        statusText.text = "Нет подключения к серверу!";
        return;
    }

    PhotonNetwork.LoadLevel("RoomList");
}
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        statusText.text = "Не найдено свободных комнат.";
        Debug.Log("Не найдено свободных комнат, создаем новую...");
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 10 });
    }

    public override void OnJoinedRoom()
    {
        statusText.text = "Подключено к комнате!";
        SceneManager.LoadScene("LobbyScene");
    }
}
