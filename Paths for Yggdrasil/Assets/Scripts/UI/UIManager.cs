using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gerencia todos os elementos de UI do jogo.
/// Controla painéis, displays de tempo, progresso e menus.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD Elements")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI puzzleProgressText;
    [SerializeField] private Image timerBar;
    [SerializeField] private Image healthBar;

    [Header("Menus")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject defeatPanel;
    [SerializeField] private GameObject lobbyPanel;

    [Header("Victory Screen")]
    [SerializeField] private TextMeshProUGUI victoryTimeText;
    [SerializeField] private TextMeshProUGUI victoryPuzzlesText;
    [SerializeField] private TextMeshProUGUI victoryErrorsText;

    [Header("Interaction")]
    [SerializeField] private TextMeshProUGUI interactionPromptText;
    [SerializeField] private GameObject interactionPromptPanel;

    [Header("Timer Colors")]
    [SerializeField] private Color normalTimerColor = Color.green;
    [SerializeField] private Color warningTimerColor = Color.yellow;
    [SerializeField] private Color criticalTimerColor = Color.red;
    [SerializeField] private float warningThreshold = 120f; // 2 minutos
    [SerializeField] private float criticalThreshold = 60f; // 1 minuto

    private float maxGameTime;

    #region Unity Lifecycle

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Inicializa com todos os painéis fechados
        HideAllPanels();

        Debug.Log("[UIManager] Inicializado");
    }

    private void Start()
    {
        // Obtém duração máxima do jogo
        if (GameManager.Instance != null)
        {
            maxGameTime = 300f; // 5 minutos default
        }
    }

    #endregion

    #region Timer Display

    /// <summary>
    /// Atualiza o display do timer
    /// </summary>
    public void UpdateTimerDisplay(float remainingTime)
    {
        if (timerText != null)
        {
            timerText.text = GameManager.FormatTime(remainingTime);
        }

        // Atualiza barra de tempo
        if (timerBar != null)
        {
            float fillAmount = remainingTime / maxGameTime;
            timerBar.fillAmount = fillAmount;

            // Muda cor baseado no tempo restante
            if (remainingTime <= criticalThreshold)
            {
                timerBar.color = criticalTimerColor;
            }
            else if (remainingTime <= warningThreshold)
            {
                timerBar.color = warningTimerColor;
            }
            else
            {
                timerBar.color = normalTimerColor;
            }
        }

        // Atualiza "barra de vida" da Yggdrasil (inverso do tempo)
        if (healthBar != null)
        {
            healthBar.fillAmount = remainingTime / maxGameTime;
            healthBar.color = timerBar != null ? timerBar.color : normalTimerColor;
        }
    }

    #endregion

    #region Puzzle Progress

    /// <summary>
    /// Atualiza o display de progresso dos puzzles
    /// </summary>
    public void UpdatePuzzleProgress(int solved, int total)
    {
        if (puzzleProgressText != null)
        {
            puzzleProgressText.text = $"Puzzles: {solved}/{total}";
        }
    }

    #endregion

    #region Interaction Prompts

    /// <summary>
    /// Mostra prompt de interação
    /// </summary>
    public void ShowInteractionPrompt(string message)
    {
        if (interactionPromptPanel != null)
        {
            interactionPromptPanel.SetActive(true);
        }

        if (interactionPromptText != null)
        {
            interactionPromptText.text = message;
        }
    }

    /// <summary>
    /// Esconde prompt de interação
    /// </summary>
    public void HideInteractionPrompt()
    {
        if (interactionPromptPanel != null)
        {
            interactionPromptPanel.SetActive(false);
        }
    }

    #endregion

    #region Menu Panels

    /// <summary>
    /// Mostra o menu de pausa
    /// </summary>
    public void ShowPauseMenu()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Esconde o menu de pausa
    /// </summary>
    public void HidePauseMenu()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Mostra a tela de vitória
    /// </summary>
    public void ShowVictoryScreen()
    {
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
        }

        // Atualiza estatísticas na tela de vitória
        if (GameManager.Instance != null)
        {
            if (victoryTimeText != null)
            {
                victoryTimeText.text = $"Tempo: {GameManager.FormatTime(GameManager.Instance.ElapsedTime)}";
            }

            if (victoryPuzzlesText != null)
            {
                victoryPuzzlesText.text = $"Puzzles Resolvidos: {GameManager.Instance.TotalPuzzlesSolved}";
            }

            if (victoryErrorsText != null)
            {
                victoryErrorsText.text = $"Erros: {GameManager.Instance.TotalErrors}";
            }
        }
    }

    /// <summary>
    /// Mostra a tela de derrota
    /// </summary>
    public void ShowDefeatScreen()
    {
        if (defeatPanel != null)
        {
            defeatPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Mostra o painel do lobby
    /// </summary>
    public void ShowLobbyPanel()
    {
        HideAllPanels();
        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Esconde todos os painéis
    /// </summary>
    private void HideAllPanels()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (defeatPanel != null) defeatPanel.SetActive(false);
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (interactionPromptPanel != null) interactionPromptPanel.SetActive(false);
    }

    #endregion

    #region Button Handlers

    /// <summary>
    /// Handler do botão Resume
    /// </summary>
    public void OnResumeButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResumeGame();
        }
    }

    /// <summary>
    /// Handler do botão Restart
    /// </summary>
    public void OnRestartButtonClicked()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }

    /// <summary>
    /// Handler do botão Menu
    /// </summary>
    public void OnMenuButtonClicked()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(0); // Assume que menu é cena 0
    }

    /// <summary>
    /// Handler do botão Quit
    /// </summary>
    public void OnQuitButtonClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }

    #endregion

    #region Cleanup

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    #endregion
}