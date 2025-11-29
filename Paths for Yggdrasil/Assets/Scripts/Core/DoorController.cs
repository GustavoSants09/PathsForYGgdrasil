using UnityEngine;
using Photon.Pun;
using System.Collections;

/// <summary>
/// Controla portas que podem ser abertas/fechadas
/// Sincronizado pela rede
/// </summary>
public class DoorController : MonoBehaviourPun, IPunObservable
{
    [Header("Configurações da Porta")]
    [Tooltip("ID único desta porta")]
    [SerializeField] private string doorID;

    [Tooltip("Posição quando fechada")]
    [SerializeField] private Vector3 closedPosition;

    [Tooltip("Posição quando aberta")]
    [SerializeField] private Vector3 openPosition;

    [Tooltip("Velocidade de abertura/fechamento")]
    [SerializeField] private float moveSpeed = 2f;

    [Tooltip("Som de abertura")]
    [SerializeField] private AudioClip openSound;

    [Tooltip("Som de fechamento")]
    [SerializeField] private AudioClip closeSound;

    // Componentes
    private AudioSource audioSource;
    private Transform doorTransform;

    // Estado da porta
    private bool isOpen = false;
    private bool isMoving = false;
    private Vector3 targetPosition;

    // Sincronização de rede
    private bool networkIsOpen = false;

    /// <summary>
    /// Inicialização
    /// </summary>
    void Awake()
    {
        doorTransform = transform;
        audioSource = GetComponent<AudioSource>();

        // Se não tem AudioSource, adiciona um
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Configura posição inicial
        closedPosition = doorTransform.localPosition;
        targetPosition = closedPosition;
    }

    /// <summary>
    /// Atualização a cada frame
    /// </summary>
    void Update()
    {
        // Sincroniza estado da porta com valor da rede (para clientes remotos)
        if (!photonView.IsMine && networkIsOpen != isOpen)
        {
            if (networkIsOpen)
            {
                OpenDoorLocal();
            }
            else
            {
                CloseDoorLocal();
            }
        }

        // Move a porta suavemente
        if (isMoving)
        {
            MoveDoor();
        }
    }

    /// <summary>
    /// Move a porta em direção à posição alvo
    /// </summary>
    private void MoveDoor()
    {
        doorTransform.localPosition = Vector3.MoveTowards(
            doorTransform.localPosition,
            targetPosition,
            moveSpeed * Time.deltaTime
        );

        // Para de mover quando chegar ao destino
        if (Vector3.Distance(doorTransform.localPosition, targetPosition) < 0.01f)
        {
            doorTransform.localPosition = targetPosition;
            isMoving = false;
        }
    }

    /// <summary>
    /// Abre a porta (chamado externamente, ex: por botão)
    /// </summary>
    public void OpenDoor()
    {
        if (isOpen) return;

        photonView.RPC("RPC_OpenDoor", RpcTarget.All);
    }

    /// <summary>
    /// Fecha a porta
    /// </summary>
    public void CloseDoor()
    {
        if (!isOpen) return;

        photonView.RPC("RPC_CloseDoor", RpcTarget.All);
    }

    /// <summary>
    /// Alterna estado da porta
    /// </summary>
    public void ToggleDoor()
    {
        if (isOpen)
        {
            CloseDoor();
        }
        else
        {
            OpenDoor();
        }
    }

    /// <summary>
    /// RPC para abrir porta em todos os clientes
    /// </summary>
    [PunRPC]
    private void RPC_OpenDoor()
    {
        OpenDoorLocal();
    }

    /// <summary>
    /// RPC para fechar porta em todos os clientes
    /// </summary>
    [PunRPC]
    private void RPC_CloseDoor()
    {
        CloseDoorLocal();
    }

    /// <summary>
    /// Lógica local de abertura
    /// </summary>
    private void OpenDoorLocal()
    {
        isOpen = true;
        targetPosition = openPosition;
        isMoving = true;

        // Toca som de abertura
        if (openSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(openSound);
        }
    }

    /// <summary>
    /// Lógica local de fechamento
    /// </summary>
    private void CloseDoorLocal()
    {
        isOpen = false;
        targetPosition = closedPosition;
        isMoving = true;

        // Toca som de fechamento
        if (closeSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(closeSound);
        }
    }

    /// <summary>
    /// Sincronização do Photon
    /// </summary>
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Envia estado da porta
            stream.SendNext(isOpen);
        }
        else
        {
            // Recebe estado da porta
            networkIsOpen = (bool)stream.ReceiveNext();
        }
    }

    /// <summary>
    /// Retorna ID da porta
    /// </summary>
    public string GetDoorID()
    {
        return doorID;
    }
}