using UnityEngine;
using Photon.Pun;
using Photon.Voice.Unity;

/// <summary>
/// Representa um jogador na rede
/// Gerencia sincronização, role e estado do jogador
/// </summary>
public class NetworkPlayer : MonoBehaviourPun
{
    [Header("Configurações")]
    [SerializeField] private PlayerRole playerRole = PlayerRole.Player1;

    [Header("Referências")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioListener audioListener;
    [SerializeField] private Recorder voiceRecorder;

    // Estado do jogador
    private bool isInitialized = false;
    private string playerName = "";

    // Propriedades públicas
    public PlayerRole Role => playerRole;
    public bool IsInitialized => isInitialized;
    public string PlayerName => playerName;
    public Camera PlayerCamera => playerCamera;

    /// <summary>
    /// Inicializa o jogador
    /// </summary>
    public void InitializePlayer(PlayerRole role, string name)
    {
        if (isInitialized)
        {
            Debug.LogWarning("Jogador já foi inicializado!");
            return;
        }

        playerRole = role;
        playerName = name;

        // Configura componentes locais
        if (photonView.IsMine)
        {
            SetupLocalPlayer();
        }
        else
        {
            SetupRemotePlayer();
        }

        isInitialized = true;

        Debug.Log($"NetworkPlayer inicializado - Role: {role}, Nome: {name}, IsMine: {photonView.IsMine}");
    }

    /// <summary>
    /// Configura jogador local
    /// </summary>
    private void SetupLocalPlayer()
    {
        // Ativa câmera e audio listener
        if (playerCamera != null)
        {
            playerCamera.enabled = true;
        }

        if (audioListener != null)
        {
            audioListener.enabled = true;
        }

        // Configura sistema de voz
        if (voiceRecorder != null && VoiceManager.Instance != null)
        {
            VoiceManager.Instance.SetupLocalRecorder(voiceRecorder);
        }

        Debug.Log("Jogador local configurado");
    }

    /// <summary>
    /// Configura jogador remoto
    /// </summary>
    private void SetupRemotePlayer()
    {
        // Desativa câmera e audio listener para jogadores remotos
        if (playerCamera != null)
        {
            playerCamera.enabled = false;
        }

        if (audioListener != null)
        {
            audioListener.enabled = false;
        }

        Debug.Log("Jogador remoto configurado");
    }

    /// <summary>
    /// RPC para respawn
    /// </summary>
    [PunRPC]
    void RPC_Respawn(Vector3 position, Quaternion rotation)
    {
        transform.position = position;
        transform.rotation = rotation;

        Debug.Log($"Jogador respawnado em {position}");
    }
}

/// <summary>
/// Define os papéis dos jogadores
/// </summary>
public enum PlayerRole
{
    Player1,
    Player2
}