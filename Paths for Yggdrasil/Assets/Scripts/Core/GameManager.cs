using Photon.Pun;
using Photon.Realtime;
//using QuantumHeist.UI;
using UnityEngine;

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

        [Header("Win Condition")]
        [SerializeField] private int targetScore = 200;
        [SerializeField] private float matchDuration = 300f; // 5 minutos

        [Header("UI References")]
        [SerializeField] private UIManager uiManager;

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

        private void Update()
        {
            CheckWinConditions();
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

        /// <summary>
        /// Verifica se algum jogador atingiu a pontuação necessária
        /// </summary>
        private void CheckWinConditions()
        {
            if (!PhotonNetwork.IsMasterClient) return;

            foreach (Player player in PhotonNetwork.PlayerList)
            {
                if (player.CustomProperties.ContainsKey("Score"))
                {
                    int playerScore = (int)player.CustomProperties["Score"];

                    if (playerScore >= targetScore)
                    {
                        // Jogador venceu!
                        photonView.RPC("RPC_GameOver", RpcTarget.All, player.NickName, playerScore);
                        return;
                    }
                }
            }
        }

        [PunRPC]
        private void RPC_GameOver(string winnerName, int finalScore)
        {
            Debug.Log($"[GameManager] VITÓRIA! {winnerName} alcançou {finalScore} pontos!");

            if (uiManager != null)
            {
                uiManager.ShowGameOver(winnerName, finalScore);
            }

            // Desabilita controles dos jogadores
            PlayerController localPlayer = FindObjectOfType<PlayerController>();
            if (localPlayer != null)
            {
                localPlayer.enabled = false;
            }
        }

        /// <summary>
        /// Obtém pontuação necessária para vencer
        /// </summary>
        public int GetTargetScore()
        {
            return targetScore;
        }

        #endregion
    }
}