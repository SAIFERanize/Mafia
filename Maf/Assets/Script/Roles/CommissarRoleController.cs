using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using TMPro;

public class CommissarRoleController : MonoBehaviourPun
{
    [Header("UI References")]
    public GameObject votingPanel;         // Панель, которую переиспользуем для проверки
    public GameObject votingEntryPrefab;   // Префаб элемента (с кнопкой и текстом)
    public Transform panelContent;         // Контейнер для элементов

    [Header("Ссылка на PhaseManager")]
    public PhaseManager phaseManager;      // Задайте через инспектор или найдите в Start()

    private bool hasChecked = false;

    private void Start()
    {
        // Если PhaseManager не задан через инспектор – ищем его
        if (phaseManager == null)
        {
            phaseManager = FindAnyObjectByType<PhaseManager>();
        }

        // Если локальный игрок не комиссар или уже мёртв – отключаем контроллер и прячем панель
        if (!IsLocalCommissioner() || IsLocalPlayerDead())
        {
            enabled = false;
            if (votingPanel) votingPanel.SetActive(false);
        }
    }

    private void Update()
    {
        // Если текущая фаза не NightVoting, скрываем комиссарскую панель (на случай, если проверка не была выполнена)
        if (phaseManager != null && phaseManager.CurrentPhase != GamePhase.NightVoting)
        {
            if (votingPanel != null && votingPanel.activeSelf)
                votingPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Проверяет, мёртв ли локальный игрок.
    /// </summary>
    private bool IsLocalPlayerDead()
    {
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("isDead", out object isDeadObj))
        {
            return (bool)isDeadObj;
        }
        return false;
    }

    /// <summary>
    /// Проверяет, является ли локальный игрок комиссаром.
    /// </summary>
    private bool IsLocalCommissioner()
    {
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("role", out object roleObj))
        {
            return roleObj.ToString().ToLower() == "commissar";
        }
        return false;
    }

    /// <summary>
    /// Показывает комиссарскую панель проверки (если комиссар жив и ещё не проверял).
    /// Вызывается, например, в ночной фазе голосования.
    /// </summary>
    public void ShowCommissionerPanel()
    {
        // Если комиссар уже проверял или мёртв, не показываем панель
        if (hasChecked || IsLocalPlayerDead()) return;

        if (votingPanel != null)
            votingPanel.SetActive(true);

        // Очищаем предыдущие элементы
        foreach (Transform child in panelContent)
        {
            Destroy(child.gameObject);
        }

        // Создаем элементы для каждого живого игрока (кроме комиссара)
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.NickName == PhotonNetwork.LocalPlayer.NickName)
                continue; // пропускаем комиссара

            bool isDead = player.CustomProperties.ContainsKey("isDead") && (bool)player.CustomProperties["isDead"];
            if (!isDead)
            {
                GameObject entryObj = Instantiate(votingEntryPrefab, panelContent);
                VotingEntry entry = entryObj.GetComponent<VotingEntry>();
                if (entry != null)
                {
                    entry.SetupForCommission(player.NickName, isDead, OnCommissionerCheck);
                }
            }
        }
    }

    /// <summary>
    /// Callback-функция для обработки выбора комиссаром.
    /// </summary>
    /// <param name="targetPlayerName">Имя выбранного игрока.</param>
    private void OnCommissionerCheck(string targetPlayerName)
    {
        if (hasChecked) return;
        hasChecked = true;

        // Скрываем панель после выбора
        if (votingPanel != null)
            votingPanel.SetActive(false);

        // Вызываем RPC, чтобы объявить роль выбранного игрока на всех клиентах
        photonView.RPC("RPC_RevealRole", RpcTarget.All, targetPlayerName, PhotonNetwork.NickName);
    }

    [PunRPC]
    private void RPC_RevealRole(string targetPlayerName, string commissarName, PhotonMessageInfo info)
    {
        string foundRole = "неизвестна";
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.NickName == targetPlayerName)
            {
                if (p.CustomProperties.TryGetValue("role", out object roleObj))
                    foundRole = roleObj.ToString().ToLower();
                break;
            }
        }

        string message = $"Оглашение комиссара: игрок {targetPlayerName} — {foundRole}";

        GameChat chat = FindAnyObjectByType<GameChat>();
        if (chat != null)
        {
            chat.photonView.RPC("RPC_AddMessage", RpcTarget.All, message, " ");
        }
    }

    public void ResetCheck()
    {
        hasChecked = false;
    }
    
    // Дополнительный метод, который можно вызвать извне для скрытия панели
    public void HideCommissionerPanel()
    {
        if (votingPanel != null)
            votingPanel.SetActive(false);
    }
}
