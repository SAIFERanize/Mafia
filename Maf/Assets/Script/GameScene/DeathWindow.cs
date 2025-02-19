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
        closeButton.onClick.AddListener(CloseWindow);
        deathWindowPanel.SetActive(false);  // Скрываем окно по умолчанию
    }

    // Метод для отображения окна смерти
    public void ShowDeathMessage(string playerName)
    {
        deathMessageText.text = $"К сожалению дорогой {playerName}, вы были убиты, теперь вам остается только наблюдать на всем и надеятся что ваши товарищи смогут отомстить!";
        deathWindowPanel.SetActive(true);  // Показываем окно
    }

    // Метод для закрытия окна
    private void CloseWindow()
    {
        deathWindowPanel.SetActive(false);  // Скрываем окно
    }
}
