using UnityEngine;
using Photon.Pun;

public class StatuePlacement : MonoBehaviourPun
{
    public string statueColor; // Ex: "Blue"
    private bool isLocked = false;

    public void LockStatue(Vector3 lockPosition)
    {
        if (isLocked) return;
        isLocked = true;

        // Sincroniza para todos os jogadores que a estátua foi travada
        photonView.RPC("LockStatueRPC", RpcTarget.AllBuffered, lockPosition);
    }

    [PunRPC]
    void LockStatueRPC(Vector3 lockPosition)
    {
        // Posiciona exatamente na base
        transform.position = lockPosition;

        // Desativa física
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        // Impede que o jogador pegue de novo
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }

    public bool IsLocked()
    {
        return isLocked;
    }
}
