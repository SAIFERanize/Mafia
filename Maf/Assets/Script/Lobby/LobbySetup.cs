
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class LobbySetup : MonoBehaviourPunCallbacks
{
    public TMP_InputField roomNameInput;
    public Toggle privateToggle;
    public TMP_InputField passwordInput;
    public Button startGameButton;
    public TMP_Text statusText;
    
    public Button exitbutton;


    private void Start()
    {
        // Отключаем поле пароля, если комната не приватная
        passwordInput.interactable = privateToggle.isOn;
        privateToggle.onValueChanged.AddListener(delegate { TogglePasswordField(); });

        // Подключаем обработчик к кнопке
        startGameButton.onClick.AddListener(CreateRoom);
        //кнопка выхода
         exitbutton.onClick.AddListener(ExitInMain);
    }

    private void TogglePasswordField()
    {
        passwordInput.interactable = privateToggle.isOn;
    }

    private void CreateRoom()
    {
        if (string.IsNullOrEmpty(roomNameInput.text))
        {
            statusText.text = "Введите имя комнаты!";
            return;
        }

        // Создаём настройки комнаты
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 10; // Максимум 10 игроков
        roomOptions.IsVisible = !privateToggle.isOn; // Если приватная - не видна в списке
        roomOptions.IsOpen = true;

        // Если комната приватная, сохраняем пароль в Custom Properties
        if (privateToggle.isOn)
        {
            ExitGames.Client.Photon.Hashtable customProperties = new ExitGames.Client.Photon.Hashtable();
            customProperties["password"] = passwordInput.text;
            roomOptions.CustomRoomProperties = customProperties;
            roomOptions.CustomRoomPropertiesForLobby = new string[] { "password" };
        }

        PhotonNetwork.CreateRoom(roomNameInput.text, roomOptions);
        statusText.text = "Создаём комнату...";
    }

    public override void OnCreatedRoom()
    {
        statusText.text = "Комната создана!";
        PhotonNetwork.LoadLevel("LobbyScene"); // Переход в лобби
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        statusText.text = "Ошибка создания комнаты: " + message;
    }
    private void ExitInMain(){
         PhotonNetwork.LoadLevel("MainMenu");
    }
}
