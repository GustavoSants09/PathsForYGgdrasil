using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;

/// <summary>
/// Gerenciador central do jogo
/// Controla fluxo, estado, estatísticas e coordenação entre sistemas
/// </summary>
public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance { get; private set; }

    [Header("Estado do Jogo")]
    [SerializeField] private GameState currentState = GameState.WaitingForPlayers;

    [Header("Estatísticas")]
    private float gameStartTime;
    private int totalPuzzlesSolved = 0;
    private int totalErrors = 0;

    // Propriedades públicas para acesso
    public GameState CurrentState => currentState;
    public float ElapsedTime => Time.time - gameStartTime;
    public int TotalPuzzlesSolved => totalPuzzlesSolved;
    public int TotalErrors => totalErrors;

    // Eventos
    public delegate void GameStateChanged(GameState newState);
    public event GameStateChanged OnGameStateChanged;

    /// <summary>
    /// Singleton
    /// </summary>
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    /// Inicialização
    /// </summary>
    void Start()
    {
        Debug.Log("GameManager inicializado");
    }

    /// <summary>
    /// Muda o estado do jogo
    /// </summary>
    public void ChangeGameState(GameState newState)
    {
        if (currentState == newState) return;

        GameState previousState = currentState;
        currentState = newState;

        Debug.Log($"Estado mudou: {previousState} -> {newState}");

        // Notifica listeners
        OnGameStateChanged?.Invoke(newState);

        // Sincroniza via Photon
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("RPC_SyncGameState", RpcTarget.Others, (int)newState);
        }
    }

    /// <summary>
    /// Sincroniza estado do jogo entre clientes
    /// </summary>
    [PunRPC]
    void RPC_SyncGameState(int stateIndex)
    {
        GameState newState = (GameState)stateIndex;

        if (currentState != newState)
        {
            currentState = newState;
            OnGameStateChanged?.Invoke(newState);
            Debug.Log($"Estado sincronizado: {newState}");
        }
    }

    /// <summary>
    /// Inicia o jogo quando todos os jogadores estão prontos
    /// </summary>
    public void OnAllPlayersReady()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Debug.Log("Todos os jogadores prontos - iniciando jogo");

        gameStartTime = Time.time;
        ChangeGameState(GameState.Playing);
    }

    /// <summary>
    /// Registra que um puzzle foi resolvido
    /// </summary>
    public void OnPuzzleSolved(string puzzleName)
    {
        totalPuzzlesSolved++;

        Debug.Log($"Puzzle resolvido: {puzzleName} (Total: {totalPuzzlesSolved})");

        // Sincroniza com outros clientes
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("RPC_SyncPuzzleSolved", RpcTarget.Others, puzzleName, totalPuzzlesSolved);
        }
    }

    /// <summary>
    /// Sincroniza puzzle resolvido entre clientes
    /// </summary>
    [PunRPC]
    void RPC_SyncPuzzleSolved(string puzzleName, int newTotal)
    {
        totalPuzzlesSolved = newTotal;
        Debug.Log($"Puzzle sincronizado: {puzzleName} (Total: {totalPuzzlesSolved})");
    }

    /// <summary>
    /// Registra um erro cometido pelos jogadores
    /// </summary>
    public void RegisterError(string errorDescription)
    {
        totalErrors++;

        Debug.Log($"Erro registrado: {errorDescription} (Total: {totalErrors})");

        // Sincroniza com outros clientes
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("RPC_SyncError", RpcTarget.Others, errorDescription, totalErrors);
        }
    }

    /// <summary>
    /// Sincroniza erro entre clientes
    /// </summary>
    [PunRPC]
    void RPC_SyncError(string errorDescription, int newTotal)
    {
        totalErrors = newTotal;
        Debug.Log($"Erro sincronizado: {errorDescription} (Total: {totalErrors})");
    }

    /// <summary>
    /// Pausa o jogo
    /// </summary>
    public void PauseGame()
    {
        if (currentState != GameState.Playing) return;

        ChangeGameState(GameState.Paused);
        Time.timeScale = 0f;

        Debug.Log("Jogo pausado");
    }

    /// <summary>
    /// Resume o jogo
    /// </summary>
    public void ResumeGame()
    {
        if (currentState != GameState.Paused) return;

        ChangeGameState(GameState.Playing);
        Time.timeScale = 1f;

        Debug.Log("Jogo resumido");
    }

    /// <summary>
    /// Finaliza o jogo
    /// </summary>
    public void EndGame(bool victory)
    {
        ChangeGameState(victory ? GameState.Victory : GameState.GameOver);

        float finalTime = ElapsedTime;

        Debug.Log($"Jogo finalizado - Vitória: {victory}, Tempo: {FormatTime(finalTime)}, Puzzles: {totalPuzzlesSolved}, Erros: {totalErrors}");
    }

    /// <summary>
    /// Formata tempo em MM:SS
    /// </summary>
    public static string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    /// <summary>
    /// Retorna se o jogo está em andamento
    /// </summary>
    public bool IsPlaying()
    {
        return currentState == GameState.Playing;
    }

    /// <summary>
    /// Callback quando jogador entra na sala
    /// </summary>
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"Jogador entrou: {newPlayer.NickName}");

        // Verifica se todos os jogadores estão presentes
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            Debug.Log("Sala completa - aguardando jogadores ficarem prontos");
        }
    }

    /// <summary>
    /// Callback quando jogador sai da sala
    /// </summary>
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.LogWarning($"Jogador saiu: {otherPlayer.NickName}");

        // Pausa o jogo se estava em andamento
        if (currentState == GameState.Playing)
        {
            PauseGame();
        }
    }
}

/// <summary>
/// Estados possíveis do jogo
/// </summary>
public enum GameState
{
    WaitingForPlayers,  // Aguardando jogadores
    Ready,              // Jogadores prontos
    Playing,            // Jogo em andamento
    Paused,             // Jogo pausado
    Victory,            // Vitória
    GameOver            // Derrota
}