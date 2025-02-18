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
    public Transform panelContent;         // Контейнер, куда добавляются элементы (например, с Vertical Layout Group)

    private bool hasChecked = false;

    private void Start()
    {
        // Если локальный игрок не комиссар, отключаем этот контроллер и прячем панель
        if (!IsLocalCommissioner())
        {
            enabled = false;
            if (votingPanel) votingPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Проверяем, является ли локальный игрок комиссаром.
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
    /// Показывает комиссарскую панель проверки. Вызывается, например, в ночной фазе голосования.
    /// </summary>
    public void ShowCommissionerPanel()
    {
        if (hasChecked) return;

        if (votingPanel != null)
            votingPanel.SetActive(true);

        // Очищаем предыдущие элементы в контейнере
        foreach (Transform child in panelContent)
        {
            Destroy(child.gameObject);
        }

        // Для каждого живого игрока (кроме комиссара) создаём элемент проверки
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.NickName == PhotonNetwork.LocalPlayer.NickName)
                continue; // пропускаем самого комиссара

            bool isDead = player.CustomProperties.ContainsKey("isDead") && (bool)player.CustomProperties["isDead"];
            if (!isDead)
            {
                GameObject entryObj = Instantiate(votingEntryPrefab, panelContent);
                VotingEntry entry = entryObj.GetComponent<VotingEntry>();
                if (entry != null)
                {
                    // Используем специальный метод настройки для комиссара
                    entry.SetupForCommission(player.NickName, isDead, OnCommissionerCheck);
                }
            }
        }
    }
    /// <summary>
    /// Callback-функция для обработки выбора игрока комиссаром.
    /// </summary>
    /// <param name="targetPlayerName">Имя выбранного игрока.</param>
    private void OnCommissionerCheck(string targetPlayerName)
    {
        if (hasChecked) return;
        hasChecked = true;

        // Закрываем панель после выбора
        if (votingPanel != null)
            votingPanel.SetActive(false);

        // Вызываем RPC, чтобы объявить роль выбранного игрока
        photonView.RPC("RPC_RevealRole", RpcTarget.All, targetPlayerName, PhotonNetwork.NickName);
    }

    [PunRPC]
    private void RPC_RevealRole(string targetPlayerName, string commissarName, PhotonMessageInfo info)
    {
        // Определяем роль выбранного игрока
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

        // Формируем сообщение для общего чата
        string message = $"Оглашение комиссара: игрок {targetPlayerName} — {foundRole}";

        // Отправляем сообщение через GameChat (предполагается, что такой скрипт уже есть)
       GameChat chat = FindFirstObjectByType<GameChat>();
        if (chat != null)
        {
            chat.photonView.RPC("RPC_AddMessage", RpcTarget.All, message, " ");
        }
    }
    public void ResetCheck()
    {
      hasChecked = false;
    }

}
