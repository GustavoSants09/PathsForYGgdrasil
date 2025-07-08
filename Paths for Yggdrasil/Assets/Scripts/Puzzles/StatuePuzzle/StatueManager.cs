using UnityEngine;
using Photon.Pun;

public class StatueManager : MonoBehaviourPun
{
    public PickupObject[] statues;
    public FinalDoor finalDoorScript; // arraste o componente FinalDoor no Inspector

    private bool puzzleResolved = false;

    [System.Obsolete]
    void Start()
    {
        statues = FindObjectsOfType<PickupObject>();
    }

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
        bool allLocked = true;

        for (int i = 0; i < statues.Length; i++)
        {
            bool locked = statues[i].IsLocked();
            Debug.Log($"🔍 Estátua {i}: isLocked = {locked}");

            if (!locked)
                allLocked = false;
        }

        return allLocked;
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
