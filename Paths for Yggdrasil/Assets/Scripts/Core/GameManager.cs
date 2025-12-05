using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using System.Collections.Generic;

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

        // ✅ Sistema de votação usando CustomProperties
        private bool votingInProgress = false;
        private bool transitionInProgress = false;

        // ✅ Chaves para CustomProperties dos jogadores
        private const string VOTE_KEY = "RematchVote";
        private const string VOTE_REMATCH = "Rematch";
        private const string VOTE_LOBBY = "Lobby";
        private const string VOTE_NONE = "None";

        // ✅ Chaves para CustomProperties da sala
        private const string ROOM_TRANSITION_KEY = "Transition";
        private const string ROOM_LOAD_SCENE_KEY = "LoadScene";
        private const string ROOM_TRANSITION_REMATCH = "Rematch";
        private const string ROOM_TRANSITION_LOBBY = "Lobby";
        private const string ROOM_TRANSITION_NONE = "None";

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
            votingInProgress = false;
            transitionInProgress = false;

            // Limpa voto do jogador local
            ClearLocalPlayerVote();

            // ✅ Limpa transição da sala (apenas Master Client)
            if (PhotonNetwork.IsMasterClient)
            {
                ClearRoomTransition();
            }

            // Spawn do jogador local automaticamente
            SpawnPlayer();

            // Encontra UIManager se não configurado
            if (uiManager == null)
            {
                uiManager = FindObjectOfType<UIManager>();
            }

            Debug.Log($"[GameManager] Jogo iniciado. Target Score: {targetScore} | IsMasterClient: {PhotonNetwork.IsMasterClient}");
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
                TriggerGameOver(player.NickName, newScore);
            }
        }

        /// <summary>
        /// Dispara o fim de jogo usando CustomProperties da sala
        /// </summary>
        private void TriggerGameOver(string winnerName, int finalScore)
        {
            if (gameEnded) return;

            gameEnded = true;

            Debug.Log($"[GameManager] 🏆 TRIGGERING GAME OVER! Winner: {winnerName}, Score: {finalScore}");

            // Usa CustomProperties da sala para sincronizar o fim do jogo
            ExitGames.Client.Photon.Hashtable gameOverProps = new ExitGames.Client.Photon.Hashtable
            {
                { "GameEnded", true },
                { "WinnerName", winnerName },
                { "FinalScore", finalScore }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(gameOverProps);

            // Executa o fim do jogo localmente
            ExecuteGameOver(winnerName, finalScore);
        }

        /// <summary>
        /// Executa o fim do jogo (chamado localmente e via callback)
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

            // ✅ Inicia sistema de votação
            votingInProgress = true;

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

        #region ✅ Sistema de Votação Rematch (SEM PhotonView)

        /// <summary>
        /// ✅ Limpa voto do jogador local
        /// </summary>
        private void ClearLocalPlayerVote()
        {
            if (PhotonNetwork.LocalPlayer == null) return;

            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
            {
                { VOTE_KEY, VOTE_NONE }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }

        /// <summary>
        /// ✅ Limpa estado de transição da sala
        /// </summary>
        private void ClearRoomTransition()
        {
            if (!PhotonNetwork.IsMasterClient) return;

            ExitGames.Client.Photon.Hashtable roomProps = new ExitGames.Client.Photon.Hashtable
            {
                { ROOM_TRANSITION_KEY, ROOM_TRANSITION_NONE },
                { ROOM_LOAD_SCENE_KEY, "" }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);

            Debug.Log("[GameManager] 🧹 Master Client limpou estado de transição da sala");
        }

        /// <summary>
        /// ✅ Jogador local vota para jogar novamente (chamado pelo botão)
        /// </summary>
        public void PlayAgain()
        {
            if (!votingInProgress || transitionInProgress)
            {
                Debug.LogWarning("[GameManager] Votação não está ativa ou transição em andamento");
                return;
            }

            // Define voto usando CustomProperties
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
            {
                { VOTE_KEY, VOTE_REMATCH }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);

            Debug.Log($"[GameManager] 🎮 Player {PhotonNetwork.NickName} votou: REMATCH");
        }

        /// <summary>
        /// ✅ Jogador local vota para voltar ao lobby (chamado pelo botão)
        /// </summary>
        public void BackToLobby()
        {
            if (!votingInProgress || transitionInProgress)
            {
                Debug.LogWarning("[GameManager] Votação não está ativa ou transição em andamento");
                return;
            }

            // Define voto usando CustomProperties
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
            {
                { VOTE_KEY, VOTE_LOBBY }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);

            Debug.Log($"[GameManager] 🚪 Player {PhotonNetwork.NickName} votou: LOBBY");
        }

        /// <summary>
        /// ✅ Conta votos de todos os jogadores
        /// </summary>
        private void CountVotes(out int rematchVotes, out int lobbyVotes, out int totalVoted)
        {
            rematchVotes = 0;
            lobbyVotes = 0;
            totalVoted = 0;

            foreach (Player player in PhotonNetwork.PlayerList)
            {
                if (player.CustomProperties.ContainsKey(VOTE_KEY))
                {
                    string vote = (string)player.CustomProperties[VOTE_KEY];

                    if (vote == VOTE_REMATCH)
                    {
                        rematchVotes++;
                        totalVoted++;
                    }
                    else if (vote == VOTE_LOBBY)
                    {
                        lobbyVotes++;
                        totalVoted++;
                    }
                }
            }
        }

        /// <summary>
        /// ✅ Atualiza UI com status da votação
        /// </summary>
        private void UpdateVotingUI()
        {
            if (uiManager == null) return;

            int rematchVotes, lobbyVotes, totalVoted;
            CountVotes(out rematchVotes, out lobbyVotes, out totalVoted);

            int totalPlayers = PhotonNetwork.CurrentRoom.PlayerCount;

            Debug.Log($"[GameManager] 📊 Votos: {totalVoted}/{totalPlayers} | Rematch: {rematchVotes} | Lobby: {lobbyVotes}");

            // Atualiza UI
            uiManager.UpdateRematchVoteStatus(rematchVotes, lobbyVotes, totalPlayers);
        }

        /// <summary>
        /// ✅ Verifica se todos os jogadores votaram e executa ação
        /// </summary>
        private void CheckVotingComplete()
        {
            if (!votingInProgress || transitionInProgress) return;

            int rematchVotes, lobbyVotes, totalVoted;
            CountVotes(out rematchVotes, out lobbyVotes, out totalVoted);

            int totalPlayers = PhotonNetwork.CurrentRoom.PlayerCount;

            // Verifica se todos votaram
            if (totalVoted < totalPlayers)
            {
                Debug.Log($"[GameManager] ⏳ Aguardando votos: {totalVoted}/{totalPlayers}");
                return;
            }

            Debug.Log($"[GameManager] 🗳️ Votação completa! Rematch: {rematchVotes}/{totalPlayers} | Lobby: {lobbyVotes}/{totalPlayers}");

            // ✅ APENAS MASTER CLIENT inicia a transição
            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.Log("[GameManager] 🎮 Cliente aguardando Master Client iniciar transição...");
                return;
            }

            // ✅ Master Client define transição via CustomProperties da sala
            if (rematchVotes == totalPlayers)
            {
                Debug.Log("[GameManager] 👑 Master Client: TODOS VOTARAM REMATCH! Iniciando transição...");
                SetRoomTransition(ROOM_TRANSITION_REMATCH, "GameScene");
            }
            else if (lobbyVotes == totalPlayers)
            {
                Debug.Log("[GameManager] 👑 Master Client: TODOS VOTARAM LOBBY! Iniciando transição...");
                SetRoomTransition(ROOM_TRANSITION_LOBBY, "");
            }
            else
            {
                Debug.Log("[GameManager] ⚠️ VOTOS DIVIDIDOS! Aguardando consenso...");
            }
        }

        /// <summary>
        /// ✅ MODIFICADO: Master Client define transição E cena a ser carregada
        /// </summary>
        private void SetRoomTransition(string transitionType, string sceneName)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.LogWarning("[GameManager] ⚠️ Apenas Master Client pode definir transição!");
                return;
            }

            ExitGames.Client.Photon.Hashtable roomProps = new ExitGames.Client.Photon.Hashtable
            {
                { ROOM_TRANSITION_KEY, transitionType },
                { ROOM_LOAD_SCENE_KEY, sceneName }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);

            Debug.Log($"[GameManager] 👑 Master Client definiu transição: {transitionType} | Cena: {sceneName}");
        }

        /// <summary>
        /// ✅ MODIFICADO: Executa rematch (TODOS os clientes carregam a cena manualmente)
        /// </summary>
        private void ExecuteRematch()
        {
            if (transitionInProgress)
            {
                Debug.LogWarning("[GameManager] ⚠️ Transição já em andamento");
                return;
            }

            transitionInProgress = true;
            votingInProgress = false;

            Debug.Log($"[GameManager] 🔄 EXECUTANDO REMATCH! (IsMasterClient: {PhotonNetwork.IsMasterClient})");

            // Reseta estado
            gameEnded = false;
            Time.timeScale = 1f;

            // Limpa voto local
            ClearLocalPlayerVote();

            // Mostra tela de carregamento
            if (uiManager != null)
            {
                uiManager.ShowRematchLoading();
            }

            // ✅ CRÍTICO: TODOS carregam a cena manualmente (não usa PhotonNetwork.LoadLevel)
            Debug.Log($"[GameManager] 🎬 {(PhotonNetwork.IsMasterClient ? "MASTER" : "CLIENT")} carregando GameScene via SceneManager em 1 segundo...");
            Invoke(nameof(LoadGameSceneManually), 1f);
        }

        /// <summary>
        /// ✅ NOVO: Carrega cena manualmente via UnityEngine.SceneManagement
        /// </summary>
        private void LoadGameSceneManually()
        {
            Debug.Log($"[GameManager] 🎬 {(PhotonNetwork.IsMasterClient ? "MASTER" : "CLIENT")} carregando GameScene MANUALMENTE...");
            UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
        }

        /// <summary>
        /// ✅ MODIFICADO: Executa retorno ao lobby
        /// </summary>
        private void ExecuteLobby()
        {
            if (transitionInProgress)
            {
                Debug.LogWarning("[GameManager] ⚠️ Transição já em andamento");
                return;
            }

            transitionInProgress = true;
            votingInProgress = false;

            Debug.Log($"[GameManager] 🚪 RETORNANDO AO LOBBY! (IsMasterClient: {PhotonNetwork.IsMasterClient})");

            // Reseta estado
            Time.timeScale = 1f;

            // Limpa voto local
            ClearLocalPlayerVote();

            // Mostra tela de carregamento
            if (uiManager != null)
            {
                uiManager.ShowLobbyLoading();
            }

            // Todos saem da sala
            Debug.Log("[GameManager] 🔌 Saindo da sala Photon...");
            PhotonNetwork.LeaveRoom();
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

            // ✅ Verifica mudanças de voto
            if (changedProps.ContainsKey(VOTE_KEY))
            {
                string vote = (string)changedProps[VOTE_KEY];
                Debug.Log($"[GameManager] 🗳️ {targetPlayer.NickName} votou: {vote}");

                // Atualiza UI
                UpdateVotingUI();

                // Verifica se todos votaram (apenas Master Client inicia transição)
                CheckVotingComplete();
            }

            // Notifica UIManager sobre mudanças
            if (uiManager != null)
            {
                uiManager.OnPlayerPropertiesUpdate(targetPlayer, changedProps);
            }
        }

        /// <summary>
        /// ✅ MODIFICADO: Callback quando CustomProperties da sala mudam
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

            // ✅ MODIFICADO: Verifica mudanças de transição
            if (propertiesThatChanged.ContainsKey(ROOM_TRANSITION_KEY))
            {
                string transition = (string)propertiesThatChanged[ROOM_TRANSITION_KEY];

                Debug.Log($"[GameManager] 📡 OnRoomPropertiesUpdate: Transição detectada = {transition} (IsMasterClient: {PhotonNetwork.IsMasterClient})");

                if (transition == ROOM_TRANSITION_REMATCH)
                {
                    Debug.Log("[GameManager] 🔄 TODOS OS CLIENTES: Iniciando REMATCH!");
                    ExecuteRematch();
                }
                else if (transition == ROOM_TRANSITION_LOBBY)
                {
                    Debug.Log("[GameManager] 🚪 TODOS OS CLIENTES: Retornando ao LOBBY!");
                    ExecuteLobby();
                }
            }
        }

        /// <summary>
        /// Callback quando sai da sala
        /// </summary>
        public override void OnLeftRoom()
        {
            Debug.Log("[GameManager] 🔌 OnLeftRoom: Saiu da sala Photon");

            // Limpa estado
            votingInProgress = false;
            transitionInProgress = false;

            // Retorna para cena do lobby
            Debug.Log("[GameManager] 🏠 Carregando cena do Lobby...");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
        }

        /// <summary>
        /// ✅ Callback quando outro jogador sai da sala
        /// </summary>
        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            Debug.Log($"[GameManager] 👋 Player {otherPlayer.NickName} (#{otherPlayer.ActorNumber}) saiu da sala");

            // Atualiza UI (voto dele será automaticamente ignorado)
            UpdateVotingUI();

            // ✅ Se estiver em votação, verifica novamente os votos
            if (votingInProgress && !transitionInProgress)
            {
                CheckVotingComplete();
            }
        }

        #endregion
    }
}