using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class VotingEntry : MonoBehaviour
{
    [Header("Ссылки на UI элементы")]
    [Tooltip("Текст для отображения ника игрока")]
    public TMP_Text playerNameText;
    
    [Tooltip("Текст для отображения количества голосов")]
    public TMP_Text voteCountText;
    
    [Tooltip("Кнопка голосования")]
    public Button voteButton;

    // Callback-функция, которая вызывается при нажатии кнопки голосования
    private Action<string> onVoteButtonClicked;
    private string playerName;

    /// Инициализирует элемент голосования.
    public void Setup(string name, Action<string> callback)
    {
        playerName = name;
        playerNameText.text = name;
        voteCountText.text = "0";
        onVoteButtonClicked = callback;

        // Убираем возможные старые слушатели и добавляем новый
        voteButton.onClick.RemoveAllListeners();
        voteButton.onClick.AddListener(() => onVoteButtonClicked?.Invoke(playerName));
    }

    /// Обновляет отображение количества голосов.
    public void SetVoteCount(int count)
    {
        voteCountText.text = count.ToString();
    }
}
