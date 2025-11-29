using UnityEngine;
using Photon.Pun;
using TMPro;

/// <summary>
/// Sistema de interação do jogador com objetos interativos do mundo
/// Usa raycast para detectar objetos interativos e permite ativá-los
/// </summary>
public class PlayerInteraction : MonoBehaviourPun
{
    [Header("Referências")]
    [Tooltip("Transform da câmera para fazer raycast")]
    [SerializeField] private Transform cameraTransform;

    [Tooltip("UI Text para mostrar prompt de interação")]
    [SerializeField] private TextMeshProUGUI interactionPromptText;

    [Header("Configurações de Interação")]
    [Tooltip("Distância máxima de interação")]
    [SerializeField] private float interactionRange = 3f;

    [Tooltip("Layers que podem ser interagidos")]
    [SerializeField] private LayerMask interactionLayer;

    [Tooltip("Tecla de interação")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;

    // Objeto atualmente sob o crosshair
    private IInteractable currentInteractable;

    /// <summary>
    /// Inicialização
    /// </summary>
    void Start()
    {
        // Desativa prompt inicialmente
        if (interactionPromptText != null)
        {
            interactionPromptText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Atualização a cada frame
    /// </summary>
    void Update()
    {
        // Só processa se for o jogador local
        if (photonView.IsMine == false && PhotonNetwork.IsConnected == true)
        {
            return;
        }

        // Verifica objetos interativos
        CheckForInteractables();

        // Processa input de interação
        if (Input.GetKeyDown(interactionKey) && currentInteractable != null)
        {
            InteractWithObject();
        }
    }

    /// <summary>
    /// Verifica se há objetos interativos sob o crosshair
    /// </summary>
    private void CheckForInteractables()
    {
        RaycastHit hit;

        // Faz raycast a partir do centro da câmera
        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out hit, interactionRange, interactionLayer))
        {
            // Tenta obter componente IInteractable do objeto atingido
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                // Novo objeto interativo detectado
                if (currentInteractable != interactable)
                {
                    currentInteractable = interactable;
                    ShowInteractionPrompt(interactable.GetInteractionPrompt());
                }
            }
            else
            {
                ClearInteractable();
            }
        }
        else
        {
            ClearInteractable();
        }
    }

    /// <summary>
    /// Interage com o objeto atual
    /// </summary>
    private void InteractWithObject()
    {
        if (currentInteractable != null && currentInteractable.CanInteract())
        {
            currentInteractable.Interact(photonView.Owner);
        }
    }

    /// <summary>
    /// Mostra prompt de interação na tela
    /// </summary>
    private void ShowInteractionPrompt(string prompt)
    {
        if (interactionPromptText != null)
        {
            interactionPromptText.text = prompt;
            interactionPromptText.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Limpa o objeto interativo atual
    /// </summary>
    private void ClearInteractable()
    {
        currentInteractable = null;

        if (interactionPromptText != null)
        {
            interactionPromptText.gameObject.SetActive(false);
        }
    }
}