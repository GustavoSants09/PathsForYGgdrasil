using UnityEngine;
using UnityEngine.Events;
using Photon.Pun;

/// <summary>
/// Objeto que pode ser interagido por players.
/// Suporta diferentes tipos de interação e restrições por papel.
/// </summary>
public class InteractableObject : MonoBehaviourPun
{
    [Header("Interaction Settings")]
    [SerializeField] private string interactionPrompt = "Pressione E para interagir";
    [SerializeField] private float interactionRange = 2f;
    [SerializeField] private bool requiresExplorer = false;
    [SerializeField] private bool requiresGuide = false;
    [SerializeField] private bool isOneTimeUse = false;

    [Header("Visual Feedback")]
    [SerializeField] private GameObject highlightObject;
    [SerializeField] private Color highlightColor = Color.yellow;

    [Header("Events")]
    public UnityEvent OnInteract;
    public UnityEvent OnHighlight;
    public UnityEvent OnUnhighlight;

    private bool isHighlighted = false;
    private bool hasBeenUsed = false;
    private Renderer[] renderers;
    private Color[] originalColors;

    #region Unity Lifecycle

    private void Awake()
    {
        // Guarda referências de renderers para highlight
        if (highlightObject != null)
        {
            renderers = highlightObject.GetComponents<Renderer>();
        }
        else
        {
            renderers = GetComponentsInChildren<Renderer>();
        }

        if (renderers.Length > 0)
        {
            originalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                originalColors[i] = renderers[i].material.color;
            }
        }
    }

    #endregion

    #region Interaction

    /// <summary>
    /// Tenta interagir com o objeto
    /// </summary>
    public void Interact(GameObject interactor)
    {
        // Verifica se já foi usado (se for one-time)
        if (isOneTimeUse && hasBeenUsed)
        {
            Debug.Log($"[InteractableObject] {gameObject.name} já foi usado");
            return;
        }

        // Verifica restrições de papel
        NetworkPlayer player = interactor.GetComponent<NetworkPlayer>();
        if (player != null)
        {
            if (requiresExplorer && !player.IsExplorer())
            {
                Debug.Log($"[InteractableObject] {gameObject.name} requer Explorer");
                return;
            }

            if (requiresGuide && !player.IsGuide())
            {
                Debug.Log($"[InteractableObject] {gameObject.name} requer Guide");
                return;
            }
        }

        // Executa interação
        Debug.Log($"[InteractableObject] {gameObject.name} interagido por {interactor.name}");

        if (PhotonNetwork.IsConnected && photonView != null)
        {
            photonView.RPC("RPC_OnInteract", RpcTarget.All);
        }
        else
        {
            ExecuteInteraction();
        }

        if (isOneTimeUse)
        {
            hasBeenUsed = true;
        }
    }

    [PunRPC]
    private void RPC_OnInteract()
    {
        ExecuteInteraction();
    }

    private void ExecuteInteraction()
    {
        OnInteract?.Invoke();
    }

    /// <summary>
    /// Verifica se o player pode interagir com este objeto
    /// </summary>
    public bool CanInteract(GameObject interactor)
    {
        if (isOneTimeUse && hasBeenUsed)
        {
            return false;
        }

        NetworkPlayer player = interactor.GetComponent<NetworkPlayer>();
        if (player == null)
        {
            return true;
        }

        if (requiresExplorer && !player.IsExplorer())
        {
            return false;
        }

        if (requiresGuide && !player.IsGuide())
        {
            return false;
        }

        return true;
    }

    #endregion

    #region Highlight

    /// <summary>
    /// Destaca o objeto visualmente
    /// </summary>
    public void Highlight()
    {
        if (isHighlighted) return;

        isHighlighted = true;

        if (renderers != null && renderers.Length > 0)
        {
            foreach (var renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.material.color = highlightColor;
                }
            }
        }

        OnHighlight?.Invoke();
    }

    /// <summary>
    /// Remove o destaque visual do objeto
    /// </summary>
    public void Unhighlight()
    {
        if (!isHighlighted) return;

        isHighlighted = false;

        if (renderers != null && originalColors != null)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && i < originalColors.Length)
                {
                    renderers[i].material.color = originalColors[i];
                }
            }
        }

        OnUnhighlight?.Invoke();
    }

    #endregion

    #region Properties

    public string InteractionPrompt => interactionPrompt;
    public float InteractionRange => interactionRange;
    public bool HasBeenUsed => hasBeenUsed;

    #endregion

    #region Debug

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }

    #endregion
}