using UnityEngine;
using UnityEngine.Events;

public static class GameEvents
{
    // Game Flow
    public static UnityAction OnGameStart;
    public static UnityAction OnGameOver;
    public static UnityAction OnVictory;
    public static UnityAction OnPlayerDisconnected;

    // Player
    public static UnityAction<PlayerRole> OnRoleAssigned;

    // Branch
    public static UnityAction OnBranchPickedUp;
    public static UnityAction OnBranchDropped;

    // Altar
    public static UnityAction OnAltarActivated;

    // Puzzles
    public static UnityAction<int> OnPuzzleCompleted; // int = puzzle index
    public static UnityAction<int> OnLoadNextPuzzle;
}