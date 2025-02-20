using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeathWindow : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject deathWindowPanel;  // Панель с окном смерти
    public TMP_Text deathMessageText;    // Текст, отображающий сообщение о смерти
    public Button closeButton;           // Кнопка для закрытия окна

    private void Start()
    {
        // Назначаем обработчик нажатия кнопки закрытия
        closeButton.onClick.AddListener(CloseWindow);
        // Скрываем окно по умолчанию
        deathWindowPanel.SetActive(false);
    }

    // Метод для отображения окна смерти
    public void ShowDeathMessage(string playerName)
    {
        deathMessageText.text = $"К сожалению, дорогой {playerName}, вы были убиты. Теперь вам остается только наблюдать за игрой и надеяться, что ваши товарищи отомстят!";
        deathWindowPanel.SetActive(true);
    }

    // Метод для закрытия окна смерти
    private void CloseWindow()
    {
        deathWindowPanel.SetActive(false);
    }
}
