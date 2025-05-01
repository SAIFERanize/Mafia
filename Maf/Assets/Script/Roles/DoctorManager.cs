using Photon.Pun;
using UnityEngine;

public class DoctorManager : MonoBehaviourPun
{
    public void SelectTargetToHeal(int actorNumber)
    {
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("role", out object role) &&
            role.ToString().ToLower() == "doctor")
        {
            GameManager gm = FindAnyObjectByType<GameManager>();
            if (gm != null)
            {
                gm.doctorTargetActorNumber = actorNumber;
                Debug.Log("[DoctorManager] Доктор выбрал цель для лечения: " + actorNumber);
            }
        }
    }
}
