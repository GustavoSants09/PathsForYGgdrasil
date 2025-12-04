using UnityEngine;
using Photon.Pun;
using QuantumHeist.Game;

/// <summary>
/// Sistema de cristal coletável que concede pontos e boost de velocidade
/// Sincronizado via Photon para multiplayer
/// </summary>
public class Crystal : MonoBehaviourPunCallbacks
{
    [Header("Crystal Settings")]
    [SerializeField] private int pointsValue = 10;
    [SerializeField] private float speedBoostMultiplier = 1.5f;
    [SerializeField] private float speedBoostDuration = 3f;
    [SerializeField] private float rotationSpeed = 50f;

    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem collectEffect;
    [SerializeField] private AudioClip collectSound;

    [Header("Bobbing Animation")]
    [SerializeField] private float bobbingHeight = 0.3f;
    [SerializeField] private float bobbingSpeed = 2f;

    private Vector3 startPosition;
    private bool isCollected = false;

    private void Start()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        if (isCollected) return;

        // Rotação visual do cristal
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

        // Animação de flutuação (bobbing)
        float newY = startPosition.y + Mathf.Sin(Time.time * bobbingSpeed) * bobbingHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Evita coleta duplicada
        if (isCollected)
            return;

        // Verifica se é um jogador
        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null)
            return;

        // ✅ CORREÇÃO CRÍTICA: Apenas o dono do PlayerController pode coletar
        PhotonView playerPhotonView = player.GetComponent<PhotonView>();
        if (playerPhotonView == null || !playerPhotonView.IsMine)
        {
            Debug.Log($"[Crystal] Ignorando colisão - não é o jogador local");
            return;
        }

        // Marca como coletado
        isCollected = true;

        Debug.Log($"[Crystal] 💎 Coletado por {player.photonView.Owner.NickName}!");

        // Chama coleta no jogador LOCAL
        player.CollectCrystal(pointsValue, speedBoostMultiplier, speedBoostDuration);

        // Efeitos visuais/sonoros
        PlayCollectEffects();

        // ✅ Destroi o cristal via RPC para sincronizar com todos
        if (PhotonNetwork.IsConnected)
        {
            photonView.RPC("RPC_DestroyCrystal", RpcTarget.AllBuffered);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// RPC que destroi o cristal em todos os clientes
    /// </summary>
    [PunRPC]
    private void RPC_DestroyCrystal()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Destroy(gameObject);
        }
        else
        {
            // Clientes apenas desabilitam visualmente
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Reproduz efeitos de coleta
    /// </summary>
    private void PlayCollectEffects()
    {
        // Partículas
        if (collectEffect != null)
        {
            ParticleSystem effect = Instantiate(collectEffect, transform.position, Quaternion.identity);
            Destroy(effect.gameObject, 2f);
        }

        // Som
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
        }
    }
}