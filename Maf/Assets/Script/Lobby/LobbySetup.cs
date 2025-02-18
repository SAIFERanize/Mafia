using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class LobbySetup : MonoBehaviourPunCallbacks
{
    [Header("Настройки комнаты")]
    public TMP_InputField roomNameInput;    // Имя комнаты
    public Toggle privateToggle;            // Приватная ли комната
    public TMP_InputField passwordInput;    // Поле пароля
    public Button startGameButton;          // Кнопка создания комнаты
    public TMP_Text statusText;             // Текст статуса
    public Button exitButton;               // Кнопка выхода в главное меню

    [Header("Настройки времени фаз (секунды)")]
    // Четыре ползунка для установки времени каждой фазы
    public Slider nightDiscussionSlider;
    public Slider nightVotingSlider;
    public Slider dayDiscussionSlider;
    public Slider dayVotingSlider;

    // Текстовые поля для отображения выбранного значения ползунка (необязательно)
    public TMP_Text nightDiscussionValueText;
    public TMP_Text nightVotingValueText;
    public TMP_Text dayDiscussionValueText;
    public TMP_Text dayVotingValueText;

    private void Start()
    {
        // Задаём минимальное и максимальное значение для ползунков (от 10 до 240 секунд)
        nightDiscussionSlider.minValue = 10;
        nightDiscussionSlider.maxValue = 60;
        nightVotingSlider.minValue = 10;
        nightVotingSlider.maxValue = 60;
        dayDiscussionSlider.minValue = 10;
        dayDiscussionSlider.maxValue = 60;
        dayVotingSlider.minValue = 10;
        dayVotingSlider.maxValue = 60;

        // Устанавливаем дефолтные значения (например, как в предыдущем варианте)
        nightDiscussionSlider.value = 10;
        nightVotingSlider.value = 10;
        dayDiscussionSlider.value = 10;
        dayVotingSlider.value = 10;

        // Обновляем текстовые поля значений (если они заданы)
        UpdateSliderValueTexts();

        // При изменении ползунка обновляем отображаемое значение
        nightDiscussionSlider.onValueChanged.AddListener(delegate { UpdateSliderValueTexts(); });
        nightVotingSlider.onValueChanged.AddListener(delegate { UpdateSliderValueTexts(); });
        dayDiscussionSlider.onValueChanged.AddListener(delegate { UpdateSliderValueTexts(); });
        dayVotingSlider.onValueChanged.AddListener(delegate { UpdateSliderValueTexts(); });

        // Поле пароля активно только если комната приватная
        passwordInput.interactable = privateToggle.isOn;
        privateToggle.onValueChanged.AddListener(delegate { TogglePasswordField(); });

        // Обработчики нажатий на кнопки
        startGameButton.onClick.AddListener(CreateRoom);
        exitButton.onClick.AddListener(ExitToMain);
    }

    // Метод обновления текстовых полей для отображения текущего значения ползунка
    private void UpdateSliderValueTexts()
    {
        if (nightDiscussionValueText != null)
            nightDiscussionValueText.text = Mathf.RoundToInt(nightDiscussionSlider.value).ToString();
        if (nightVotingValueText != null)
            nightVotingValueText.text = Mathf.RoundToInt(nightVotingSlider.value).ToString();
        if (dayDiscussionValueText != null)
            dayDiscussionValueText.text = Mathf.RoundToInt(dayDiscussionSlider.value).ToString();
        if (dayVotingValueText != null)
            dayVotingValueText.text = Mathf.RoundToInt(dayVotingSlider.value).ToString();
    }

    // Включаем/выключаем поле пароля
    private void TogglePasswordField()
    {
        passwordInput.interactable = privateToggle.isOn;
    }

    // Создание комнаты с передачей настроек через CustomRoomProperties
    private void CreateRoom()
    {
        if (string.IsNullOrEmpty(roomNameInput.text))
        {
            statusText.text = "Введите имя комнаты!";
            return;
        }

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 10;
        roomOptions.IsVisible = !privateToggle.isOn;
        roomOptions.IsOpen = true;

        Hashtable customProperties = new Hashtable();

        // Если комната приватная, сохраняем пароль
        if (privateToggle.isOn)
        {
            customProperties["password"] = passwordInput.text;
            roomOptions.CustomRoomPropertiesForLobby = new string[] { "password" };
        }

        // Считываем время для каждой фазы из ползунков
        float nightDiscussionTime = nightDiscussionSlider.value;
        float nightVotingTime = nightVotingSlider.value;
        float dayDiscussionTime = dayDiscussionSlider.value;
        float dayVotingTime = dayVotingSlider.value;

        customProperties["nightDiscussionTime"] = nightDiscussionTime;
        customProperties["nightVotingTime"] = nightVotingTime;
        customProperties["dayDiscussionTime"] = dayDiscussionTime;
        customProperties["dayVotingTime"] = dayVotingTime;

        roomOptions.CustomRoomProperties = customProperties;

        PhotonNetwork.CreateRoom(roomNameInput.text, roomOptions);
        statusText.text = "Создаём комнату...";
    }

    public override void OnCreatedRoom()
    {
        statusText.text = "Комната создана!";
        PhotonNetwork.LoadLevel("LobbyScene");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        statusText.text = "Ошибка создания комнаты: " + message;
    }

    private void ExitToMain()
    {
        PhotonNetwork.LoadLevel("MainMenu");
    }
}
