using Photon.Pun;
using UnityEngine;
using System.Collections.Generic;

public class ParkourPuzzleController : PuzzleController
{
    [Header("Parkour Settings")]
    [SerializeField] private LeverSequenceController leverController; // Player A
    [SerializeField] private List<PlatformSyncController> platforms; // Player B
    [SerializeField] private List<int> correctLeverSequence; // Ex: [1, 3, 2, 4]

    private int currentLeverIndex = 0;

    public override void InitializePuzzle()
    {
        base.InitializePuzzle();

        // Inicializa alavancas para Player A
        leverController?.InitializeLevers(correctLeverSequence.Count);

        // Inicializa plataformas para Player B (todas invisíveis)
        foreach (var platform in platforms)
        {
            platform.SetVisible(false);
        }
    }

    public void OnLeverPulled(int leverID)
    {
        if (!isActive) return;

        photonView.RPC("RPC_LeverPulled", RpcTarget.All, leverID);
    }

    [PunRPC]
    private void RPC_LeverPulled(int leverID)
    {
        if (leverID == correctLeverSequence[currentLeverIndex])
        {
            // Alavanca correta - ativa plataforma correspondente
            platforms[currentLeverIndex].SetVisible(true);
            currentLeverIndex++;

            Debug.Log($"[Puzzle 3] Correct lever {currentLeverIndex}/{correctLeverSequence.Count}");

            if (currentLeverIndex >= correctLeverSequence.Count)
            {
                OnPuzzleCompleted();
            }
        }
        else
        {
            // Alavanca errada - reseta todas as plataformas
            ResetPuzzle();
        }
    }

    private void ResetPuzzle()
    {
        currentLeverIndex = 0;

        foreach (var platform in platforms)
        {
            platform.SetVisible(false);
        }

        Debug.LogWarning("[Puzzle 3] Wrong lever! Resetting...");
    }

    public override bool CheckSolution()
    {
        return currentLeverIndex >= correctLeverSequence.Count;
    }
}