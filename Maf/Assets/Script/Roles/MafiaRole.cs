using UnityEngine;
using Photon.Pun;

public class MafiaRole : MonoBehaviourPun
{
    // Метод для отправки ночного сообщения, видимого только мафии
    public void SendMafiaMessage(string message)
    {
        // Отправляем RPC, который будет обрабатываться только мафией
        photonView.RPC("RPC_AddMafiaMessage", RpcTarget.All, PhotonNetwork.NickName, message);
    }

    [PunRPC]
    public void RPC_AddMafiaMessage(string sender, string message, PhotonMessageInfo info)
    {
        // Проверяем роль локального игрока
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("role", out object role) && 
            role.ToString().ToLower() == "mafia")
        {
            // Здесь можно реализовать вывод в отдельный чат для мафии или иной UI-отчет
            Debug.Log($"[Mafia Chat] {sender}: {message}");
        }
    }
}
