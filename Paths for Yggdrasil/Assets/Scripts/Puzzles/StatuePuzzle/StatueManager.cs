using UnityEngine;
using Photon.Pun;

public class StatueManager : MonoBehaviourPun
{
    public StatuePlacement[] statues;
    public FinalDoor finalDoorScript; // arraste o componente FinalDoor no Inspector

    private bool puzzleResolved = false;

    void Update()
    {
        if (puzzleResolved) return;

        bool allLocked = AllStatuesLocked();
        Debug.Log("🔍 Verificando estátuas travadas: " + allLocked);

        if (allLocked)
        {
            puzzleResolved = true;
            Debug.Log("✅ Todas as estátuas estão travadas! Chamando RPC...");
            photonView.RPC("ActivateDoorScript", RpcTarget.AllBuffered);
        }
    }



    bool AllStatuesLocked()
    {
        foreach (var statue in statues)
        {
            if (!statue.IsLocked())
                return false;
        }
        return true;
    }

    [PunRPC]
    void ActivateDoorScript()
    {
        if (finalDoorScript != null)
        {
            finalDoorScript.canInteract = true;
            Debug.Log("🔓 Script FinalDoor ativado — Porta agora pode ser interagida.");
        }
        else
        {
            Debug.LogError("❌ Campo 'finalDoorScript' não foi atribuído no Inspector.");
        }
    }
}
