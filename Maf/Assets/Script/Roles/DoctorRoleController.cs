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

    [Header("Ссылка на PhaseManager")]
    public PhaseManager phaseManager;
    [Header("Кнопка самохил")]
    public Button selfHealButton; // назначь в инспекторе
    private int remainingSelfHeals = 1;

    private bool hasHealed = false;

    private int maxSelfHeals = 1;

    private void Start()
    {
        remainingSelfHeals = maxSelfHeals;

        if (phaseManager == null)
        {
            phaseManager = FindAnyObjectByType<PhaseManager>();
        }

        if (!IsLocalDoctor() || IsLocalPlayerDead())
        {
            enabled = false;
            if (votingPanel) votingPanel.SetActive(false);
        }
        if (selfHealButton != null)
        {
            selfHealButton.onClick.AddListener(UseSelfHeal);
            selfHealButton.gameObject.SetActive(false);
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
        if (selfHealButton != null)
        {
            bool canUseSelfHeal = remainingSelfHeals > 0 && !hasHealed && !IsLocalPlayerDead();
            selfHealButton.gameObject.SetActive(true);
            selfHealButton.interactable = canUseSelfHeal;
        }

    }

    private void OnDoctorHeal(string targetPlayerName)
    {
        if (hasHealed) return;
        hasHealed = true;

        if (votingPanel != null)
            votingPanel.SetActive(false);

        // Если доктор пытается лечить сам себя:
        if (targetPlayerName == PhotonNetwork.LocalPlayer.NickName)
        {
            if (remainingSelfHeals > 0)
            {
                remainingSelfHeals--;
                Debug.Log("Доктор использовал самохил. Осталось: " + remainingSelfHeals);
            }
            else
            {
                Debug.Log("Самохил недоступен. Осталось попыток: 0");
                return;
            }
        }

        photonView.RPC("RPC_HealPlayer", RpcTarget.MasterClient, targetPlayerName); // лечение обрабатывает MasterClient
    }
    private void UseSelfHeal()
    {
        if (hasHealed || remainingSelfHeals <= 0 || IsLocalPlayerDead()) return;

        hasHealed = true;
        remainingSelfHeals--;

        if (votingPanel != null)
            votingPanel.SetActive(false);

        photonView.RPC("RPC_HealPlayer", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.NickName);
        Debug.Log("[Doctor] Самохил использован. Осталось: " + remainingSelfHeals);

        if (selfHealButton != null)
            selfHealButton.interactable = false;
    }


    [PunRPC]
    private void RPC_HealPlayer(string targetPlayerName, PhotonMessageInfo info)
    {
        Debug.Log($"Доктор пытается лечить игрока: {targetPlayerName}");

        PhaseManager manager = FindAnyObjectByType<PhaseManager>();
        if (manager != null)
        {
            manager.SetDoctorTarget(targetPlayerName); // добавь этот метод в PhaseManager
        }
    }

    public void SelectTargetToHeal(string targetName)
    {
        if (hasHealed) return;

        hasHealed = true;

        if (targetName == PhotonNetwork.LocalPlayer.NickName)
        {
            if (remainingSelfHeals > 0)
            {
                remainingSelfHeals--;
                Debug.Log("Доктор использовал самохил. Осталось: " + remainingSelfHeals);
            }
            else
            {
                Debug.Log("Нет попыток самохилиться.");
                return;
            }
        }

        photonView.RPC("RPC_HealPlayer", RpcTarget.MasterClient, targetName);
    }

    public bool CanSelfHeal()
    {
        return remainingSelfHeals > 0 && !hasHealed;
    }
    public void ResetHeal()
    {
        hasHealed = false;
    }

    public void ResetSelfHealCooldown()
    {
        remainingSelfHeals = maxSelfHeals;
    }

    public void HideDoctorPanel()
    {
        if (votingPanel != null)
            votingPanel.SetActive(false);
    }
}
