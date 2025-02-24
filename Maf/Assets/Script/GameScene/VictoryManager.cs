using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class VictoryManager : MonoBehaviourPun
{
    [Header("Ссылки на менеджеры и UI")]
    public PhaseManager phaseManager;      // Для остановки таймера и смены фазы
    public VictoryPanel victoryPanel;      // Для отображения окна победы

    // Флаг, чтобы победа фиксировалась только один раз
    private bool victoryAssigned = false;

    // Новый метод Update для проверки победы в любой фазе (только на мастере)
    void Update()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        if (!victoryAssigned)
            CheckVictoryConditions();
    }

    // Метод проверки условий победы – можно оставить почти без изменений
    public void CheckVictoryConditions()
{
    if (victoryAssigned)
        return;

    // Если игра ещё в фазе предстарта, не проверяем победу
    if (phaseManager.CurrentPhase == GamePhase.GameStart)
        return;

    // Проверяем, что у всех игроков уже назначена роль
    foreach (Player player in PhotonNetwork.PlayerList)
    {
        if (!player.CustomProperties.ContainsKey("role"))
        {
            // Роли ещё не установлены – выходим из метода
            return;
        }
    }

    int mafiaAlive = 0;
    int civiliansAlive = 0;

    foreach (Player player in PhotonNetwork.PlayerList)
    {
        string role = "civilian";
        if (player.CustomProperties.TryGetValue("role", out object roleObj))
            role = roleObj.ToString().ToLower();

        bool isDead = false;
        if (player.CustomProperties.TryGetValue("isDead", out object isDeadObj))
            isDead = (bool)isDeadObj;

        if (!isDead)
        {
            if (role == "mafia")
                mafiaAlive++;
            else
                civiliansAlive++;
        }
    }

    if (mafiaAlive == 0)
    {
        victoryAssigned = true;
        photonView.RPC("RPC_ShowVictoryMessage", RpcTarget.All, "Победа мирных! Справедливость восторжествовала.");
    }
    else if (civiliansAlive == 0)
    {
        victoryAssigned = true;
        photonView.RPC("RPC_ShowVictoryMessage", RpcTarget.All, "Победа мафии! Темные силы захватили город.");
    }
}


    [PunRPC]
    public void RPC_ShowVictoryMessage(string victoryMessage)
    {
        if (victoryPanel != null)
            victoryPanel.ShowVictoryMessage(victoryMessage);

        // Останавливаем таймер и переключаем фазу на день
        if (phaseManager != null)
        {
            phaseManager.StopTimer(); // Останавливаем таймер
            phaseManager.SetPhase(GamePhase.DayDiscussion); // Переключаем в дневную фазу
        }
    }
}
