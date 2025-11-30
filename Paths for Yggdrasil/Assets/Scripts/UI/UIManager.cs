using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

namespace QuantumHeist.UI
{
    /// <summary>
    /// Gerencia interface do usuário durante o jogo
    /// Centraliza atualizações de UI e transições de painéis
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("HUD Elements")]
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text playerNameText;

        [Header("Ability Cooldowns")]
        [SerializeField] private Image dashCooldownFill;
        [SerializeField] private TMP_Text dashCooldownText;
        [SerializeField] private Image slowZoneCooldownFill;
        [SerializeField] private TMP_Text slowZoneCooldownText;

        [Header("Status Indicators")]
        [SerializeField] private GameObject speedBoostIndicator;
        [SerializeField] private GameObject slowedIndicator;
        [SerializeField] private GameObject stunnedIndicator;

        [Header("Scoreboard")]
        [SerializeField] private GameObject scoreboardPanel;
        [SerializeField] private Transform scoreboardContent;
        [SerializeField] private GameObject scoreboardItemPrefab;
        [SerializeField] private KeyCode scoreboardToggleKey = KeyCode.Tab;

        [Header("Notifications")]
        [SerializeField] private GameObject notificationPanel;
        [SerializeField] private TMP_Text notificationText;
        [SerializeField] private float notificationDuration = 3f;

        [Header("End Game")]
        [SerializeField] private GameObject endGamePanel;
        [SerializeField] private TMP_Text winnerText;
        [SerializeField] private TMP_Text finalScoreText;
        [SerializeField] private Transform finalScoreboardContent;

        [Header("Pause Menu")]
        [SerializeField] private GameObject pauseMenuPanel;
        [SerializeField] private KeyCode pauseKey = KeyCode.Escape;

        private bool isPaused = false;
        private bool scoreboardVisible = false;

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
            // Inicializa UI
            InitializeUI();
        }

        private void Update()
        {
            // Toggle scoreboard
            if (Input.GetKeyDown(scoreboardToggleKey))
            {
                ToggleScoreboard();
            }

            // Toggle pause menu
            if (Input.GetKeyDown(pauseKey))
            {
                TogglePauseMenu();
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Inicializa elementos de UI
        /// </summary>
        private void InitializeUI()
        {
            // Mostra HUD
            if (hudPanel != null)
                hudPanel.SetActive(true);

            // Esconde painéis
            if (scoreboardPanel != null)
                scoreboardPanel.SetActive(false);

            if (endGamePanel != null)
                endGamePanel.SetActive(false);

            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(false);

            if (notificationPanel != null)
                notificationPanel.SetActive(false);

            // Define nome do jogador
            if (playerNameText != null)
                playerNameText.text = PhotonNetwork.NickName;

            // Reseta indicadores
            ResetStatusIndicators();
        }

        #endregion

        #region HUD Updates

        /// <summary>
        /// Atualiza texto de pontuação
        /// </summary>
        public void UpdateScore(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = $"Pontos: {score}";
            }
        }

        /// <summary>
        /// Atualiza texto de timer
        /// </summary>
        public void UpdateTimer(string timeString)
        {
            if (timerText != null)
            {
                timerText.text = timeString;
            }
        }

        /// <summary>
        /// Atualiza cooldown do dash
        /// </summary>
        public void UpdateDashCooldown(float currentCooldown, float maxCooldown)
        {
            if (dashCooldownFill != null)
            {
                dashCooldownFill.fillAmount = 1f - (currentCooldown / maxCooldown);
            }

            if (dashCooldownText != null)
            {
                if (currentCooldown > 0)
                {
                    dashCooldownText.text = $"{currentCooldown:F1}s";
                    dashCooldownText.color = Color.red;
                }
                else
                {
                    dashCooldownText.text = "PRONTO";
                    dashCooldownText.color = Color.green;
                }
            }
        }

        /// <summary>
        /// Atualiza cooldown da slow zone
        /// </summary>
        public void UpdateSlowZoneCooldown(float currentCooldown, float maxCooldown)
        {
            if (slowZoneCooldownFill != null)
            {
                slowZoneCooldownFill.fillAmount = 1f - (currentCooldown / maxCooldown);
            }

            if (slowZoneCooldownText != null)
            {
                if (currentCooldown > 0)
                {
                    slowZoneCooldownText.text = $"{currentCooldown:F1}s";
                    slowZoneCooldownText.color = Color.red;
                }
                else
                {
                    slowZoneCooldownText.text = "PRONTO";
                    slowZoneCooldownText.color = Color.green;
                }
            }
        }

        #endregion

        #region Status Indicators

        /// <summary>
        /// Reseta todos os indicadores de status
        /// </summary>
        private void ResetStatusIndicators()
        {
            SetSpeedBoostIndicator(false);
            SetSlowedIndicator(false);
            SetStunnedIndicator(false);
        }

        /// <summary>
        /// Mostra/esconde indicador de speed boost
        /// </summary>
        public void SetSpeedBoostIndicator(bool active)
        {
            if (speedBoostIndicator != null)
                speedBoostIndicator.SetActive(active);
        }

        /// <summary>
        /// Mostra/esconde indicador de slow
        /// </summary>
        public void SetSlowedIndicator(bool active)
        {
            if (slowedIndicator != null)
                slowedIndicator.SetActive(active);
        }

        /// <summary>
        /// Mostra/esconde indicador de stun
        /// </summary>
        public void SetStunnedIndicator(bool active)
        {
            if (stunnedIndicator != null)
                stunnedIndicator.SetActive(active);
        }

        #endregion

        #region Scoreboard

        /// <summary>
        /// Alterna visibilidade do scoreboard
        /// </summary>
        public void ToggleScoreboard()
        {
            scoreboardVisible = !scoreboardVisible;

            if (scoreboardPanel != null)
                scoreboardPanel.SetActive(scoreboardVisible);

            if (scoreboardVisible)
            {
                UpdateScoreboard();
            }
        }

        /// <summary>
        /// Atualiza conteúdo do scoreboard
        /// </summary>
        private void UpdateScoreboard()
        {
            if (scoreboardContent == null || scoreboardItemPrefab == null)
                return;

            // Limpa scoreboard atual
            foreach (Transform child in scoreboardContent)
            {
                Destroy(child.gameObject);
            }

            // Obtém e ordena jogadores por pontuação
            var players = PhotonNetwork.CurrentRoom.Players.Values;
            var sortedPlayers = System.Linq.Enumerable.OrderByDescending(
                players,
                p => Game.GameManager.Instance.GetScore(p.ActorNumber)
            );

            // Cria item para cada jogador
            int rank = 1;
            foreach (var player in sortedPlayers)
            {
                GameObject item = Instantiate(scoreboardItemPrefab, scoreboardContent);
                TMP_Text text = item.GetComponentInChildren<TMP_Text>();

                if (text != null)
                {
                    int score = Game.GameManager.Instance.GetScore(player.ActorNumber);
                    string masterTag = player.IsMasterClient ? " [HOST]" : "";
                    text.text = $"{rank}. {player.NickName}{masterTag}: {score}";
                }

                rank++;
            }
        }

        #endregion

        #region Notifications

        /// <summary>
        /// Mostra notificação temporária
        /// </summary>
        public void ShowNotification(string message)
        {
            if (notificationPanel == null || notificationText == null)
                return;

            notificationText.text = message;
            notificationPanel.SetActive(true);

            CancelInvoke(nameof(HideNotification));
            Invoke(nameof(HideNotification), notificationDuration);
        }

        /// <summary>
        /// Esconde notificação
        /// </summary>
        private void HideNotification()
        {
            if (notificationPanel != null)
                notificationPanel.SetActive(false);
        }

        #endregion

        #region End Game

        /// <summary>
        /// Mostra painel de fim de jogo
        /// </summary>
        public void ShowEndGameScreen(string winnerName, bool reachedTarget, int targetScore)
        {
            if (endGamePanel == null)
                return;

            // Esconde HUD
            if (hudPanel != null)
                hudPanel.SetActive(false);

            // Mostra painel de fim de jogo
            endGamePanel.SetActive(true);

            // Define texto de vitória
            if (winnerText != null)
            {
                string reason = reachedTarget
                    ? $"atingiu {targetScore} pontos!"
                    : "teve a maior pontuação!";
                winnerText.text = $"{winnerName} venceu!\n{reason}";
            }

            // Atualiza scoreboard final
            UpdateFinalScoreboard();
        }

        /// <summary>
        /// Atualiza scoreboard final
        /// </summary>
        private void UpdateFinalScoreboard()
        {
            if (finalScoreboardContent == null || scoreboardItemPrefab == null)
                return;

            // Limpa scoreboard atual
            foreach (Transform child in finalScoreboardContent)
            {
                Destroy(child.gameObject);
            }

            // Obtém e ordena jogadores por pontuação
            var players = PhotonNetwork.CurrentRoom.Players.Values;
            var sortedPlayers = System.Linq.Enumerable.OrderByDescending(
                players,
                p => Game.GameManager.Instance.GetScore(p.ActorNumber)
            );

            // Cria item para cada jogador
            int rank = 1;
            foreach (var player in sortedPlayers)
            {
                GameObject item = Instantiate(scoreboardItemPrefab, finalScoreboardContent);
                TMP_Text text = item.GetComponentInChildren<TMP_Text>();

                if (text != null)
                {
                    int score = Game.GameManager.Instance.GetScore(player.ActorNumber);
                    string medal = rank == 1 ? "🥇" : rank == 2 ? "🥈" : rank == 3 ? "🥉" : "";
                    text.text = $"{medal} {rank}. {player.NickName}: {score} pontos";
                }

                rank++;
            }
        }

        #endregion

        #region Pause Menu

        /// <summary>
        /// Alterna menu de pausa
        /// </summary>
        public void TogglePauseMenu()
        {
            isPaused = !isPaused;

            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(isPaused);

            // Pausa/despausa o tempo (apenas visual, não afeta rede)
            Time.timeScale = isPaused ? 0f : 1f;

            // Esconde scoreboard se estiver aberto
            if (isPaused && scoreboardVisible)
            {
                ToggleScoreboard();
            }
        }

        /// <summary>
        /// Botão: Resume game
        /// </summary>
        public void ResumeGame()
        {
            if (isPaused)
            {
                TogglePauseMenu();
            }
        }

        /// <summary>
        /// Botão: Leave game
        /// </summary>
        public void LeaveGame()
        {
            Time.timeScale = 1f; // Restaura time scale
            PhotonNetwork.LeaveRoom();
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            // Garante que time scale seja restaurado
            Time.timeScale = 1f;
        }

        #endregion
    }
}