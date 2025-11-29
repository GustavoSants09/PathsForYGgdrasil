using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;

/// <summary>
/// Gerencia a criação, sincronização e controle de jogadores na rede
/// </summary>
public class PlayerManager : MonoBehaviourPunCallbacks
{
    public static PlayerManager Instance { get; private set; }

    [Header("Configurações de Spawn")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform player1SpawnPoint;
    [SerializeField] private Transform player2SpawnPoint;

    [Header("Estado")]
    private NetworkPlayer localPlayer;
    private Dictionary<int, NetworkPlayer> allPlayers = new Dictionary<int, NetworkPlayer>();

    // Propriedades públicas
    public NetworkPlayer LocalPlayer => localPlayer;

    /// <summary>
    /// Singleton
    /// </summary>
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    /// Inicialização
    /// </summary>
    void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogError("PlayerManager requer conexão com Photon Network!");
            return;
        }

        Debug.Log("PlayerManager inicializado");
    }

    /// <summary>
    /// Spawna o jogador local na rede
    /// </summary>
    public void SpawnLocalPlayer()
    {
        if (localPlayer != null)
        {
            Debug.LogWarning("Jogador local já foi spawnado!");
            return;
        }

        if (playerPrefab == null)
        {
            Debug.LogError("Player Prefab não está configurado!");
            return;
        }

        // Define role e posição baseado no ActorNumber
        PlayerRole role = AssignPlayerRole(PhotonNetwork.LocalPlayer);
        Vector3 spawnPosition = GetSpawnPosition(role);

        // Instancia o jogador na rede
        GameObject playerObj = PhotonNetwork.Instantiate(
            playerPrefab.name,
            spawnPosition,
            Quaternion.identity
        );

        // Obtém componente NetworkPlayer
        localPlayer = playerObj.GetComponent<NetworkPlayer>();

        if (localPlayer != null)
        {
            // Inicializa o jogador
            localPlayer.InitializePlayer(role, PhotonNetwork.LocalPlayer.NickName);

            // Registra na lista de jogadores
            allPlayers[PhotonNetwork.LocalPlayer.ActorNumber] = localPlayer;

            Debug.Log($"Jogador local spawnado: {role} em {spawnPosition}");
        }
        else
        {
            Debug.LogError("NetworkPlayer component não encontrado no prefab!");
        }
    }

    /// <summary>
    /// Atribui role ao jogador
    /// </summary>
    private PlayerRole AssignPlayerRole(Player player)
    {
        // Primeiro jogador é Player1, segundo é Player2
        PlayerRole role = (player.ActorNumber == 1) ? PlayerRole.Player1 : PlayerRole.Player2;

        Debug.Log($"Role atribuída: {role} para jogador {player.NickName}");

        return role;
    }

    /// <summary>
    /// Retorna posição de spawn baseada no role
    /// </summary>
    private Vector3 GetSpawnPosition(PlayerRole role)
    {
        switch (role)
        {
            case PlayerRole.Player1:
                return player1SpawnPoint != null ? player1SpawnPoint.position : Vector3.zero;

            case PlayerRole.Player2:
                return player2SpawnPoint != null ? player2SpawnPoint.position : new Vector3(5f, 0f, 0f);

            default:
                return Vector3.zero;
        }
    }

    /// <summary>
    /// Retorna o jogador local (controlado por este cliente)
    /// </summary>
    public NetworkPlayer GetLocalPlayer()
    {
        return localPlayer;
    }

    /// <summary>
    /// Retorna um jogador por ActorNumber
    /// </summary>
    public NetworkPlayer GetPlayerByActorNumber(int actorNumber)
    {
        if (allPlayers.ContainsKey(actorNumber))
        {
            return allPlayers[actorNumber];
        }

        return null;
    }

    /// <summary>
    /// Retorna todos os jogadores ativos
    /// </summary>
    public List<NetworkPlayer> GetAllPlayers()
    {
        return new List<NetworkPlayer>(allPlayers.Values);
    }

    /// <summary>
    /// Atualiza nome do jogador na UI
    /// </summary>
    private void UpdatePlayerName(NetworkPlayer player)
    {
        if (player == null) return;

        string name = player.PlayerName;

        if (string.IsNullOrEmpty(name))
        {
            name = player.photonView.Owner.NickName;
        }

        Debug.Log($"Nome do jogador atualizado: {name}");
    }

    /// <summary>
    /// Callback quando novo jogador entra na sala
    /// </summary>
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"Novo jogador entrou: {newPlayer.NickName} (ActorNumber: {newPlayer.ActorNumber})");
    }

    /// <summary>
    /// Callback quando jogador sai da sala
    /// </summary>
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.LogWarning($"Jogador saiu: {otherPlayer.NickName} (ActorNumber: {otherPlayer.ActorNumber})");

        // Remove da lista de jogadores
        if (allPlayers.ContainsKey(otherPlayer.ActorNumber))
        {
            allPlayers.Remove(otherPlayer.ActorNumber);
        }
    }
}