using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;

public class YggdrasilHealthSystem : MonoBehaviourPun
{
    public static YggdrasilHealthSystem Instance { get; private set; }

    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 300f; // 5 minutos base
    [SerializeField] private float drainRate = 1f; // HP por segundo
    [SerializeField] private float restoreAmount = 60f; // +1 minuto por puzzle

    [Header("Current State")]
    private float currentHealth;
    private bool isDraining = false;

    [Header("Events")]
    public UnityEvent<float, float> OnHealthChanged; // current, max
    public UnityEvent OnHealthDepleted;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthPercentage => currentHealth / maxHealth;

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
        currentHealth = maxHealth;
        GameEvents.OnGameStart += StartDraining;
        GameEvents.OnAltarActivated += RestoreHealth;
    }

    private void Update()
    {
        if (!PhotonNetwork.IsMasterClient) return; // Apenas Master controla drenagem
        if (!isDraining) return;

        currentHealth -= drainRate * Time.deltaTime;
        currentHealth = Mathf.Max(0f, currentHealth);

        // Sincroniza via RPC a cada frame (otimizar se necessário)
        photonView.RPC("RPC_SyncHealth", RpcTarget.Others, currentHealth);

        // Notifica mudança local
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
        {
            TriggerGameOver();
        }
    }

    public void StartDraining()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        isDraining = true;
        Debug.Log("[Yggdrasil] Health draining started");
    }

    public void RestoreHealth()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        currentHealth = Mathf.Min(currentHealth + restoreAmount, maxHealth);
        photonView.RPC("RPC_SyncHealth", RpcTarget.All, currentHealth);

        Debug.Log($"[Yggdrasil] Health restored: {currentHealth}/{maxHealth}");
    }

    [PunRPC]
    private void RPC_SyncHealth(float newHealth)
    {
        currentHealth = newHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void TriggerGameOver()
    {
        isDraining = false;
        photonView.RPC("RPC_GameOver", RpcTarget.All);
    }

    [PunRPC]
    private void RPC_GameOver()
    {
        Debug.LogWarning("[Yggdrasil] Health depleted! Game Over!");
        OnHealthDepleted?.Invoke();
        GameEvents.OnGameOver?.Invoke();
    }

    private void OnDestroy()
    {
        GameEvents.OnGameStart -= StartDraining;
        GameEvents.OnAltarActivated -= RestoreHealth;
    }
}