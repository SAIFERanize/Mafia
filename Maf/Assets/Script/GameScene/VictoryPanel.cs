using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class VictoryPanel : MonoBehaviour
{
    [Header ("UI элементы")]
    public GameObject panel; // панель для победы
    public TMP_Text victoryMessage; // для сообщений
    public Button exitButton; // кнопка для закрытия панели

    void Start()
    {
        // панель скрыта в начале игры
        panel.SetActive(false);
        // обработчик собития для кнопки выхода
        exitButton.onClick.AddListener(OnexitButtonClicked);
    }

    public void ShowVictoryMessage(string message)
    {
        victoryMessage.text = message;
        panel.SetActive(true);
    }

    private void OnexitButtonClicked()
    {
         panel.SetActive(false);
    }
}