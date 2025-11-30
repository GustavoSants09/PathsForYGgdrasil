using UnityEngine;
using Photon.Pun;
using Photon.Voice.PUN;

/// <summary>
/// Gerencia a inicialização e configuração do Photon Voice 2 integrado com PUN.
/// O PunVoiceClient conecta automaticamente quando PUN entra na sala.
/// </summary>
public class VoiceManager : MonoBehaviourPunCallbacks
{
    [Header("Referências")]
    [SerializeField] private PunVoiceClient punVoiceClient;

    private static VoiceManager instance;

    private void Awake()
    {
        // Singleton pattern
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        // Obter referência ao PunVoiceClient
        if (punVoiceClient == null)
        {
            punVoiceClient = GetComponent<PunVoiceClient>();
        }

        if (punVoiceClient == null)
        {
            Debug.LogError("❌ PunVoiceClient não encontrado! Adicione o componente.");
            return;
        }
    }

    private void Start()
    {
        Debug.Log("✅ VoiceManager inicializado. Aguardando conexão PUN...");
    }

    /// <summary>
    /// Chamado quando entra em uma sala PUN.
    /// PunVoiceClient conecta automaticamente à sala de voz.
    /// </summary>
    public override void OnJoinedRoom()
    {
        Debug.Log($"🎤 Voice conectado à sala: {PhotonNetwork.CurrentRoom.Name}");
        Debug.Log($"📡 Estado Voice Client: {punVoiceClient.Client.State}");
    }

    /// <summary>
    /// Chamado quando sai da sala.
    /// </summary>
    public override void OnLeftRoom()
    {
        Debug.Log("🔇 Voice desconectado da sala");
    }

    /// <summary>
    /// Retorna a instância singleton.
    /// </summary>
    public static VoiceManager Instance => instance;

    /// <summary>
    /// Retorna o PunVoiceClient para acesso direto.
    /// </summary>
    public PunVoiceClient GetPunVoiceClient() => punVoiceClient;
}