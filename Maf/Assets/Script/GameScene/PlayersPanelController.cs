using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Realtime;
using Photon.Pun;

public class PlayersPanelController : MonoBehaviour
{
    [Header("Ссылки на UI")]
    [Tooltip("Объект панели (Panel), который хотим показывать/скрывать")]
    public GameObject playersPanel;

    [Tooltip("Кнопка «Игроки», при нажатии открывает/закрывает панель")]
    public Button playersButton;

    [Tooltip("Текст для левой колонки")]
    public TMP_Text column1Text;

    [Tooltip("Текст для правой колонки")]
    public TMP_Text column2Text;

    [Header("Настройки цветов")]
    [Tooltip("Цвет для живых игроков")]
    public Color alivePlayerColor = Color.white;

    [Tooltip("Цвет для мёртвых игроков (тёмно-красный)")]
    public Color deadPlayerColor = new Color(0.5f, 0f, 0f);

    private bool isPanelOpen = false;

    private void Start()
    {
        // При старте панель скрыта
        if (playersPanel != null)
            playersPanel.SetActive(false);

        // Подписываемся на кнопку «Игроки»
        if (playersButton != null)
            playersButton.onClick.AddListener(TogglePlayersPanel);
    }

    // Открыть/закрыть панель
    public void TogglePlayersPanel()
    {
        isPanelOpen = !isPanelOpen;
        if (playersPanel != null)
            playersPanel.SetActive(isPanelOpen);

        // Если открываем панель — сразу обновляем список
        if (isPanelOpen)
        {
            UpdatePlayersList();
        }
    }

    // Собираем список игроков из PhotonNetwork.PlayerList и раскладываем в две колонки
    public void UpdatePlayersList()
    {
        if (column1Text == null || column2Text == null)
            return;

        // Очищаем тексты в колонках
        column1Text.text = "";
        column2Text.text = "";

        // Флаг, указывающий, куда писать следующего игрока: в левую (true) или правую (false) колонку
        bool writeToColumn1 = true;

        // Проходим по всем игрокам
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            // Проверяем, жив игрок или мёртв
            bool isDead = player.CustomProperties.ContainsKey("isDead")
                          && (bool)player.CustomProperties["isDead"];

            // Определяем цвет
            Color colorToUse = isDead ? deadPlayerColor : alivePlayerColor;
            // Переводим в Hex-формат для TMP
            string colorHex = ColorUtility.ToHtmlStringRGB(colorToUse);

            // Готовим строку с раскраской
            string playerLine = $"<color=#{colorHex}>{player.NickName}</color>\n";

            // Чередуем: в левую или правую колонку
            if (writeToColumn1)
                column1Text.text += playerLine;
            else
                column2Text.text += playerLine;

            // Переключаем флаг
            writeToColumn1 = !writeToColumn1;
        }
    }
}
