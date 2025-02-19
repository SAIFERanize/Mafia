using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public enum GamePhase
{
    NightDiscussion,   // Ночное обсуждение
    NightVoting,       // Ночное голосование
    DayDiscussion,     // Дневное обсуждение
    DayVoting          // Дневное голосование
}

public class PhaseManager : MonoBehaviour
{
    [Header("Дефолтные значения длительности фаз (секунды)")]
    public float defaultNightDiscussionTime = 30f;
    public float defaultNightVotingTime = 20f;
    public float defaultDayDiscussionTime = 30f;
    public float defaultDayVotingTime = 20f;

    [Header("Фактические длительности фаз (загружаются из настроек комнаты)")]
    public float nightDiscussionTime;
    public float nightVotingTime;
    public float dayDiscussionTime;

    public float dayVotingTime;

    public GamePhase CurrentPhase { get; private set; }
    public float CurrentTime { get; private set; }

    private Text timerText; 
    public VotingManager votingManager; 
    
    public CommissarRoleController commissarRoleController;

    void Start()
    {
        // Если мы в комнате, получаем настройки из CustomRoomProperties
        if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.CustomProperties != null)
        {
            var props = PhotonNetwork.CurrentRoom.CustomProperties;
            nightDiscussionTime = props.ContainsKey("nightDiscussionTime") ? (float)props["nightDiscussionTime"] : defaultNightDiscussionTime;
            nightVotingTime = props.ContainsKey("nightVotingTime") ? (float)props["nightVotingTime"] : defaultNightVotingTime;
            dayDiscussionTime = props.ContainsKey("dayDiscussionTime") ? (float)props["dayDiscussionTime"] : defaultDayDiscussionTime;
            dayVotingTime = props.ContainsKey("dayVotingTime") ? (float)props["dayVotingTime"] : defaultDayVotingTime;
        }
        else
        {
            // Если настроек нет, используем дефолтные значения
            nightDiscussionTime = defaultNightDiscussionTime;
            nightVotingTime = defaultNightVotingTime;
            dayDiscussionTime = defaultDayDiscussionTime;
            dayVotingTime = defaultDayVotingTime;
        }

        // Начинаем с ночного обсуждения
        SetPhase(GamePhase.NightDiscussion);
    }

    void Update()
    {
        CurrentTime -= Time.deltaTime;
        if (CurrentTime <= 0f)
        {
            AdvancePhase();
        }
        UpdateTimerUI();
    }

    // Устанавливаем фазу и задаём соответствующее время
    public void SetPhase(GamePhase newPhase)
    {
        CurrentPhase = newPhase;
        switch (newPhase)
        {
            case GamePhase.NightDiscussion:
                CurrentTime = nightDiscussionTime;
                break;
            case GamePhase.NightVoting:
                CurrentTime = nightVotingTime;
                break;
            case GamePhase.DayDiscussion:
                CurrentTime = dayDiscussionTime;
                break;
            case GamePhase.DayVoting:
                CurrentTime = dayVotingTime;
                break;
        }
    }

    // Переход к следующей фазе по кругу
     public void AdvancePhase()
    {
        switch (CurrentPhase)
        {
            case GamePhase.NightDiscussion:
                SetPhase(GamePhase.NightVoting);
                ShowCommissarPanelIfNeeded();
                if (commissarRoleController != null)
                {
                commissarRoleController.ResetCheck();
                }
                votingManager?.ShowVotingPanel();
                break;

            case GamePhase.NightVoting:
                votingManager?.EndVoting();
                SetPhase(GamePhase.DayDiscussion);
                break;

            case GamePhase.DayDiscussion:
                SetPhase(GamePhase.DayVoting);
                votingManager?.ShowVotingPanel();
                break;

            case GamePhase.DayVoting:
                votingManager?.EndVoting();
                SetPhase(GamePhase.NightDiscussion);
                break;
        }
    }

    // Обновление UI таймера (изменение текста и цвета в последние 5 секунд)
    void UpdateTimerUI()
    {
        if (timerText != null)
        {
            int secondsLeft = Mathf.CeilToInt(CurrentTime);
            string phaseName = "";
            switch (CurrentPhase)
            {
                case GamePhase.NightDiscussion:
                    phaseName = "Ночное обсуждение";
                    break;
                case GamePhase.NightVoting:
                    phaseName = "Ночное голосование";
                    break;
                case GamePhase.DayDiscussion:
                    phaseName = "Дневное обсуждение";
                    break;
                case GamePhase.DayVoting:
                    phaseName = "Дневное голосование";
                    break;
            }
            timerText.text = $"{phaseName}: {secondsLeft}с";
            timerText.color = secondsLeft <= 5 ? Color.red : Color.white;
        }
    }
       public void ShowCommissarPanelIfNeeded()
{
    // Проверяем, есть ли в сцене CommissarRoleController
    CommissarRoleController commissar = FindFirstObjectByType<CommissarRoleController>();
    if (commissar == null) return;

    // Проверяем, что локальный игрок – комиссар
    if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("role", out object roleObj))
    {
        if (roleObj.ToString().ToLower() == "commissar")
        {
            // Показываем панель проверки
            commissar.ShowCommissionerPanel();
        }
    }
}

}
    