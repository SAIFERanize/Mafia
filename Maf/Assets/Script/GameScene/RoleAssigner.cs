using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine;
using System.Collections.Generic;

public static class RoleAssigner
{
    public static void AssignRoles()
    {
        Player[] players = PhotonNetwork.PlayerList;

        int mafiaIndex = Random.Range(0, players.Length);
        int commissionerIndex = mafiaIndex;
        while (commissionerIndex == mafiaIndex)
        {
            commissionerIndex = Random.Range(0, players.Length);
        }

        int doctorIndex = mafiaIndex;
        while (doctorIndex == mafiaIndex || doctorIndex == commissionerIndex)
        {
            doctorIndex = Random.Range(0, players.Length);
        }

        for (int i = 0; i < players.Length; i++)
        {
            string role = "civilian";
            if (i == mafiaIndex) role = "mafia";
            else if (i == commissionerIndex) role = "commissar";
            else if (i == doctorIndex) role = "doctor";

            Hashtable props = new Hashtable
            {
                { "role", role },
                { "playerNumber", i },
                { "isDead", false }
            };

            players[i].SetCustomProperties(props);
            Debug.Log($"[RoleAssigner] {players[i].NickName} получил роль: {role}");
        }
    }
}
