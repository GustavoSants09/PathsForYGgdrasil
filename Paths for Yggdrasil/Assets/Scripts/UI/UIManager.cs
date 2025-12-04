using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;

namespace QuantumHeist.Game
{
    /// <summary>
    /// Gerencia interface do usuário durante o jogo
    /// Centraliza atualizações de UI e transições de painéis
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Score UI")]
        [SerializeField] private TextMeshProUGUI localPlayerScoreText;
        [SerializeField] private TextMeshProUGUI localPlayerNameText;
        [SerializeField] private Slider localPlayerProgressBar;
        [SerializeField] private TextMeshProUGUI opponentScoreText;
        [SerializeField] private TextMeshProUGUI opponentNameText;
        [SerializeField] private Slider opponentProgressBar;
        [SerializeField] private TextMeshProUGUI targetScoreText;

        [Header("Game Over UI")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TextMeshProUGUI gameOverTitleText;
        [SerializeField] private TextMeshProUGUI gameOverDetailsText;
        [SerializeField] private Button rematchButton; // ✅ NOVO
        [SerializeField] private Button returnToLobbyButton;

        [Header("Rematch System")] // ✅ NOVO
        [SerializeField] private TextMeshProUGUI rematchStatusText;
        [SerializeField] private GameObject waitingForOpponentPanel;

        private GameManager gameManager;
        private bool hasVotedRematch = false; // ✅ NOVO

        void Start()
        {
            gameManager = FindObjectOfType<GameManager>();

            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);

            if (waitingForOpponentPanel != null)
                waitingForOpponentPanel.SetActive(false);

            // ✅ Configura botões
            if (rematchButton != null)
                rematchButton.onClick.AddListener(VoteRematch);

            if (returnToLobbyButton != null)
                returnToLobbyButton.onClick.AddListener(ReturnToLobby);

            UpdateTargetScoreDisplay();
            StartCoroutine(UpdateScoresRoutine());
        }

        #region Score Display

        /// <summary>
        /// Atualiza display do score alvo
        /// </summary>
        private void UpdateTargetScoreDisplay()
        {
            if (targetScoreText != null && gameManager != null)
            {
                targetScoreText.text = $"META: {gameManager.GetTargetScore()} pts";
            }
        }

        /// <summary>
        /// Atualiza pontuação do jogador local
        /// </summary>
        public void UpdateScore(int newScore)
        {
            if (localPlayerScoreText != null)
            {
                localPlayerScoreText.text = $"{newScore}";
            }

            UpdateProgressBar(localPlayerProgressBar, newScore);
        }

        /// <summary>
        /// Atualiza barra de progresso
        /// </summary>
        private void UpdateProgressBar(Slider progressBar, int currentScore)
        {
            if (progressBar != null && gameManager != null)
            {
                float progress = (float)currentScore / gameManager.GetTargetScore();
                progressBar.value = progress;
            }
        }

