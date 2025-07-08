using UnityEngine;
using TMPro;

public class InteractableDoorLogic : MonoBehaviour, Interectable
{
    public GameObject panelToShow; // 👈 Painel a ser ativado na interação

    public void Interact()
    {
        if (panelToShow != null)
        {
            panelToShow.SetActive(true);
            Debug.Log("🔓 Painel de porta ativado.");
        }
    }

    public string GetDescription()
    {
        return "Aperte [E] para abrir a porta.";
    }
}
