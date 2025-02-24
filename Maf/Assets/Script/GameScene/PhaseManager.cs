using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public enum GamePhase
{
    GameStart,          // Предфаза для загрузки
    NightDiscussion,    // Ночное обсуждение
    NightVoting,        // Ночное голосование
    DayDiscussion,      // Дневное обсуждение
    DayVoting           // Дневное голосование
}

public class PhaseManager : MonoBehaviour
{
    [Header("Дефолтные значения длительности фаз (секунды)")]
    public float defaultGameStartTime = 3f; 
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

    // Флаг для остановки таймера после победы
    private bool gameEnded = false;

    // --- Новые поля для организации первого цикла ---
    private bool firstCycle = true;
    private int firstCycleIndex = 0;
    // Порядок фаз в первом цикле:
    // 0: GameStart (предфаза)
    // 1: DayDiscussion — первый раз дневное обсуждение
    // 2: NightDiscussion
    // 3: NightVoting
    // 4: DayDiscussion — повторное дневное обсуждение
    // 5: DayVoting
    private readonly GamePhase[] firstCycleOrder = new GamePhase[]
    {
        GamePhase.GameStart,
        GamePhase.DayDiscussion,
        GamePhase.NightDiscussion,
        GamePhase.NightVoting,
        GamePhase.DayDiscussion,
        GamePhase.DayVoting
    };

    void Start()
    {
        // Получение настроек длительности фаз (если есть)
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
            nightDiscussionTime = defaultNightDiscussionTime;
            nightVotingTime = defaultNightVotingTime;
            dayDiscussionTime = defaultDayDiscussionTime;
            dayVotingTime = defaultDayVotingTime;
        }

        // Запускаем первую фазу — предфазу
        SetPhase(GamePhase.GameStart);
    }

    void Update()
    {
        if (gameEnded)
            return;

        CurrentTime -= Time.deltaTime;
        if (CurrentTime <= 0f)
        {
            AdvancePhase();
        }
        UpdateTimerUI();
    }

    // Метод для установки фазы и соответствующего времени
    public void SetPhase(GamePhase newPhase)
    {
        CurrentPhase = newPhase;
        switch (newPhase)
        {
            case GamePhase.GameStart:
                CurrentTime = defaultGameStartTime;
                break;
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

        // Скрываем комиссарскую панель, если фаза не ночное голосование
        if (newPhase != GamePhase.NightVoting && commissarRoleController != null)
        {
            commissarRoleController.HideCommissionerPanel();
        }
    }

    // Метод для перехода к следующей фазе
    public void AdvancePhase()
    {
        if (firstCycle)
        {
            firstCycleIndex++;
            if (firstCycleIndex < firstCycleOrder.Length)
            {
                GamePhase nextPhase = firstCycleOrder[firstCycleIndex];
                // Сохраняем предыдущую фазу для логики перехода
                GamePhase prevPhase = CurrentPhase;
                // Обновляем фазу
                SetPhase(nextPhase);
            
                // Логика для первого цикла в зависимости от перехода
                if (prevPhase == GamePhase.NightDiscussion && nextPhase == GamePhase.NightVoting)
                {
                ShowCommissarPanelIfNeeded();
                 if (commissarRoleController != null)
                 commissarRoleController.ResetCheck();
                 // Вызываем ShowVotingPanel на всех клиентах для инициализации словаря голосов.
                  // UI-панель будет показана только у мафии согласно внутренней проверке.
                    votingManager?.ShowVotingPanel();
                }

                else if (prevPhase == GamePhase.NightVoting && nextPhase == GamePhase.DayDiscussion)
                {
                    votingManager?.EndVoting();
                }
                else if (prevPhase == GamePhase.DayDiscussion && nextPhase == GamePhase.DayVoting)
                {
                    votingManager?.ShowVotingPanel();
                }
                else if (prevPhase == GamePhase.DayVoting)
                {
                    votingManager?.EndVoting();
                }
            
                if (firstCycleIndex == firstCycleOrder.Length - 1)
                {
                    firstCycle = false;
                }
            }
            else
            {
                // На всякий случай переходим в обычный цикл
                SetPhase(GamePhase.NightDiscussion);
                firstCycle = false;
            }
        }
        else
        {
            // Обычная схема фаз
            switch (CurrentPhase)
            {
                case GamePhase.NightDiscussion:
                    SetPhase(GamePhase.NightVoting);
                    if (commissarRoleController != null)
                        commissarRoleController.ResetCheck();
                    ShowCommissarPanelIfNeeded();
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
                    if (commissarRoleController != null)
                        commissarRoleController.ResetCheck();
                    break;
            }
        }
    }

    // Обновление UI таймера
    void UpdateTimerUI()
    {
        if (timerText != null)
        {
            int secondsLeft = Mathf.CeilToInt(CurrentTime);
            string phaseName = "";
            switch (CurrentPhase)
            {
                case GamePhase.GameStart:
                    phaseName = "Предфаза";
                    break;
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
        CommissarRoleController commissar = FindFirstObjectByType<CommissarRoleController>();
        if (commissar == null) return;
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("role", out object roleObj))
        {
            if (roleObj.ToString().ToLower() == "commissar")
            {
                commissar.ShowCommissionerPanel();
            }
        }
    }

    // Остановка таймера при достижении победных условий
    public void StopTimer()
    {
      gameEnded = true;

      if(timerText != null){
        timerText.text = "День";
        timerText.color = Color.white;
      }
    }
}
