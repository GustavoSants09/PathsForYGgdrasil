using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using ExitGames.Client.Photon;

namespace QuantumHeist.Game
{
    /// <summary>
    /// Gerencia o estado do jogo, pontuações e condições de vitória
    /// Sincroniza dados de jogo via Custom Properties do Photon
    /// </summary>
    public class GameManager : MonoBehaviourPunCallbacks
    {
        public static GameManager Instance { get; private set; }

        [Header("Configurações de Jogo")]
        [SerializeField] private int targetScore = 200;
        [SerializeField] private float matchDuration = 300f; // 5 minutos

        [Header("Spawn Settings")]
        [SerializeField] private Transform[] playerSpawnPoints;
        [SerializeField] private GameObject playerPrefab;

        [Header("UI References")]
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private Transform scoreboardContent;
        [SerializeField] private GameObject scoreboardItemPrefab;
        [SerializeField] private GameObject endGamePanel;
        [SerializeField] private TMP_Text winnerText;

        // Dicionário de pontuações (ActorNumber -> Score)
        private Dictionary<int, int> playerScores = new Dictionary<int, int>();

        // Controle de tempo
        private float gameStartTime;
        private bool gameEnded = false;

        // Constantes para Custom Properties
        private const string SCORE_KEY = "Score";
        private const string GAME_START_TIME_KEY = "GameStartTime";

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
            // Apenas Master Client inicializa o tempo de jogo
            if (PhotonNetwork.IsMasterClient)
            {
                Hashtable roomProps = new Hashtable
                {
                    { GAME_START_TIME_KEY, (float)PhotonNetwork.Time }
                };
                PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
            }

            // Spawn do jogador local
            SpawnPlayer();

            // Inicializa pontuações de todos os jogadores
            InitializeScores();
        }

        private void Update()
        {
            if (gameEnded)
                return;

            // Atualiza timer
            UpdateTimer();

            // Verifica condições de vitória
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

            // Escolhe ponto de spawn aleatório
            Transform spawnPoint = playerSpawnPoints[Random.Range(0, playerSpawnPoints.Length)];

            // Instancia jogador via Photon
            GameObject player = PhotonNetwork.Instantiate(
                playerPrefab.name,
                spawnPoint.position,
                spawnPoint.rotation
            );

            Debug.Log($"Jogador '{PhotonNetwork.NickName}' spawnado em {spawnPoint.position}");
        }

        #endregion

        #region Score Management

        /// <summary>
        /// Inicializa pontuação de todos os jogadores para 0
        /// </summary>
        private void InitializeScores()
        {
            foreach (var playerEntry in PhotonNetwork.CurrentRoom.Players)
            {
                Player player = playerEntry.Value;

                // Inicializa pontuação se ainda não existe
                if (!player.CustomProperties.ContainsKey(SCORE_KEY))
                {
                    Hashtable props = new Hashtable { { SCORE_KEY, 0 } };
                    player.SetCustomProperties(props);
                }

                playerScores[player.ActorNumber] = 0;
            }

            UpdateScoreboard();
        }

        /// <summary>
        /// Adiciona pontos ao jogador especificado
        /// </summary>
        public void AddScore(int actorNumber, int points)
        {
            Player player = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
            if (player == null)
                return;

            // Obtém pontuação atual
            int currentScore = player.CustomProperties.ContainsKey(SCORE_KEY)
                ? (int)player.CustomProperties[SCORE_KEY]
                : 0;

            // Adiciona pontos
            int newScore = Mathf.Max(0, currentScore + points);

            // Atualiza Custom Property
            Hashtable props = new Hashtable { { SCORE_KEY, newScore } };
            player.SetCustomProperties(props);

            Debug.Log($"Jogador {player.NickName}: {currentScore} -> {newScore} ({points:+#;-#;0} pontos)");
        }

        /// <summary>
        /// Obtém a pontuação atual de um jogador
        /// </summary>
        public int GetScore(int actorNumber)
        {
            Player player = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
            if (player == null || !player.CustomProperties.ContainsKey(SCORE_KEY))
                return 0;

            return (int)player.CustomProperties[SCORE_KEY];
        }

        #endregion

        #region Game Flow

        /// <summary>
        /// Atualiza o timer do jogo
        /// </summary>
        private void UpdateTimer()
        {
            if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(GAME_START_TIME_KEY))
                return;

