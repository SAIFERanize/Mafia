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
public string GetRoleDescription(string role)
{
    // Приводим роль к нижнему регистру для универсальности.
    switch (role.ToLower())
    {
        case "mafia":
            return "Вы - мафия. Ваша цель — устранить всех мирных жителей, оставаясь в тени. Будьте хитры и расчетливы.";
        case "commissar":
            return "Вы - комиссар. Используйте свои аналитические способности, чтобы выявить мафию и защитить невинных.";
        case "civilian":
        default:
            return "Вы - мирный житель. Ваша задача — выжить и помочь разоблачить мафию.";
    }
}

}
