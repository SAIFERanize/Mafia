using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TimerUI : MonoBehaviour
{
    [Header("Links")]
    public PhaseManager phaseManager; 
    public TMP_Text timerText;            
    
    [Header("Background")]
    public Image backgroundImage;  // Ссылка на Image, который хотим перекрашивать
    public Color dayColor = Color.white;
    public Color nightColor = Color.black;

    void Update()
    {
        UpdateTimerText();
        UpdateBackgroundColor();
    }

    void UpdateTimerText()
    {
        if (phaseManager == null || timerText == null)
            return;

        GamePhase currentPhase = phaseManager.CurrentPhase;
        float timeLeft = phaseManager.CurrentTime;

        int secondsLeft = Mathf.CeilToInt(timeLeft);

        string phaseName = "";
        switch (currentPhase)
        {
            case GamePhase.NightDiscussion:  phaseName = "Ночное обсуждение";    break;
            case GamePhase.NightVoting:      phaseName = "Ночное голосование";   break;
            case GamePhase.DayDiscussion:    phaseName = "Дневное обсуждение";    break;
            case GamePhase.DayVoting:        phaseName = "Дневное голосование";   break;
        }

        timerText.text = $"{phaseName}: {secondsLeft}с";

        // Меняем цвет текста в последние 5 секунд
        if (secondsLeft <= 5)
            timerText.color = Color.red;
        else
            timerText.color = Color.white;
    }

    void UpdateBackgroundColor()
    {
        if (phaseManager == null || backgroundImage == null)
            return;

        // Проверяем, дневная ли фаза
        bool isDay = phaseManager.CurrentPhase == GamePhase.DayDiscussion 
                  || phaseManager.CurrentPhase == GamePhase.DayVoting;

        // Если день – ставим dayColor, если ночь – nightColor
        backgroundImage.color = isDay ? dayColor : nightColor;
    }
}