            float startTime = (float)PhotonNetwork.CurrentRoom.CustomProperties[GAME_START_TIME_KEY];
            float elapsedTime = (float)PhotonNetwork.Time - startTime;
            float remainingTime = Mathf.Max(0, matchDuration - elapsedTime);

            // Atualiza UI
            int minutes = Mathf.FloorToInt(remainingTime / 60);
            int seconds = Mathf.FloorToInt(remainingTime % 60);
            timerText.text = $"{minutes:00}:{seconds:00}";

            // Verifica se tempo acabou
            if (remainingTime <= 0 && !gameEnded)
            {
                EndGameByTime();
            }
        }

        /// <summary>
        /// Verifica se alguém atingiu a pontuação alvo
        /// </summary>
        private void CheckWinConditions()
        {
            if (gameEnded)
                return;

            foreach (var playerEntry in PhotonNetwork.CurrentRoom.Players)
            {
                Player player = playerEntry.Value;
                int score = GetScore(player.ActorNumber);

                if (score >= targetScore)
                {
                    EndGameByScore(player);
                    return;
                }
            }
        }

        /// <summary>
        /// Finaliza o jogo quando alguém atinge a pontuação
        /// </summary>
        private void EndGameByScore(Player winner)
        {
            if (gameEnded)
                return;

            gameEnded = true;
            photonView.RPC("RPC_EndGame", RpcTarget.All, winner.ActorNumber, true);
        }

        /// <summary>
        /// Finaliza o jogo quando o tempo acaba
        /// </summary>
        private void EndGameByTime()
        {
            if (gameEnded)
                return;

            gameEnded = true;

            // Encontra jogador com maior pontuação
            Player winner = PhotonNetwork.CurrentRoom.Players.Values
                .OrderByDescending(p => GetScore(p.ActorNumber))
                .FirstOrDefault();

            if (winner != null)
            {
                photonView.RPC("RPC_EndGame", RpcTarget.All, winner.ActorNumber, false);
            }
        }

        /// <summary>
        /// RPC que finaliza o jogo para todos os clientes
        /// </summary>
        [PunRPC]
        private void RPC_EndGame(int winnerActorNumber, bool reachedTargetScore)
        {
            gameEnded = true;

            Player winner = PhotonNetwork.CurrentRoom.GetPlayer(winnerActorNumber);
            if (winner != null)
            {
                string reason = reachedTargetScore
                    ? $"atingiu {targetScore} pontos!"
                    : "teve a maior pontuação!";

                winnerText.text = $"{winner.NickName} venceu!\n{reason}";
            }

            // Mostra painel de fim de jogo
            endGamePanel.SetActive(true);

            // Desabilita controles do jogador
            var playerController = FindObjectOfType<PlayerController>();
            if (playerController != null && playerController.photonView.IsMine)
            {
                playerController.enabled = false;
            }
        }

        #endregion

        #region UI Methods

        /// <summary>
        /// Atualiza o scoreboard com pontuações atuais
        /// </summary>
        private void UpdateScoreboard()
        {
            // Limpa scoreboard atual
            foreach (Transform child in scoreboardContent)
            {
                Destroy(child.gameObject);
            }

            // Ordena jogadores por pontuação
            var sortedPlayers = PhotonNetwork.CurrentRoom.Players.Values
                .OrderByDescending(p => GetScore(p.ActorNumber));

            // Cria item para cada jogador
            foreach (Player player in sortedPlayers)
            {
                GameObject item = Instantiate(scoreboardItemPrefab, scoreboardContent);
                TMP_Text text = item.GetComponentInChildren<TMP_Text>();

                if (text != null)
                {
                    int score = GetScore(player.ActorNumber);
                    text.text = $"{player.NickName}: {score}";
                }
            }
        }

        /// <summary>
        /// Botão: Jogar novamente (reinicia a cena)
        /// </summary>
        public void PlayAgain()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.LoadLevel("GameScene");
            }
        }

        /// <summary>
        /// Botão: Voltar ao lobby
        /// </summary>
        public void BackToLobby()
        {
            PhotonNetwork.LeaveRoom();
        }

        #endregion

        #region Photon Callbacks

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            // Atualiza scoreboard quando pontuação muda
            if (changedProps.ContainsKey(SCORE_KEY))
            {
                UpdateScoreboard();
            }
        }

        public override void OnLeftRoom()
        {
            // Retorna para cena do lobby
            UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
        }

        #endregion
    }
}