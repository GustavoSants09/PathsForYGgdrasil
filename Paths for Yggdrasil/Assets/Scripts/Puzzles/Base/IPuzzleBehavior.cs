public interface IPuzzleBehavior
{
    void InitializePuzzle();
    void ActivatePuzzle();
    void DeactivatePuzzle();
    bool CheckSolution();
    void OnPuzzleCompleted();
}