using Photon.Pun;
using UnityEngine;
using System.Collections.Generic;

public class RunePuzzleController : PuzzleController
{
    [Header("Rune Settings")]
    [SerializeField] private List<RuneData> runeSequence; // Sequência correta
    [SerializeField] private RuneAlphabetDisplay alphabetDisplay; // Player A
    [SerializeField] private RuneInputPanel inputPanel; // Player B

    private List<RuneData> playerInput = new List<RuneData>();

    public override void InitializePuzzle()
    {
        base.InitializePuzzle();

        // Gera sequência aleatória (ou fixa)
        GenerateRuneSequence();

        // Exibe alfabeto para Player A
        alphabetDisplay?.ShowAlphabet();

        // Exibe runas não-traduzidas para Player B
        inputPanel?.SetRuneSequence(runeSequence);
    }

    private void GenerateRuneSequence()
    {
        // Exemplo: sequência fixa de 4 runas
        runeSequence = new List<RuneData>
        {
            RuneDatabase.GetRune("Fehu"),
            RuneDatabase.GetRune("Uruz"),
            RuneDatabase.GetRune("Thurisaz"),
            RuneDatabase.GetRune("Ansuz")
        };
    }

    public void SubmitAnswer(List<RuneData> input)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        playerInput = input;

        if (CheckSolution())
        {
            OnPuzzleCompleted();
        }
        else
        {
            photonView.RPC("RPC_WrongAnswer", RpcTarget.All);
        }
    }

    public override bool CheckSolution()
    {
        if (playerInput.Count != runeSequence.Count) return false;

        for (int i = 0; i < runeSequence.Count; i++)
        {
            if (playerInput[i].runeName != runeSequence[i].runeName)
            {
                return false;
            }
        }

        return true;
    }

    [PunRPC]
    private void RPC_WrongAnswer()
    {
        Debug.Log("[Puzzle 1] Wrong answer! Try again.");
        inputPanel?.ShowFeedback(false);
    }
}

[System.Serializable]
public class RuneData
{
    public string runeName;
    public Sprite runeSymbol;
    public string translation;
}