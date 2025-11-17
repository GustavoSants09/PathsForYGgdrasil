using Photon.Pun;
using UnityEngine;

public class PuzzleSequencer : MonoBehaviourPun
{
    public static PuzzleSequencer Instance { get; private set; }

    [Header("Puzzle References")]
    [SerializeField] private RunePuzzleController puzzle1;
    [SerializeField] private MazePuzzleController puzzle2;
    [SerializeField] private ParkourPuzzleController puzzle3;

    private int currentPuzzleIndex = 1;
    private PuzzleController currentPuzzle;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        GameEvents.OnGameStart += StartFirstPuzzle;
        GameEvents.OnLoadNextPuzzle += LoadPuzzle;
    }

    private void StartFirstPuzzle()
    {
        LoadPuzzle(1);
    }

    public void LoadPuzzle(int puzzleIndex)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        photonView.RPC("RPC_LoadPuzzle", RpcTarget.All, puzzleIndex);
    }

    [PunRPC]
    private void RPC_LoadPuzzle(int puzzleIndex)
    {
        // Desativa puzzle anterior
        currentPuzzle?.DeactivatePuzzle();

        currentPuzzleIndex = puzzleIndex;

        switch (puzzleIndex)
        {
            case 1:
                currentPuzzle = puzzle1;
                break;
            case 2:
                currentPuzzle = puzzle2;
                break;
            case 3:
                currentPuzzle = puzzle3;
                break;
            default:
                Debug.LogError($"[Sequencer] Invalid puzzle index: {puzzleIndex}");
                return;
        }

        currentPuzzle.InitializePuzzle();
        currentPuzzle.ActivatePuzzle();

        Debug.Log($"[Sequencer] Loaded Puzzle {puzzleIndex}");
    }

    private void OnDestroy()
    {
        GameEvents.OnGameStart -= StartFirstPuzzle;
        GameEvents.OnLoadNextPuzzle -= LoadPuzzle;
    }
}