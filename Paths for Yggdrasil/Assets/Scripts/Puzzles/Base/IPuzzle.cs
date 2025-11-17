namespace Yggdrasil.Puzzles
{
    public enum PuzzleState
    {
        NotStarted,
        Active,
        Completed,
        Failed
    }

    public interface IPuzzle
    {
        PuzzleState State { get; }
        int PuzzleIndex { get; }

        void Initialize();
        void StartPuzzle();
        void CompletePuzzle();
        void FailPuzzle();
        void ResetPuzzle();
    }
}