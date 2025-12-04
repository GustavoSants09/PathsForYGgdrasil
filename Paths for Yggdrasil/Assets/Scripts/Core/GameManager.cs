using Photon.Pun;
using Photon.Realtime;
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
        [SerializeField] private int targetScore = 250;
        [SerializeField] private float matchDuration = 300f; // 5 minutos

        [Header("UI References")]
        [SerializeField] private UIManager uiManager;

        private bool gameEnded = false;

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
            // Reseta estado do jogo
            gameEnded = false;

            // Reseta votos de rematch ao iniciar
            ResetRematchVotes();

            // Spawn do jogador local automaticamente
            SpawnPlayer();

            // Encontra UIManager se não configurado
            if (uiManager == null)
            {
                uiManager = FindObjectOfType<UIManager>();
            }

            Debug.Log($"[GameManager] Jogo iniciado. Target Score: {targetScore}");
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
        /// Verifica vitória IMEDIATAMENTE quando score é atualizado
        /// Chamado pelo PlayerController quando coleta cristal
        /// </summary>
        public void CheckScoreUpdate(int newScore, Player player)
        {
            if (gameEnded)
            {
                Debug.Log($"[GameManager] Jogo já terminou, ignorando score update");
                return;
            }

            Debug.Log($"[GameManager] CheckScoreUpdate: {player.NickName} = {newScore}/{targetScore}");

            // Verifica se atingiu o target
            if (newScore >= targetScore)
            {
                Debug.Log($"[GameManager] 🏆 {player.NickName} ATINGIU O TARGET! Finalizando jogo...");

                // ✅ QUALQUER CLIENTE pode finalizar o jogo (via CustomProperties)
                TriggerGameOver(player.NickName, newScore);
            }
        }

        /// <summary>
        /// ✅ NOVO: Dispara o fim de jogo usando CustomProperties da sala
        /// </summary>
        private void TriggerGameOver(string winnerName, int finalScore)
        {
            if (gameEnded) return;

            gameEnded = true;

            Debug.Log($"[GameManager] 🏆 TRIGGERING GAME OVER! Winner: {winnerName}, Score: {finalScore}");

            // ✅ Usa CustomProperties da sala para sincronizar o fim do jogo
            ExitGames.Client.Photon.Hashtable gameOverProps = new ExitGames.Client.Photon.Hashtable
            {
                { "GameEnded", true },
                { "WinnerName", winnerName },
                { "FinalScore", finalScore }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(gameOverProps);

            // ✅ Executa o fim do jogo localmente
            ExecuteGameOver(winnerName, finalScore);
        }

        /// <summary>
        /// ✅ NOVO: Executa o fim do jogo (chamado localmente e via callback)
        /// </summary>
        private void ExecuteGameOver(string winnerName, int finalScore)
        {
            if (!gameEnded)
            {
                gameEnded = true;
            }

            Debug.Log($"[GameManager] 🏆 GAME OVER! {winnerName} venceu com {finalScore} pontos!");

            // Desabilita controles de todos os jogadores IMEDIATAMENTE
            PlayerController[] allPlayers = FindObjectsOfType<PlayerController>();
            foreach (PlayerController player in allPlayers)
            {
                player.enabled = false;
                Debug.Log($"[GameManager] Player {player.name} desabilitado");
            }

            // Libera cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Exibe UI de game over
            if (uiManager != null)
            {
                uiManager.ShowGameOver(winnerName, finalScore);
            }
            else
            {
                Debug.LogError("[GameManager] UIManager não encontrado!");
            }
        }

        /// <summary>
        /// Obtém pontuação necessária para vencer
        /// </summary>
        public int GetTargetScore()
        {
            return targetScore;
        }

        /// <summary>
        /// Verifica se o jogo terminou
        /// </summary>
        public bool IsGameEnded()
        {
            return gameEnded;
        }

        #endregion

        #region Rematch System

        /// <summary>
        /// Reseta votos de rematch de todos os jogadores
        /// </summary>
        private void ResetRematchVotes()
        {
            ExitGames.Client.Photon.Hashtable resetProps = new ExitGames.Client.Photon.Hashtable
            {
                { "WantsRematch", false }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(resetProps);

            Debug.Log("[GameManager] Votos de rematch resetados");
        }

        #endregion

        #region Photon Callbacks

        /// <summary>
        /// Callback quando CustomProperties de um jogador mudam
        /// </summary>
        public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
        {
            // Verifica mudanças de score
            if (changedProps.ContainsKey("Score"))
            {
                int newScore = (int)changedProps["Score"];
                Debug.Log($"[GameManager] OnPlayerPropertiesUpdate: {targetPlayer.NickName} score = {newScore}");

                // Verifica vitória quando score é atualizado
                CheckScoreUpdate(newScore, targetPlayer);
            }

            // Notifica UIManager sobre mudanças
            if (uiManager != null)
            {
                uiManager.OnPlayerPropertiesUpdate(targetPlayer, changedProps);
            }
        }

        /// <summary>
        /// ✅ NOVO: Callback quando CustomProperties da sala mudam
        /// </summary>
        public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
        {
            // Verifica se o jogo terminou
            if (propertiesThatChanged.ContainsKey("GameEnded"))
            {
                bool ended = (bool)propertiesThatChanged["GameEnded"];

                if (ended && !gameEnded)
                {
                    string winnerName = (string)PhotonNetwork.CurrentRoom.CustomProperties["WinnerName"];
                    int finalScore = (int)PhotonNetwork.CurrentRoom.CustomProperties["FinalScore"];

                    Debug.Log($"[GameManager] OnRoomPropertiesUpdate: Jogo terminou! {winnerName} venceu!");

                    ExecuteGameOver(winnerName, finalScore);
                }
            }
        }

        #endregion
    }
}