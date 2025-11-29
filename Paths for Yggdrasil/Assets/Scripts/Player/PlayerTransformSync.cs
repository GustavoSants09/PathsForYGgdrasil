using UnityEngine;
using Photon.Pun;

/// <summary>
/// Sincroniza posição e rotação do jogador pela rede
/// Utiliza interpolação suave para movimento fluido
/// </summary>
public class PlayerTransformSync : MonoBehaviourPun, IPunObservable
{
    [Header("Configurações de Sincronização")]
    [Tooltip("Velocidade de interpolação da posição (maior = mais rápido)")]
    [SerializeField] private float positionLerpSpeed = 10f;

    [Tooltip("Velocidade de interpolação da rotação (maior = mais rápido)")]
    [SerializeField] private float rotationLerpSpeed = 10f;

    [Tooltip("Distância mínima para teletransportar ao invés de interpolar")]
    [SerializeField] private float teleportDistance = 5f;

    // Variáveis de rede - valores recebidos dos outros jogadores
    private Vector3 networkPosition;
    private Quaternion networkRotation;

    // Cache do transform
    private Transform playerTransform;

    /// <summary>
    /// Inicialização
    /// </summary>
    void Awake()
    {
        playerTransform = transform;

        // Inicializa com posição e rotação atuais
        networkPosition = playerTransform.position;
        networkRotation = playerTransform.rotation;
    }

    /// <summary>
    /// Atualização a cada frame
    /// </summary>
    void Update()
    {
        // Só interpola se NÃO for o jogador local
        if (photonView.IsMine == false)
        {
            InterpolateTransform();
        }
    }

    /// <summary>
    /// Interpola suavemente posição e rotação do jogador remoto
    /// </summary>
    private void InterpolateTransform()
    {
        // Calcula distância até a posição de rede
        float distance = Vector3.Distance(playerTransform.position, networkPosition);

        // Se a distância for muito grande, teletransporta (útil para casos de lag extremo)
        if (distance > teleportDistance)
        {
            playerTransform.position = networkPosition;
            playerTransform.rotation = networkRotation;
        }
        else
        {
            // Interpola posição suavemente
            playerTransform.position = Vector3.Lerp(
                playerTransform.position,
                networkPosition,
                Time.deltaTime * positionLerpSpeed
            );

            // Interpola rotação suavemente
            playerTransform.rotation = Quaternion.Lerp(
                playerTransform.rotation,
                networkRotation,
                Time.deltaTime * rotationLerpSpeed
            );
        }
    }

    /// <summary>
    /// Método chamado pelo Photon para sincronizar dados pela rede
    /// Chamado aproximadamente 10 vezes por segundo
    /// </summary>
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Somos o dono deste jogador: enviamos nossa posição e rotação
            stream.SendNext(playerTransform.position);
            stream.SendNext(playerTransform.rotation);
        }
        else
        {
            // Somos um jogador remoto: recebemos posição e rotação
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();

            // Estimativa de lag para melhor interpolação
            float lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));

            // Ajusta posição de rede baseado no lag estimado
            // (predição simples para compensar latência)
            networkPosition += (networkPosition - playerTransform.position) * lag;
        }
    }
}