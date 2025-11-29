using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Classe base para todos os puzzles do jogo.
/// Define interface comum e lógica de resolução.
/// </summary>
public abstract class PuzzleBase : MonoBehaviour
{
    [Header("Puzzle Configuration")]
    [SerializeField] protected string puzzleID;
    [SerializeField] protected string puzzleName;
    [SerializeField] protected bool requiresBothPlayers = true;

    [Header("State")]
    [SerializeField] protected bool isActive = false;
    [SerializeField] protected bool isSolved = false;

    [Header("Events")]
    public UnityEvent OnPuzzleActivated;
    public UnityEvent OnPuzzleSolved;
    public UnityEvent OnPuzzleFailed;

    protected float startTime;
    protected int attemptCount = 0;

    #region Properties

    public string PuzzleID => puzzleID;
    public string PuzzleName => puzzleName;
    public bool IsActive => isActive;
    public bool IsSolved => isSolved;
    public int AttemptCount => attemptCount;

    #endregion

    #region Lifecycle

    protected virtual void Awake()
    {
        if (string.IsNullOrEmpty(puzzleID))
        {
            puzzleID = $"Puzzle_{GetInstanceID()}";
        }
    }

    protected virtual void Start()
    {
        InitializePuzzle();
    }

    #endregion

    #region Abstract Methods

    /// <summary>
    /// Inicializa o estado do puzzle
    /// </summary>
    protected abstract void InitializePuzzle();

    /// <summary>
    /// Verifica se o puzzle foi resolvido corretamente
    /// </summary>
    protected abstract bool CheckSolution();

    /// <summary>
    /// Reseta o puzzle para o estado inicial
    /// </summary>
    public abstract void ResetPuzzle();

    #endregion

    #region Public Methods

    /// <summary>
    /// Ativa o puzzle
    /// </summary>
    public virtual void ActivatePuzzle()
    {
        if (isActive || isSolved)
        {
            return;
        }

        isActive = true;
        startTime = Time.time;
        OnPuzzleActivated?.Invoke();

        Debug.Log($"[{puzzleName}] Puzzle ativado");
    }

    /// <summary>
    /// Tenta resolver o puzzle
    /// </summary>
    public virtual void AttemptSolution()
    {
        if (!isActive || isSolved)
        {
            return;
        }

        attemptCount++;
        bool solved = CheckSolution();

        if (solved)
        {
            CompletePuzzle();
        }
        else
        {
            OnPuzzleFailed?.Invoke();
            Debug.Log($"[{puzzleName}] Tentativa {attemptCount} falhou");
        }
    }

    /// <summary>
    /// Marca o puzzle como completo
    /// </summary>
    protected virtual void CompletePuzzle()
    {
        isSolved = true;
        isActive = false;
        float completionTime = Time.time - startTime;

        OnPuzzleSolved?.Invoke();

        Debug.Log($"[{puzzleName}] ✓ Puzzle resolvido! Tempo: {completionTime:F2}s | Tentativas: {attemptCount}");

        // Notifica o GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPuzzleSolved(puzzleID, completionTime, attemptCount);
        }
    }

    #endregion

    #region Debug

    protected void LogDebug(string message)
    {
        Debug.Log($"[{puzzleName}] {message}");
    }

    protected void LogWarning(string message)
    {
        Debug.LogWarning($"[{puzzleName}] {message}");
    }

    protected void LogError(string message)
    {
        Debug.LogError($"[{puzzleName}] {message}");
    }

    #endregion
}