using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class PlayerNetworkController : MonoBehaviourPun, IPunObservable
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioListener audioListener;

    [Header("Network Sync")]
    [SerializeField] private float positionLerpSpeed = 10f;
    [SerializeField] private float rotationLerpSpeed = 8f;

    private Vector3 networkPosition;
    private Quaternion networkRotation;

    public PlayerRole Role { get; private set; }
    public bool IsLocalPlayer => photonView.IsMine;

    private void Awake()
    {
        // Desabilita componentes para jogadores remotos
        if (!photonView.IsMine)
        {
            if (playerCamera != null) playerCamera.enabled = false;
            if (audioListener != null) audioListener.enabled = false;
        }
        else
        {
            // Atribui role baseado na ordem de entrada
            Role = PhotonNetwork.IsMasterClient ? PlayerRole.PlayerA : PlayerRole.PlayerB;
            GameEvents.OnRoleAssigned?.Invoke(Role);
        }
    }

    private void Update()
    {
        if (!photonView.IsMine)
        {
            // Interpola posição/rotação de jogadores remotos
            transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * positionLerpSpeed);
            transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation, Time.deltaTime * rotationLerpSpeed);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Envia dados para a rede
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else
        {
            // Recebe dados da rede
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
        }
    }
}