using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class DoctorUI : MonoBehaviourPun
{
    public GameObject panel;
    public Button confirmButton;
    public Transform playerListContainer;
    public GameObject playerButtonPrefab;

    private DoctorRoleController doctor;

    private Player selectedPlayer;

    void Start()
    {
        doctor = FindObjectOfType<DoctorRoleController>();
        panel.SetActive(false);
        confirmButton.onClick.AddListener(ConfirmHeal);
    }

    public void ShowDoctorPanel()
    {
        panel.SetActive(true);

        foreach (Transform child in playerListContainer)
            Destroy(child.gameObject);

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            GameObject buttonObj = Instantiate(playerButtonPrefab, playerListContainer);
            buttonObj.GetComponentInChildren<Text>().text = player.NickName;

            Button btn = buttonObj.GetComponent<Button>();
            btn.onClick.AddListener(() => SelectPlayer(player));
        }
    }

    private void SelectPlayer(Player player)
    {
        selectedPlayer = player;
        Debug.Log($"[DoctorUI] Вы выбрали {player.NickName}");
    }

    private void ConfirmHeal()
    {
        if (selectedPlayer == null) return;

        doctor.SelectTargetToHeal(selectedPlayer);
        panel.SetActive(false);
    }
}
