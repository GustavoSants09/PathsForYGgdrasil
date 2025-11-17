using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Lobby,
    InGame,
    GameOver,
    Victory
}

public class GameStateManager : MonoBehaviourPun
{
    public static GameStateManager Instance { get; private set; }

    [Header("Current State")]
    private GameState currentState = GameState.Lobby;

    public GameState CurrentState => currentState;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Subscreve eventos críticos
        GameEvents.OnGameStart += OnGameStart;
        GameEvents.OnGameOver += OnGameOver;
        GameEvents.OnVictory += OnVictory;
        GameEvents.OnPlayerDisconnected += OnPlayerDisconnected;
    }

    private void OnGameStart()
    {
        TransitionToState(GameState.InGame);
    }

    private void OnGameOver()
    {
        TransitionToState(GameState.GameOver);
    }

    private void OnVictory()
    {
        TransitionToState(GameState.Victory);
    }

    private void OnPlayerDisconnected()
    {
        // Se um jogador desconectar, pausa/encerra o jogo
        Debug.LogWarning("[GameState] Player disconnected. Returning to lobby...");
        ReturnToLobby();
    }

    public void TransitionToState(GameState newState)
    {
        if (currentState == newState) return;

        Debug.Log($"[GameState] Transitioning from {currentState} to {newState}");

        currentState = newState;

        switch (newState)
        {
            case GameState.InGame:
                HandleInGameState();
                break;
            case GameState.GameOver:
                HandleGameOverState();
                break;
            case GameState.Victory:
                HandleVictoryState();
                break;
        }
    }

    private void HandleInGameState()
    {
        Debug.Log("[GameState] Game started!");
        // Inicializa sistemas de jogo
    }

    private void HandleGameOverState()
    {
        Debug.Log("[GameState] Game Over!");
        // Mostra UI de Game Over
        // Para drenagem de vida
        // Desabilita inputs
    }

    private void HandleVictoryState()
    {
        Debug.Log("[GameState] Victory!");
        // Mostra UI de vitória
        // Para sistemas
    }

    public void ReturnToLobby()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("Lobby");
        }
    }

    public void RestartGame()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("GameScene");
        }
    }

    private void OnDestroy()
    {
        GameEvents.OnGameStart -= OnGameStart;
        GameEvents.OnGameOver -= OnGameOver;
        GameEvents.OnVictory -= OnVictory;
        GameEvents.OnPlayerDisconnected -= OnPlayerDisconnected;
    }
}