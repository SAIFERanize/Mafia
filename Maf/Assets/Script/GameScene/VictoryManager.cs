using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class VictoryManager : MonoBehaviourPun
{
    [Header("Ссылка на PhaseManager для остановки таймера")]
    public PhaseManager phaseManager; // Привяжите в инспекторе объект с PhaseManager

    // Метод для проверки условий победы.
    // Если все мафии убиты – побеждают мирные,
    // если все мирные (civilian и commissar) убиты – побеждает мафия.
    public void CheckVictoryConditions()
    {
        int mafiaAlive = 0;
        int civiliansAlive = 0;

        // Перебираем всех игроков в комнате.
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            // Получаем роль игрока. Если роль не задана, считаем его гражданским.
            string role = "civilian";
            if (player.CustomProperties.TryGetValue("role", out object roleObj))
            {
                role = roleObj.ToString().ToLower();
            }

            // Проверяем, мёртв ли игрок.
            // Если свойство "isDead" не задано – считаем, что игрок жив.
            bool isDead = false;
            if (player.CustomProperties.TryGetValue("isDead", out object isDeadObj))
            {
                isDead = (bool)isDeadObj;
            }

            // Если игрок живой – увеличиваем счётчик соответствующей стороны.
            if (!isDead)
            {
                if (role == "mafia")
                    mafiaAlive++;
                else
                    civiliansAlive++;
            }
        }

        // Если все мафии убиты, мирные побеждают.
        if (mafiaAlive == 0)
        {
            Debug.Log("[VictoryManager] Победа мирных! Все мафии убиты.");
            if (phaseManager != null)
                phaseManager.StopTimer();
        }
        // Если все гражданские (мирные) убиты, побеждает мафия.
        else if (civiliansAlive == 0)
        {
            Debug.Log("[VictoryManager] Победа мафии! Все мирные убиты.");
            if (phaseManager != null)
                phaseManager.StopTimer();
        }
    }
}
