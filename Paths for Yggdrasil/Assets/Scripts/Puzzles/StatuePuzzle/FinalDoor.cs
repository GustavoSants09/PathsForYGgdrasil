using UnityEngine;

public class FinalDoor : MonoBehaviour, Interectable
{
    public string message = "Você concluiu o puzzle! Parabéns!";
    public bool canInteract = false; // Controla se a porta pode ser usada

    public void Interact()
    {
        if (!canInteract) return;

        Debug.Log(message);
        // Aqui você pode adicionar lógica para mostrar painel de UI ou finalizar a alpha
    }

    public string GetDescription()
    {
        return canInteract ? "[E] Interagir com a porta" : "";
    }
}
