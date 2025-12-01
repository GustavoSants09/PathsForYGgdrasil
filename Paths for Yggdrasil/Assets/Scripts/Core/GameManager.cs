using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

namespace QuantumHeist.Game
{
    /// <summary>
    /// Gerencia spawn de jogadores e estado inicial do jogo
    /// Responsável por instanciar players via Photon quando entrarem na cena
    /// </summary>
    public class GameManager : MonoBehaviourPunCallbacks
    {
        public static GameManager Instance { get; private set; }

        [Header("Spawn Settings")]
        [SerializeField] private Transform[] playerSpawnPoints;
        [SerializeField] private GameObject playerPrefab;

        #region Unity Callbacks

        private void Awake()
        {
            // Implementa Singleton
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Spawn do jogador local automaticamente
            SpawnPlayer();
        }

        #endregion

        #region Player Management

        /// <summary>
        /// Spawna o jogador local em um ponto aleatório
        /// </summary>
        private void SpawnPlayer()
        {
            if (playerPrefab == null || playerSpawnPoints.Length == 0)
            {
                Debug.LogError("PlayerPrefab ou SpawnPoints não configurados!");
                return;
            }

            // Escolhe ponto de spawn baseado no ActorNumber para evitar overlap
            int spawnIndex = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % playerSpawnPoints.Length;
            Transform spawnPoint = playerSpawnPoints[spawnIndex];

            // Instancia jogador via Photon
            GameObject player = PhotonNetwork.Instantiate(
                playerPrefab.name,
                spawnPoint.position,
                spawnPoint.rotation
            );

            Debug.Log($"Jogador '{PhotonNetwork.NickName}' spawnado em {spawnPoint.position}");
        }

        #endregion
    }
}