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
        [SerializeField] private Button returnToLobbyButton;

        private GameManager gameManager;

        void Start()
        {
            gameManager = FindObjectOfType<GameManager>();

            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);

            if (returnToLobbyButton != null)
                returnToLobbyButton.onClick.AddListener(ReturnToLobby);

            UpdateTargetScoreDisplay();
            StartCoroutine(UpdateScoresRoutine());

        }

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
private System.Collections.IEnumerator UpdateScoresRoutine()
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

/// <summary>
/// Exibe tela de Game Over
/// </summary>
public void ShowGameOver(string winnerName, int finalScore)
{
    if (gameOverPanel != null)
        gameOverPanel.SetActive(true);

    bool isLocalPlayerWinner = winnerName == PhotonNetwork.LocalPlayer.NickName;

    if (gameOverTitleText != null)
    {
        gameOverTitleText.text = isLocalPlayerWinner ? "VITÓRIA!" : "DERROTA";
        gameOverTitleText.color = isLocalPlayerWinner ? Color.green : Color.red;
    }

    if (gameOverDetailsText != null)
    {
        gameOverDetailsText.text = $"{winnerName} venceu com {finalScore} pontos!";
    }

    Debug.Log($"[UIManager] Game Over exibido. Vencedor: {winnerName}");
}

/// <summary>
/// Retorna ao lobby
/// </summary>
private void ReturnToLobby()
{
    PhotonNetwork.LeaveRoom();
    PhotonNetwork.LoadLevel("LobbyScene"); // Ajuste o nome da sua cena de lobby
}
    }
}