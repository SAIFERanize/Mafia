using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;

public class RoomListManager : MonoBehaviourPunCallbacks
{
    public Transform contentPanel; // Панель, в которую будем добавлять префабы комнат
    public GameObject roomItemPrefab; // Префаб комнаты
    public TMP_Text noRoomsText; // Текст "Нет доступных комнат"
    public Button backButton; // Кнопка "Назад"

    private void Start()
    {
        backButton.onClick.AddListener(() => PhotonNetwork.LoadLevel("MainMenu"));
        
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
        
        PhotonNetwork.JoinLobby(); // Входим в лобби, чтобы получить список комнат
    }

public override void OnRoomListUpdate(List<RoomInfo> roomList)
{
    if (contentPanel == null || roomItemPrefab == null || noRoomsText == null)
    {
        Debug.LogError("❌ Ошибка: Не все ссылки в RoomListManager установлены в инспекторе!");
        return;
    }

    // Очистка списка
    foreach (Transform child in contentPanel)
    {
        Destroy(child.gameObject);
    }

    // Показываем или скрываем текст "Нет комнат"
    noRoomsText.gameObject.SetActive(roomList.Count == 0);

   foreach (RoomInfo room in roomList)
{
    if (!room.RemovedFromList)
    {
        GameObject roomItem = Instantiate(roomItemPrefab, contentPanel);
        roomItem.transform.SetParent(contentPanel, false);

        // Исправляем позицию
        RectTransform rectTransform = roomItem.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.localScale = Vector3.one;

        TMP_Text roomNameText = roomItem.GetComponentInChildren<TMP_Text>();
        Button joinButton = roomItem.GetComponentInChildren<Button>();

        if (roomNameText != null)
        {
            roomNameText.text = $"{room.Name} ({room.PlayerCount}/{room.MaxPlayers})";
        }
        else
        {
            Debug.LogError("❌ RoomItemPrefab не содержит RoomNameText!");
        }

        if (joinButton != null)
        {
            joinButton.onClick.AddListener(() => PhotonNetwork.JoinRoom(room.Name));
        }
        else
        {
            Debug.LogError("❌ RoomItemPrefab не содержит JoinButton!");
        }
    }
}

// Обновляем Scroll View
Canvas.ForceUpdateCanvases();
        }

    public override void OnJoinedRoom()
    {
        PhotonNetwork.LoadLevel("LobbyScene"); // Переход в комнату
    }
}
