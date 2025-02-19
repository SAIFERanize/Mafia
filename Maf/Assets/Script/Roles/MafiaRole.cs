using UnityEngine;
using Photon.Pun;

public class MafiaRole : MonoBehaviourPun
{
     public PhaseManager phaseManager;  
    // Метод для отправки ночного сообщения, видимого только мафии
    public void SendMafiaMessage(string message)
{
    // Проверяем, что игра в ночной фазе и игрок - мафия
    if (phaseManager.CurrentPhase == GamePhase.NightDiscussion &&
        PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("role", out object role) &&
        role.ToString().ToLower() == "mafia")
    {
        // Отправляем сообщение от мафии всем игрокам, но только мафия его увидит
        photonView.RPC("RPC_AddMafiaMessage", RpcTarget.All, PhotonNetwork.NickName, message);
    }
}


    [PunRPC]
    public void RPC_AddMafiaMessage(string sender, string message, PhotonMessageInfo info)
    {
        // Если у локального игрока нет роли или роль не "mafia", то выходим
        if (!PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("role", out object role) ||
            role.ToString().ToLower() != "mafia")
        {
            return;
        }

        // Только для мафии – выводим сообщение в UI или лог
        Debug.Log($"[Mafia Chat] {sender}: {message}");
        // Здесь можно добавить логику для отображения сообщения в специальном чате для мафии
    }
}
