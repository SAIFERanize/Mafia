using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoleDescriptionPanel : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text descriptionText;  // Текст описания роли
    public Button closeButton;        // Кнопка закрытия (крестик)

    void Start()
    {
        // Назначаем обработчик нажатия кнопки закрытия
        closeButton.onClick.AddListener(HidePanel);
    }

    // Метод для отображения панели с нужным описанием роли
    public void ShowPanel(string roleDescription)
{
    // Устанавливаем текст описания роли, полученный в параметре.
    descriptionText.text = roleDescription;
    // Делаем панель видимой, активируя объект.
    gameObject.SetActive(true);
}

public void HidePanel()
{
    // Скрываем панель, деактивируя объект.
    gameObject.SetActive(false);
}
// Возвращает описание для заданной роли.
}
