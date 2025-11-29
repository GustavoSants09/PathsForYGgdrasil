using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

/// <summary>
/// Gerencia checkpoints e respawn de jogadores
/// </summary>
public class CheckpointManager : MonoBehaviourPun
{
    public static CheckpointManager Instance { get; private set; }

    [Header("Configurações")]
    [Tooltip("Lista de pontos de spawn para Player 1")]
    [SerializeField] private List<Transform> player1SpawnPoints = new List<Transform>();

    [Tooltip("Lista de pontos de spawn para Player 2")]
    [SerializeField] private List<Transform> player2SpawnPoints = new List<Transform>();

    // Checkpoint atual de cada jogador
    private Dictionary<int, int> playerCheckpoints = new Dictionary<int, int>();

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
        }
    }

    /// <summary>
    /// Retorna posição de spawn baseado no tipo de jogador
    /// </summary>
    public Vector3 GetSpawnPosition(PlayerType playerType, int checkpointIndex = 0)
    {
        List<Transform> spawnPoints = playerType == PlayerType.Player1 ? player1SpawnPoints : player2SpawnPoints;

        if (spawnPoints.Count == 0)
        {
            Debug.LogError("Nenhum spawn point configurado!");
            return Vector3.zero;
        }

        // Garante que o índice está dentro dos limites
        checkpointIndex = Mathf.Clamp(checkpointIndex, 0, spawnPoints.Count - 1);

        return spawnPoints[checkpointIndex].position;
    }

    /// <summary>
    /// Retorna rotação de spawn baseado no tipo de jogador
    /// </summary>
    public Quaternion GetSpawnRotation(PlayerType playerType, int checkpointIndex = 0)
    {
        List<Transform> spawnPoints = playerType == PlayerType.Player1 ? player1SpawnPoints : player2SpawnPoints;

        if (spawnPoints.Count == 0)
        {
            return Quaternion.identity;
        }

        checkpointIndex = Mathf.Clamp(checkpointIndex, 0, spawnPoints.Count - 1);

        return spawnPoints[checkpointIndex].rotation;
    }

    /// <summary>
    /// Atualiza checkpoint de um jogador
    /// </summary>
    public void SetPlayerCheckpoint(int actorNumber, int checkpointIndex)
    {
        playerCheckpoints[actorNumber] = checkpointIndex;
        Debug.Log($"Jogador {actorNumber} alcançou checkpoint {checkpointIndex}");
    }

    /// <summary>
    /// Retorna checkpoint atual de um jogador
    /// </summary>
    public int GetPlayerCheckpoint(int actorNumber)
    {
        return playerCheckpoints.ContainsKey(actorNumber) ? playerCheckpoints[actorNumber] : 0;
    }

    /// <summary>
    /// Respawna um jogador no último checkpoint
    /// </summary>
    public void RespawnPlayer(GameObject player, PlayerType playerType)
    {
        PhotonView pv = player.GetComponent<PhotonView>();

        if (pv == null) return;

        int checkpointIndex = GetPlayerCheckpoint(pv.Owner.ActorNumber);
        Vector3 spawnPos = GetSpawnPosition(playerType, checkpointIndex);
        Quaternion spawnRot = GetSpawnRotation(playerType, checkpointIndex);

        // Reseta posição via RPC para sincronizar
        pv.RPC("RPC_Respawn", RpcTarget.All, spawnPos, spawnRot);
    }
}