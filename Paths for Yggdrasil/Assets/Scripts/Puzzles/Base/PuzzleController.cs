using Photon.Pun;
using UnityEngine;

public abstract class PuzzleController : MonoBehaviourPun, IPuzzleBehavior
{
    [Header("Base Settings")]
    [SerializeField] protected int puzzleIndex;
    [SerializeField] protected bool isActive = false;

    protected bool isCompleted = false;

    public virtual void InitializePuzzle()
    {
        isActive = false;
        isCompleted = false;
        Debug.Log($"[Puzzle {puzzleIndex}] Initialized");
    }

    public virtual void ActivatePuzzle()
    {
        isActive = true;
        Debug.Log($"[Puzzle {puzzleIndex}] Activated");
    }

    public virtual void DeactivatePuzzle()
    {
        isActive = false;
        Debug.Log($"[Puzzle {puzzleIndex}] Deactivated");
    }

    public abstract bool CheckSolution();

    public virtual void OnPuzzleCompleted()
    {
        if (isCompleted) return;

        isCompleted = true;
        isActive = false;

        photonView.RPC("RPC_PuzzleCompleted", RpcTarget.All);
    }

    [PunRPC]
    protected virtual void RPC_PuzzleCompleted()
    {
        Debug.Log($"[Puzzle {puzzleIndex}] COMPLETED!");
        GameEvents.OnPuzzleCompleted?.Invoke(puzzleIndex);
    }
}