using UnityEngine;

public class FinalDoor : MonoBehaviour, Interectable
{
    public string message = "Você concluiu o puzzle! Parabéns!";
    public bool canInteract = false; // Controla se a porta pode ser usada

    public GameObject panelToShow; // 👈 Painel a ser ativado na interação
    public BoxCollider player;
    


    public void Interact()
    {
        if (!canInteract) return;

        Debug.Log(message);

        if (panelToShow != null)
        {
            panelToShow.SetActive(true);
            Debug.Log("🔓 Painel de porta ativado.");
            player.enabled = false;
            this.enabled = false;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    public string GetDescription()
    {
        return canInteract ? "[E] Interagir com a porta" : "";
    }
}
