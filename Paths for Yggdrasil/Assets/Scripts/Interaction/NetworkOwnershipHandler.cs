using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// Encapsula ownership do Photon. N�o devolve ownership para a cena.
/// Sempre transfere para quem pedir (takeover).
/// </summary>
[DisallowMultipleComponent]
public class NetworkOwnershipHandler : MonoBehaviourPun, IPunOwnershipCallbacks
{
    public System.Action<Player> OnOwnerChanged;

    void OnEnable() => PhotonNetwork.AddCallbackTarget(this);
    void OnDisable() => PhotonNetwork.RemoveCallbackTarget(this);

    /// <summary>Se n�o sou dono, pede ownership.</summary>
    public void RequestOwnership()
    {
        if (!photonView.IsMine)
            photonView.RequestOwnership();
    }

    /// <summary>Transfere explicitamente para um player.</summary>
    public void TransferTo(Player newOwner)
    {
        if (newOwner != null && photonView.Owner != newOwner)
            photonView.TransferOwnership(newOwner);
    }

    public bool IsMine => photonView.IsMine;
    public Player Owner => photonView.Owner;

    // Aceita qualquer pedido de ownership (takeover).
    public void OnOwnershipRequest(PhotonView targetView, Player requestingPlayer)
    {
        if (targetView == photonView)
            photonView.TransferOwnership(requestingPlayer);
    }

    public void OnOwnershipTransfered(PhotonView targetView, Player previousOwner)
    {
        if (targetView == photonView)
            OnOwnerChanged?.Invoke(photonView.Owner);
    }

    public void OnOwnershipTransferFailed(PhotonView targetView, Player senderOfFailedRequest)
    {
        if (targetView == photonView)
            Debug.LogWarning($"[Ownership] Falha ao transferir para {senderOfFailedRequest?.NickName}");
    }
}