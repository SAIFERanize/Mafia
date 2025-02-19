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
        descriptionText.text = roleDescription;
        gameObject.SetActive(true);
    }

    // Метод для скрытия панели
    public void HidePanel()
    {
        gameObject.SetActive(false);
    }
}
