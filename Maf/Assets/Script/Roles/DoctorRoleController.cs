using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;

public class DoctorRoleController : MonoBehaviourPun
{
    [Header("UI References")]
    public GameObject votingPanel;
    public GameObject votingEntryPrefab;
    public Transform panelContent;

    [Header("—сылка на PhaseManager")]
    public PhaseManager phaseManager;

    private bool hasHealed = false;

    private void Start()
    {
        if (phaseManager == null)
        {
            phaseManager = FindAnyObjectByType<PhaseManager>();
        }

        if (!IsLocalDoctor() || IsLocalPlayerDead())
        {
            enabled = false;
            if (votingPanel) votingPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (phaseManager != null && phaseManager.CurrentPhase != GamePhase.NightVoting)
        {
            if (votingPanel != null && votingPanel.activeSelf)
                votingPanel.SetActive(false);
        }
    }

    private bool IsLocalPlayerDead()
    {
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("isDead", out object isDeadObj))
        {
            return (bool)isDeadObj;
        }
        return false;
    }

    private bool IsLocalDoctor()
    {
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("role", out object roleObj))
        {
            return roleObj.ToString().ToLower() == "doctor";
        }
        return false;
    }

    public void ShowDoctorPanel()
    {
        if (hasHealed || IsLocalPlayerDead()) return;

        if (votingPanel != null)
            votingPanel.SetActive(true);

        foreach (Transform child in panelContent)
        {
            Destroy(child.gameObject);
        }

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            bool isDead = player.CustomProperties.ContainsKey("isDead") && (bool)player.CustomProperties["isDead"];
            if (!isDead)
            {
                GameObject entryObj = Instantiate(votingEntryPrefab, panelContent);
                VotingEntry entry = entryObj.GetComponent<VotingEntry>();
                if (entry != null)
                {
                    entry.SetupForDoctor(player.NickName, isDead, OnDoctorHeal);
                }
            }
        }
    }

    private void OnDoctorHeal(string targetPlayerName)
    {
        if (hasHealed) return;
        hasHealed = true;

        if (votingPanel != null)
            votingPanel.SetActive(false);

        photonView.RPC("RPC_HealPlayer", RpcTarget.MasterClient, targetPlayerName); // лечение обрабатывает MasterClient
    }

    [PunRPC]
    private void RPC_HealPlayer(string targetPlayerName, PhotonMessageInfo info)
    {
        // —охран€ем игрока, которого доктор лечит Ч это можно применить в PhaseManager при проверке убийства мафией.
        Debug.Log($"ƒоктор пытаетс€ лечить игрока: {targetPlayerName}");

        PhaseManager manager = FindAnyObjectByType<PhaseManager>();
        if (manager != null)
        {
            manager.SetDoctorTarget(targetPlayerName); // добавь этот метод в PhaseManager
        }
    }

    public void ResetHeal()
    {
        hasHealed = false;
    }

    public void HideDoctorPanel()
    {
        if (votingPanel != null)
            votingPanel.SetActive(false);
    }
}
