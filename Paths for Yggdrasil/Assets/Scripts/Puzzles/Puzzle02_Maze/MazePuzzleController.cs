using Photon.Pun;
using UnityEngine;
using System.Collections.Generic;

public class MazePuzzleController : PuzzleController
{
    [Header("Maze Settings")]
    [SerializeField] private MazeMapDisplay mapDisplay; // Player A
    [SerializeField] private InvisiblePathController pathController; // Player B
    [SerializeField] private List<Vector2Int> correctPath; // Caminho correto

    private int currentStep = 0;

    public override void InitializePuzzle()
    {
        base.InitializePuzzle();

        // Define caminho correto (pode ser fixo ou gerado)
        DefineCorrectPath();

        // Mostra mapa para Player A
        mapDisplay?.ShowMap(correctPath);

        // Inicializa caminho invisível para Player B
        pathController?.InitializePath(correctPath);
    }

    private void DefineCorrectPath()
    {
        // Exemplo: caminho pré-definido
        correctPath = new List<Vector2Int>
        {
            new Vector2Int(0, 0),
            new Vector2Int(0, 1),
            new Vector2Int(1, 1),
            new Vector2Int(1, 2),
            new Vector2Int(2, 2)
        };
    }

    public void ValidateStep(Vector2Int playerPosition)
    {
        if (!isActive) return;

        if (currentStep < correctPath.Count && playerPosition == correctPath[currentStep])
        {
            currentStep++;
            photonView.RPC("RPC_CorrectStep", RpcTarget.All, currentStep);

            if (currentStep >= correctPath.Count)
            {
                OnPuzzleCompleted();
            }
        }
        else
        {
            // Passo errado - Player B morre e reseta
            photonView.RPC("RPC_PlayerFell", RpcTarget.All);
        }
    }

    [PunRPC]
    private void RPC_CorrectStep(int step)
    {
        Debug.Log($"[Puzzle 2] Correct step {step}/{correctPath.Count}");
        pathController?.RevealStep(step);
    }

    [PunRPC]
    private void RPC_PlayerFell()
    {
        Debug.LogWarning("[Puzzle 2] Player B fell! Resetting...");
        currentStep = 0;
        pathController?.ResetPlayer();
    }

    public override bool CheckSolution()
    {
        return currentStep >= correctPath.Count;
    }
}