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

    // Callback-функция, вызываемая при нажатии на кнопку голосования
    private Action<string> onVoteButtonClicked;
    private string playerName;

    /// Инициализирует элемент голосования.
    /// <param name="name">Имя игрока</param>
    /// <param name="isDead">Если true – игрок мёртв</param>
    /// <param name="callback">Callback на нажатие</param>
  public void Setup(string name, bool isDead, Action<string> callback)
{
    playerName = name;
    playerNameText.text = name;
    voteCountText.text = "0";
    onVoteButtonClicked = callback;

    voteButton.onClick.RemoveAllListeners();
    if (isDead)
    {
        // Если игрок мёртв, отключаем кнопку (либо можно скрыть её) и окрашиваем имя в красный
        voteButton.interactable = false;
        // Для полного скрытия можно раскомментировать:
        // voteButton.gameObject.SetActive(false);
        playerNameText.color = Color.red;
    }
    else
    {
        voteButton.interactable = true;
        voteButton.onClick.AddListener(() => onVoteButtonClicked?.Invoke(playerName));
        playerNameText.color = Color.white;
    }
}
  // Специальный Setup для комиссара (для проверки игроков)
    public void SetupForCommission(string name, bool isDead, Action<string> callback)
    {
        playerName = name;
        playerNameText.text = name;
        onVoteButtonClicked = callback;

        voteButton.onClick.RemoveAllListeners();
        if (isDead)
        {
            voteButton.interactable = false;
            playerNameText.color = Color.red;
        }
        else
        {
            voteButton.interactable = true;
            // Изменяем текст кнопки на "Проверить"
            TMP_Text btnText = voteButton.GetComponentInChildren<TMP_Text>();
            if (btnText != null)
                btnText.text = "Проверить";
            voteButton.onClick.AddListener(() => onVoteButtonClicked?.Invoke(playerName));
            playerNameText.color = Color.white;
        }
    }
    /// Обновляет отображение количества голосов.
    public void SetVoteCount(int count)
    {
        voteCountText.text = count.ToString();
    }
}
