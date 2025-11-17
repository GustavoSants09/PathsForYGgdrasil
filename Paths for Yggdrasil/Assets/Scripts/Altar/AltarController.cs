using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;

public class AltarController : MonoBehaviourPun
{
    [Header("Settings")]
    [SerializeField] private int puzzleIndex; // 1, 2 ou 3
    [SerializeField] private Transform branchPlacementPoint;
    [SerializeField] private GameObject altarVisualEffect;

    [Header("State")]
    private bool isActive = false;
    private bool branchPlaced = false;

    [Header("Events")]
    public UnityEvent OnAltarEnabled;
    public UnityEvent OnBranchPlaced;

    private void Start()
    {
        SetAltarActive(false);
        GameEvents.OnPuzzleCompleted += OnPuzzleCompleted;
    }

    private void OnPuzzleCompleted(int completedPuzzleIndex)
    {
        if (completedPuzzleIndex == puzzleIndex)
        {
            EnableAltar();
        }
    }

    private void EnableAltar()
    {
        isActive = true;

        if (altarVisualEffect != null)
        {
            altarVisualEffect.SetActive(true);
        }

        OnAltarEnabled?.Invoke();
        Debug.Log($"[Altar {puzzleIndex}] Enabled and ready to receive branch");
    }

    private void SetAltarActive(bool active)
    {
        isActive = active;

        if (altarVisualEffect != null)
        {
            altarVisualEffect.SetActive(active);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive || branchPlaced) return;

        BranchObject branch = other.GetComponent<BranchObject>();
        if (branch != null)
        {
            PlaceBranch(branch);
        }
    }

    private void PlaceBranch(BranchObject branch)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        photonView.RPC("RPC_PlaceBranch", RpcTarget.AllBuffered);

        // Destrói o galho após colocação
        PhotonNetwork.Destroy(branch.gameObject);
    }

    [PunRPC]
    private void RPC_PlaceBranch()
    {
        branchPlaced = true;
        isActive = false;

        OnBranchPlaced?.Invoke();
        GameEvents.OnAltarActivated?.Invoke();

        Debug.Log($"[Altar {puzzleIndex}] Branch placed! Restoring Yggdrasil health...");

        // Trigger próximo puzzle após delay
        if (PhotonNetwork.IsMasterClient)
        {
            Invoke(nameof(LoadNextPuzzle), 2f);
        }
    }

    private void LoadNextPuzzle()
    {
        int nextPuzzleIndex = puzzleIndex + 1;

        if (nextPuzzleIndex <= 3) // 3 puzzles no total
        {
            GameEvents.OnLoadNextPuzzle?.Invoke(nextPuzzleIndex);
        }
        else
        {
            // Todos os puzzles completados - vitória!
            photonView.RPC("RPC_Victory", RpcTarget.All);
        }
    }

    [PunRPC]
    private void RPC_Victory()
    {
        Debug.Log("[Game] All puzzles completed! Victory!");
        GameEvents.OnVictory?.Invoke();
    }

    private void OnDestroy()
    {
        GameEvents.OnPuzzleCompleted -= OnPuzzleCompleted;
    }
}