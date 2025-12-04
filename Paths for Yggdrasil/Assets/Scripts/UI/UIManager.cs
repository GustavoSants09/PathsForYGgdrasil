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
        [SerializeField] private Button rematchButton;
        [SerializeField] private Button returnToLobbyButton;

        [Header("Rematch System")]
        [SerializeField] private TextMeshProUGUI rematchStatusText;
        [SerializeField] private GameObject waitingForOpponentPanel;
        [SerializeField] private TextMeshProUGUI loadingText;

        private GameManager gameManager;
        private bool hasVoted = false;

        void Start()
        {
            gameManager = FindObjectOfType<GameManager>();

            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);

            if (waitingForOpponentPanel != null)
                waitingForOpponentPanel.SetActive(false);

            // ✅ Configura botões
            if (rematchButton != null)
                rematchButton.onClick.AddListener(OnRematchButtonClicked);

            if (returnToLobbyButton != null)
                returnToLobbyButton.onClick.AddListener(OnReturnToLobbyButtonClicked);

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

            // ✅ Habilita botões e reseta estado
            if (rematchButton != null)
                rematchButton.interactable = true;

            if (returnToLobbyButton != null)
                returnToLobbyButton.interactable = true;

            hasVoted = false;

            // ✅ Atualiza status inicial
            UpdateRematchVoteStatus(0, 0, PhotonNetwork.CurrentRoom.PlayerCount);

            Debug.Log($"[UIManager] Game Over exibido. Vencedor: {winnerName}");
        }

        #endregion

        #region Rematch System

        /// <summary>
        /// ✅ Botão: Votar para rematch
        /// </summary>
        private void OnRematchButtonClicked()
        {
            if (hasVoted)
            {
                Debug.Log("[UIManager] Já votou!");
                return;
            }

            hasVoted = true;

            // Desabilita botões para evitar cliques duplicados
            if (rematchButton != null)
                rematchButton.interactable = false;

            if (returnToLobbyButton != null)
                returnToLobbyButton.interactable = false;

            // ✅ Chama GameManager para registrar voto
            if (gameManager != null)
            {
                gameManager.PlayAgain();
            }

            Debug.Log($"[UIManager] {PhotonNetwork.LocalPlayer.NickName} clicou em REMATCH");
        }

        /// <summary>
        /// ✅ Botão: Votar para voltar ao lobby
        /// </summary>
        private void OnReturnToLobbyButtonClicked()
        {
            if (hasVoted)
            {
                Debug.Log("[UIManager] Já votou!");
                return;
            }

            hasVoted = true;

            // Desabilita botões para evitar cliques duplicados
            if (rematchButton != null)
                rematchButton.interactable = false;

            if (returnToLobbyButton != null)
                returnToLobbyButton.interactable = false;

            // ✅ Chama GameManager para registrar voto
            if (gameManager != null)
            {
                gameManager.BackToLobby();
            }

            Debug.Log($"[UIManager] {PhotonNetwork.LocalPlayer.NickName} clicou em LOBBY");
        }

        /// <summary>
        /// ✅ Atualiza status de votação (chamado pelo GameManager)
        /// </summary>
        public void UpdateRematchVoteStatus(int rematchVotes, int lobbyVotes, int totalPlayers)
        {
            if (rematchStatusText == null)
                return;

            if (hasVoted)
            {
                rematchStatusText.text = $"⏳ Aguardando oponente...\n" +
                                         $"Rematch: {rematchVotes}/{totalPlayers} | Lobby: {lobbyVotes}/{totalPlayers}";
            }
            else
            {
                rematchStatusText.text = $"Escolha uma opção:\n" +
                                         $"Rematch: {rematchVotes}/{totalPlayers} | Lobby: {lobbyVotes}/{totalPlayers}";
            }

            Debug.Log($"[UIManager] Status atualizado: Rematch={rematchVotes}, Lobby={lobbyVotes}, Total={totalPlayers}");
        }

        /// <summary>
        /// ✅ Mostra tela de loading do rematch
        /// </summary>
        public void ShowRematchLoading()
        {
            if (waitingForOpponentPanel != null)
                waitingForOpponentPanel.SetActive(true);

            if (loadingText != null)
                loadingText.text = "🔄 Reiniciando partida...";

            Debug.Log("[UIManager] Exibindo loading de rematch");
        }

        /// <summary>
        /// ✅ Mostra tela de loading do retorno ao lobby
        /// </summary>
        public void ShowLobbyLoading()
        {
            if (waitingForOpponentPanel != null)
                waitingForOpponentPanel.SetActive(true);

            if (loadingText != null)
                loadingText.text = "🚪 Retornando ao lobby...";

            Debug.Log("[UIManager] Exibindo loading de retorno ao lobby");
        }

        /// <summary>
        /// ✅ Callback quando CustomProperties de um jogador mudam
        /// </summary>
        public void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
        {
            // Você pode adicionar lógica adicional aqui se necessário
            Debug.Log($"[UIManager] Player {targetPlayer.NickName} atualizou propriedades");
        }

        #endregion
    }
}