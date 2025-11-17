using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PhotonView))]
public class BranchObject : MonoBehaviourPun, IPunObservable
{
    [Header("Physics")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Collider col;
    [SerializeField] private float mass = 5f;

    [Header("Carry Settings")]
    [SerializeField] private Vector3 carryOffset = new Vector3(0, -0.5f, 1f);
    [SerializeField] private float carryDistance = 2f;

    private Transform carriedByPlayer;
    private bool isBeingCarried = false;

    // Network sync
    private Vector3 networkPosition;
    private Quaternion networkRotation;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (col == null) col = GetComponent<Collider>();

        rb.mass = mass;
        rb.useGravity = true;
        rb.isKinematic = false;
    }

    private void Update()
    {
        if (isBeingCarried && carriedByPlayer != null)
        {
            // Segue o jogador que está carregando
            Vector3 targetPosition = carriedByPlayer.position + carriedByPlayer.TransformDirection(carryOffset);
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 10f);
            transform.rotation = Quaternion.Lerp(transform.rotation, carriedByPlayer.rotation, Time.deltaTime * 5f);
        }
        else if (!photonView.IsMine)
        {
            // Sincroniza posição para clientes remotos
            transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * 10f);
            transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation, Time.deltaTime * 5f);
        }
    }

    public void Pickup(Transform player)
    {
        if (isBeingCarried) return;

        // Solicita ownership do objeto
        photonView.RequestOwnership();
        photonView.RPC("RPC_Pickup", RpcTarget.AllBuffered, player.GetComponent<PhotonView>().ViewID);
    }

    [PunRPC]
    private void RPC_Pickup(int playerViewID)
    {
        PhotonView playerView = PhotonView.Find(playerViewID);
        if (playerView == null) return;

        carriedByPlayer = playerView.transform;
        isBeingCarried = true;

        // Desabilita física
        rb.isKinematic = true;
        rb.useGravity = false;
        col.enabled = false;

        Debug.Log($"[Branch] Picked up by {playerView.Owner.NickName}");
        GameEvents.OnBranchPickedUp?.Invoke();
    }

    public void Drop()
    {
        if (!isBeingCarried) return;

        photonView.RPC("RPC_Drop", RpcTarget.AllBuffered);
    }

    [PunRPC]
    private void RPC_Drop()
    {
        carriedByPlayer = null;
        isBeingCarried = false;

        // Reabilita física
        rb.isKinematic = false;
        rb.useGravity = true;
        col.enabled = true;

        Debug.Log("[Branch] Dropped");
        GameEvents.OnBranchDropped?.Invoke();
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(isBeingCarried);
        }
        else
        {
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
            isBeingCarried = (bool)stream.ReceiveNext();
        }
    }
}