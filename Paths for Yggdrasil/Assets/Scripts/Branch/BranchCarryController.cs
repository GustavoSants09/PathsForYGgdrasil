using UnityEngine;

public class BranchCarryController : MonoBehaviour
{
    [Header("Carry Constraints")]
    [SerializeField] private float carrySpeedMultiplier = 0.6f; // 60% da velocidade normal
    [SerializeField] private float carryJumpMultiplier = 0.5f;  // 50% da altura de pulo

    private PlayerController playerController;
    private BranchObject currentBranch;
    private bool isCarryingBranch = false;

    private float originalSpeed;
    private float originalJumpHeight;

    private void Start()
    {
        playerController = GetComponent<PlayerController>();

        if (playerController != null)
        {
            originalSpeed = playerController.MoveSpeed;
            originalJumpHeight = playerController.JumpHeight;
        }

        GameEvents.OnBranchPickedUp += OnBranchPickedUp;
        GameEvents.OnBranchDropped += OnBranchDropped;
    }

    private void OnBranchPickedUp()
    {
        // Verifica se este jogador está carregando
        // (lógica pode ser expandida com referência específica)
        isCarryingBranch = true;
        ApplyCarryConstraints();
    }

    private void OnBranchDropped()
    {
        isCarryingBranch = false;
        RemoveCarryConstraints();
    }

    private void ApplyCarryConstraints()
    {
        if (playerController == null) return;

        playerController.MoveSpeed = originalSpeed * carrySpeedMultiplier;
        playerController.JumpHeight = originalJumpHeight * carryJumpMultiplier;

        Debug.Log("[Carry] Movement constraints applied");
    }

    private void RemoveCarryConstraints()
    {
        if (playerController == null) return;

        playerController.MoveSpeed = originalSpeed;
        playerController.JumpHeight = originalJumpHeight;

        Debug.Log("[Carry] Movement constraints removed");
    }

    private void OnDestroy()
    {
        GameEvents.OnBranchPickedUp -= OnBranchPickedUp;
        GameEvents.OnBranchDropped -= OnBranchDropped;
    }
}