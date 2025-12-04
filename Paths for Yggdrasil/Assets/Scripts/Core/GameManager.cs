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

        // ✅ Sistema de votação
        private HashSet<int> playersVotedRematch = new HashSet<int>();
        private HashSet<int> playersVotedLobby = new HashSet<int>();
        private bool rematchInProgress = false;

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
        /// ✅ Dispara o fim de jogo usando CustomProperties da sala
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
        /// ✅ Executa o fim do jogo (chamado localmente e via callback)
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
        /// ✅ Reseta votos de rematch
        /// </summary>
        private void ResetRematchVotes()
        {
            playersVotedRematch.Clear();
            playersVotedLobby.Clear();
            rematchInProgress = false;

            // Limpa propriedades customizadas de rematch
            if (PhotonNetwork.LocalPlayer != null)
            {
                ExitGames.Client.Photon.Hashtable clearProps = new ExitGames.Client.Photon.Hashtable
                {
                    { "WantsRematch", false },
                    { "WantsLobby", false }
                };
                PhotonNetwork.LocalPlayer.SetCustomProperties(clearProps);
            }
        }

        /// <summary>
        /// ✅ Botão: Jogar novamente - registra voto do player
        /// </summary>
        public void PlayAgain()
        {
            if (rematchInProgress)
            {
                Debug.Log("[GameManager] Rematch já em progresso!");
                return;
            }

            int myActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;

            // ✅ Envia RPC para todos os clientes
            photonView.RPC("RPC_VoteForRematch", RpcTarget.AllBuffered, myActorNumber);

            Debug.Log($"[GameManager] Player {PhotonNetwork.NickName} (#{myActorNumber}) votou para REMATCH");
        }

        /// <summary>
        /// ✅ Botão: Voltar ao lobby - registra voto do player
        /// </summary>
        public void BackToLobby()
        {
            if (rematchInProgress)
            {
                Debug.Log("[GameManager] Transição já em progresso!");
                return;
            }

            int myActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;

            // ✅ Envia RPC para todos os clientes
            photonView.RPC("RPC_VoteForLobby", RpcTarget.AllBuffered, myActorNumber);

            Debug.Log($"[GameManager] Player {PhotonNetwork.NickName} (#{myActorNumber}) votou para LOBBY");
        }

        /// <summary>
        /// ✅ RPC: Registra voto para rematch
        /// </summary>
        [PunRPC]
        private void RPC_VoteForRematch(int actorNumber)
        {
            if (!playersVotedRematch.Contains(actorNumber))
            {
                playersVotedRematch.Add(actorNumber);
                Debug.Log($"[GameManager] ✅ Voto REMATCH registrado: Player #{actorNumber}. Total: {playersVotedRematch.Count}/{PhotonNetwork.CurrentRoom.PlayerCount}");
            }

            // Remove voto de lobby se existir
            if (playersVotedLobby.Contains(actorNumber))
            {
                playersVotedLobby.Remove(actorNumber);
                Debug.Log($"[GameManager] 🔄 Player #{actorNumber} mudou voto de LOBBY para REMATCH");
            }

            // Notifica UIManager
            if (uiManager != null)
            {
                uiManager.UpdateRematchVoteStatus(playersVotedRematch.Count, playersVotedLobby.Count, PhotonNetwork.CurrentRoom.PlayerCount);
            }

            // Verifica se todos votaram
            CheckRematchVotes();
        }

        /// <summary>
        /// ✅ RPC: Registra voto para voltar ao lobby
        /// </summary>
        [PunRPC]
        private void RPC_VoteForLobby(int actorNumber)
        {
            if (!playersVotedLobby.Contains(actorNumber))
            {
                playersVotedLobby.Add(actorNumber);
                Debug.Log($"[GameManager] ✅ Voto LOBBY registrado: Player #{actorNumber}. Total: {playersVotedLobby.Count}/{PhotonNetwork.CurrentRoom.PlayerCount}");
            }

            // Remove voto de rematch se existir
            if (playersVotedRematch.Contains(actorNumber))
            {
                playersVotedRematch.Remove(actorNumber);
                Debug.Log($"[GameManager] 🔄 Player #{actorNumber} mudou voto de REMATCH para LOBBY");
            }

            // Notifica UIManager
            if (uiManager != null)
            {
                uiManager.UpdateRematchVoteStatus(playersVotedRematch.Count, playersVotedLobby.Count, PhotonNetwork.CurrentRoom.PlayerCount);
            }

            // Verifica se todos votaram
            CheckRematchVotes();
        }

        /// <summary>
        /// ✅ Verifica se todos os jogadores votaram
        /// </summary>
        private void CheckRematchVotes()
        {
            int totalPlayers = PhotonNetwork.CurrentRoom.PlayerCount;

            Debug.Log($"[GameManager] CheckRematchVotes: Rematch={playersVotedRematch.Count}/{totalPlayers}, Lobby={playersVotedLobby.Count}/{totalPlayers}");

            // ✅ Todos votaram rematch
            if (playersVotedRematch.Count == totalPlayers && totalPlayers >= 1)
            {
                Debug.Log("[GameManager] 🎮 TODOS VOTARAM REMATCH! Executando...");
                photonView.RPC("RPC_ExecuteRematch", RpcTarget.All);
            }
            // ✅ Todos votaram lobby
            else if (playersVotedLobby.Count == totalPlayers && totalPlayers >= 1)
            {
                Debug.Log("[GameManager] 🚪 TODOS VOTARAM LOBBY! Executando...");
                photonView.RPC("RPC_ExecuteLobby", RpcTarget.All);
            }
        }

        /// <summary>
        /// ✅ RPC: Executa rematch para todos os clientes
        /// </summary>
        [PunRPC]
        private void RPC_ExecuteRematch()
        {
            if (rematchInProgress)
            {
                Debug.Log("[GameManager] ⚠️ Rematch já em progresso, ignorando chamada duplicada");
                return;
            }

            rematchInProgress = true;
            Debug.Log("[GameManager] 🔄 EXECUTANDO REMATCH para todos os jogadores!");

            // Limpa votos
            playersVotedRematch.Clear();
            playersVotedLobby.Clear();

            // Reseta estado do jogo
            gameEnded = false;
            Time.timeScale = 1f;

            // Notifica UIManager
            if (uiManager != null)
            {
                uiManager.ShowRematchLoading();
            }

            // ✅ IMPORTANTE: Apenas Master Client carrega a cena
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log("[GameManager] 👑 Master Client carregando GameScene para todos...");
                PhotonNetwork.LoadLevel("GameScene");
            }
            else
            {
                Debug.Log("[GameManager] 🎮 Cliente aguardando Master carregar a cena...");
            }
        }

        /// <summary>
        /// ✅ RPC: Executa retorno ao lobby para todos os clientes
        /// </summary>
        [PunRPC]
        private void RPC_ExecuteLobby()
        {
            if (rematchInProgress)
            {
                Debug.Log("[GameManager] ⚠️ Transição já em progresso, ignorando chamada duplicada");
                return;
            }

            rematchInProgress = true;
            Debug.Log("[GameManager] 🚪 EXECUTANDO RETORNO AO LOBBY para todos os jogadores!");

            // Limpa votos
            playersVotedRematch.Clear();
            playersVotedLobby.Clear();

            // Reseta time scale
            Time.timeScale = 1f;

            // Notifica UIManager
            if (uiManager != null)
            {
                uiManager.ShowLobbyLoading();
            }

            // ✅ Todos os clientes saem da sala
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

            // Notifica UIManager sobre mudanças
            if (uiManager != null)
            {
                uiManager.OnPlayerPropertiesUpdate(targetPlayer, changedProps);
            }
        }

        /// <summary>
        /// ✅ Callback quando CustomProperties da sala mudam
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

        /// <summary>
        /// ✅ Callback quando sai da sala
        /// </summary>
        public override void OnLeftRoom()
        {
            Debug.Log("[GameManager] 🔌 OnLeftRoom: Saiu da sala Photon");

            // Limpa votos ao sair da sala
            playersVotedRematch.Clear();
            playersVotedLobby.Clear();
            rematchInProgress = false;

            // ✅ Retorna para cena do lobby usando SceneManager (não Photon)
            Debug.Log("[GameManager] 🏠 Carregando cena do Lobby...");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
        }

        /// <summary>
        /// ✅ Callback quando outro jogador sai da sala
        /// </summary>
        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            Debug.Log($"[GameManager] 👋 Player {otherPlayer.NickName} (#{otherPlayer.ActorNumber}) saiu da sala");

            // Remove votos do player que saiu
            playersVotedRematch.Remove(otherPlayer.ActorNumber);
            playersVotedLobby.Remove(otherPlayer.ActorNumber);

            // Atualiza UI
            if (uiManager != null)
            {
                uiManager.UpdateRematchVoteStatus(playersVotedRematch.Count, playersVotedLobby.Count, PhotonNetwork.CurrentRoom.PlayerCount);
            }

            // Verifica votos novamente (se jogo terminou)
            if (gameEnded && !rematchInProgress)
            {
                CheckRematchVotes();
            }
        }

        #endregion
    }
}