        /// <summary>
        /// Atualiza scores de todos os jogadores periodicamente
        /// </summary>
        private IEnumerator UpdateScoresRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.5f);
                UpdateAllPlayerScores();
            }
        }

        /// <summary>
        /// Atualiza UI com pontuações de todos os jogadores
        /// </summary>
        public void UpdateAllPlayerScores()
        {
            if (!PhotonNetwork.InRoom) return;

            Player localPlayer = PhotonNetwork.LocalPlayer;
            Player opponent = null;

            // Encontra o oponente
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                if (player != localPlayer)
                {
                    opponent = player;
                    break;
                }
            }

            // Atualiza jogador local
            if (localPlayerNameText != null)
                localPlayerNameText.text = localPlayer.NickName;

            if (localPlayer.CustomProperties.ContainsKey("Score"))
            {
                int localScore = (int)localPlayer.CustomProperties["Score"];
                UpdateScore(localScore);
            }

            // Atualiza oponente
            if (opponent != null)
            {
                if (opponentNameText != null)
                    opponentNameText.text = opponent.NickName;

                if (opponent.CustomProperties.ContainsKey("Score"))
                {
                    int opponentScore = (int)opponent.CustomProperties["Score"];

                    if (opponentScoreText != null)
                        opponentScoreText.text = $"{opponentScore}";

                    UpdateProgressBar(opponentProgressBar, opponentScore);
                }
            }
            else
            {
                // Sem oponente
                if (opponentNameText != null)
                    opponentNameText.text = "Aguardando...";
                if (opponentScoreText != null)
                    opponentScoreText.text = "0";
            }
        }

        #endregion

        #region Game Over System

        /// <summary>
        /// ✅ Exibe tela de Game Over com opções
        /// </summary>
        public void ShowGameOver(string winnerName, int finalScore)
        {
            if (gameOverPanel != null)
                gameOverPanel.SetActive(true);

            bool isLocalPlayerWinner = winnerName == PhotonNetwork.LocalPlayer.NickName;

            if (gameOverTitleText != null)
            {
                gameOverTitleText.text = isLocalPlayerWinner ? "🏆 VITÓRIA!" : "💀 DERROTA";
                gameOverTitleText.color = isLocalPlayerWinner ? Color.green : Color.red;
            }

            if (gameOverDetailsText != null)
            {
                gameOverDetailsText.text = $"{winnerName} venceu com {finalScore} pontos!";
            }

            // ✅ Habilita botões
            if (rematchButton != null)
                rematchButton.interactable = true;

            if (returnToLobbyButton != null)
                returnToLobbyButton.interactable = true;

            // ✅ Reseta estado de rematch
            hasVotedRematch = false;
            UpdateRematchStatus();

            Debug.Log($"[UIManager] Game Over exibido. Vencedor: {winnerName}");
        }

        #endregion

        #region Rematch System

        /// <summary>
        /// ✅ NOVO: Vota para jogar novamente
        /// </summary>
        private void VoteRematch()
        {
            if (hasVotedRematch)
            {
                Debug.Log("[UIManager] Já votou para rematch!");
                return;
            }

            hasVotedRematch = true;

            // ✅ Marca voto nas CustomProperties
            ExitGames.Client.Photon.Hashtable voteProps = new ExitGames.Client.Photon.Hashtable
            {
                { "WantsRematch", true }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(voteProps);

            Debug.Log($"[UIManager] {PhotonNetwork.LocalPlayer.NickName} votou para REMATCH!");

            // ✅ Verifica se todos votaram
            CheckRematchVotes();

            // ✅ Atualiza UI
            UpdateRematchStatus();
        }

        /// <summary>
        /// ✅ NOVO: Verifica se todos os jogadores votaram para rematch
        /// </summary>
        private void CheckRematchVotes()
        {
            int totalPlayers = PhotonNetwork.PlayerList.Length;
            int rematchVotes = 0;

            foreach (Player player in PhotonNetwork.PlayerList)
            {
                if (player.CustomProperties.ContainsKey("WantsRematch"))
                {
                    bool wantsRematch = (bool)player.CustomProperties["WantsRematch"];
                    if (wantsRematch)
                    {
                        rematchVotes++;
                    }
                }
            }

            Debug.Log($"[UIManager] Votos para rematch: {rematchVotes}/{totalPlayers}");

            // ✅ Se todos votaram, inicia rematch
            if (rematchVotes == totalPlayers && totalPlayers > 1)
            {
                Debug.Log("[UIManager] 🔄 TODOS VOTARAM! Iniciando rematch...");
                StartCoroutine(StartRematchDelayed());
            }
        }

        /// <summary>
        /// ✅ NOVO: Inicia rematch após delay
        /// </summary>
        private IEnumerator StartRematchDelayed()
        {
            // Mostra painel de loading
            if (waitingForOpponentPanel != null)
                waitingForOpponentPanel.SetActive(true);

            if (rematchStatusText != null)
                rematchStatusText.text = "🔄 Reiniciando partida...";

            yield return new WaitForSeconds(2f);

            // ✅ Master Client reinicia o jogo
            if (PhotonNetwork.IsMasterClient)
            {
                // Reseta votos de rematch
                foreach (Player player in PhotonNetwork.PlayerList)
                {
                    ExitGames.Client.Photon.Hashtable resetProps = new ExitGames.Client.Photon.Hashtable
                    {
                        { "WantsRematch", false },
                        { "Score", 0 }
                    };
                    player.SetCustomProperties(resetProps);
                }

                // Recarrega a cena
                PhotonNetwork.LoadLevel(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            }
        }

        /// <summary>
        /// ✅ NOVO: Atualiza status do rematch na UI
        /// </summary>
        private void UpdateRematchStatus()
        {
            if (rematchStatusText == null)
                return;

            int totalPlayers = PhotonNetwork.PlayerList.Length;
            int rematchVotes = 0;

            foreach (Player player in PhotonNetwork.PlayerList)
            {
                if (player.CustomProperties.ContainsKey("WantsRematch"))
                {
                    bool wantsRematch = (bool)player.CustomProperties["WantsRematch"];
                    if (wantsRematch)
                    {
                        rematchVotes++;
                    }
                }
            }

            if (hasVotedRematch)
            {
                rematchStatusText.text = $"⏳ Aguardando oponente... ({rematchVotes}/{totalPlayers})";

                if (waitingForOpponentPanel != null)
                    waitingForOpponentPanel.SetActive(true);
            }
            else
            {
                rematchStatusText.text = "Deseja jogar novamente?";
            }
        }

        /// <summary>
        /// ✅ Callback quando CustomProperties de um jogador mudam
        /// </summary>
        public void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
        {
            if (changedProps.ContainsKey("WantsRematch"))
            {
                Debug.Log($"[UIManager] {targetPlayer.NickName} atualizou voto de rematch");
                CheckRematchVotes();
                UpdateRematchStatus();
            }
        }

        #endregion

        #region Navigation

        /// <summary>
        /// Retorna ao lobby
        /// </summary>
        private void ReturnToLobby()
        {
            Debug.Log("[UIManager] Retornando ao lobby...");

            // ✅ Limpa votos antes de sair
            ExitGames.Client.Photon.Hashtable clearProps = new ExitGames.Client.Photon.Hashtable
            {
                { "WantsRematch", false },
                { "Score", 0 }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(clearProps);

            // ✅ Sai da sala
            PhotonNetwork.LeaveRoom();

            // ✅ Carrega cena do lobby
            StartCoroutine(LoadLobbyWhenDisconnected());
        }

        /// <summary>
        /// ✅ Aguarda sair da sala antes de carregar lobby
        /// </summary>
        private IEnumerator LoadLobbyWhenDisconnected()
        {
            while (PhotonNetwork.InRoom)
            {
                yield return null;
            }

            // ✅ Carrega cena de lobby (ajuste o nome conforme sua cena)
            PhotonNetwork.LoadLevel("Lobby");
        }

        #endregion

        #region Unity Callbacks

        private void OnEnable()
        {
            // ✅ Registra callbacks do Photon
            PhotonNetwork.NetworkingClient.EventReceived += OnEventReceived;
        }

        private void OnDisable()
        {
            // ✅ Remove callbacks do Photon
            PhotonNetwork.NetworkingClient.EventReceived -= OnEventReceived;
        }

        /// <summary>
        /// ✅ Handler de eventos customizados do Photon
        /// </summary>
        private void OnEventReceived(ExitGames.Client.Photon.EventData photonEvent)
        {
            // Placeholder para eventos futuros
        }

        #endregion
    }
}