using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// Implementação básica de um botão interativo
/// Usado como base para puzzles e mecânicas de jogo
/// </summary>
public class InteractableButton : MonoBehaviourPun, IInteractable
{
    [Header("Configurações do Botão")]
    [Tooltip("ID único deste botão")]
    [SerializeField] private string buttonID;

    [Tooltip("Texto mostrado ao jogador quando pode interagir")]
    [SerializeField] private string interactionPrompt = "Pressione E para ativar";

    [Tooltip("Pode ser usado múltiplas vezes?")]
    [SerializeField] private bool isReusable = false;

    [Tooltip("Tempo de cooldown entre usos (se reusável)")]
    [SerializeField] private float cooldownTime = 2f;

    [Header("Feedback Visual")]
    [Tooltip("Renderer para mudar cor quando ativado")]
    [SerializeField] private Renderer buttonRenderer;

    [Tooltip("Cor quando inativo")]
    [SerializeField] private Color inactiveColor = Color.red;

    [Tooltip("Cor quando ativo")]
    [SerializeField] private Color activeColor = Color.green;

    // Estado do botão
    private bool isActivated = false;
    private float lastActivationTime;

    /// <summary>
    /// Inicialização
    /// </summary>
    void Start()
    {
        UpdateVisuals();
    }

    /// <summary>
    /// Retorna o prompt de interação
    /// </summary>
    public string GetInteractionPrompt()
    {
        return interactionPrompt;
    }

    /// <summary>
    /// Verifica se pode ser interagido
    /// </summary>
    public bool CanInteract()
    {
        // Se não é reusável e já foi ativado, não pode interagir
        if (!isReusable && isActivated)
        {
            return false;
        }

        // Se é reusável, verifica cooldown
        if (isReusable && Time.time - lastActivationTime < cooldownTime)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Executa a interação
    /// </summary>
    public void Interact(Player player)
    {
        if (!CanInteract())
        {
            return;
        }

        // Chama RPC para sincronizar ativação em todos os clientes
        photonView.RPC("RPC_ActivateButton", RpcTarget.All, player.ActorNumber);
    }

    /// <summary>
    /// RPC para ativar o botão em todos os clientes
    /// </summary>
    [PunRPC]
    private void RPC_ActivateButton(int actorNumber)
    {
        isActivated = true;
        lastActivationTime = Time.time;

        UpdateVisuals();

        // Notifica o GameManager sobre a ativação
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnButtonActivated(buttonID, actorNumber);
        }

        Debug.Log($"Botão {buttonID} ativado pelo jogador {actorNumber}");
    }

    /// <summary>
    /// Atualiza visuais do botão baseado no estado
    /// </summary>
    private void UpdateVisuals()
    {
        if (buttonRenderer != null)
        {
            buttonRenderer.material.color = isActivated ? activeColor : inactiveColor;
        }
    }

    /// <summary>
    /// Reseta o botão (chamado externamente se necessário)
    /// </summary>
    public void ResetButton()
    {
        photonView.RPC("RPC_ResetButton", RpcTarget.All);
    }

    /// <summary>
    /// RPC para resetar o botão em todos os clientes
    /// </summary>
    [PunRPC]
    private void RPC_ResetButton()
    {
        isActivated = false;
        UpdateVisuals();
    }
}