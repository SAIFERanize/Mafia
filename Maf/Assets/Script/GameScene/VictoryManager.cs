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

    // Метод проверки условий победы – вызывается мастером после каждого убийства
    public void CheckVictoryConditions()
{
    if (victoryAssigned)
        return;

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
        phaseManager.StopTimer();
        phaseManager.SetPhase(GamePhase.DayDiscussion);
    }
}

}
