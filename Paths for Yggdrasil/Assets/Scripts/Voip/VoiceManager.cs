using UnityEngine;
using Photon.Voice.Unity;
using Photon.Voice.PUN;

/// <summary>
/// Gerencia sistema de voz usando Photon Voice
/// Implementa push-to-talk com tecla V
/// </summary>
public class VoiceManager : MonoBehaviour
{
    public static VoiceManager Instance { get; private set; }

    [Header("Configurações de Voz")]
    [SerializeField] private KeyCode pushToTalkKey = KeyCode.V;
    [SerializeField] private bool voiceEnabled = true;

    [Header("Referências")]
    private Recorder localRecorder;
    private bool voiceSystemReady = false;

    // Estado
    private bool isPushingToTalk = false;

    /// <summary>
    /// Singleton
    /// </summary>
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    /// Inicialização
    /// </summary>
    void Start()
    {
        Debug.Log("VoiceManager inicializado");
    }

    /// <summary>
    /// Update para detectar input de push-to-talk
    /// </summary>
    void Update()
    {
        if (!voiceSystemReady || localRecorder == null || !voiceEnabled)
            return;

        // Detecta quando jogador pressiona a tecla
        if (Input.GetKeyDown(pushToTalkKey))
        {
            StartTransmitting();
        }

        // Detecta quando jogador solta a tecla
        if (Input.GetKeyUp(pushToTalkKey))
        {
            StopTransmitting();
        }
    }

    /// <summary>
    /// Inicia transmissão de voz
    /// </summary>
    private void StartTransmitting()
    {
        if (isPushingToTalk) return;

        isPushingToTalk = true;
        localRecorder.TransmitEnabled = true;

        Debug.Log("Transmissão de voz iniciada");
    }

    /// <summary>
    /// Para transmissão de voz
    /// </summary>
    private void StopTransmitting()
    {
        if (!isPushingToTalk) return;

        isPushingToTalk = false;
        localRecorder.TransmitEnabled = false;

        Debug.Log("Transmissão de voz parada");
    }

    /// <summary>
    /// Configura o recorder local quando o jogador spawna
    /// </summary>
    public void SetupLocalRecorder(Recorder recorder)
    {
        if (recorder == null)
        {
            Debug.LogError("Recorder é null!");
            return;
        }

        localRecorder = recorder;

        // Configura recorder para push-to-talk
        localRecorder.TransmitEnabled = false; // Começa desabilitado

        voiceSystemReady = true;

        Debug.Log("Recorder local configurado para push-to-talk");
    }

    /// <summary>
    /// Ativa/desativa sistema de voz
    /// </summary>
    public void SetVoiceEnabled(bool enabled)
    {
        voiceEnabled = enabled;

        if (!enabled && isPushingToTalk)
        {
            StopTransmitting();
        }

        Debug.Log($"Sistema de voz {(enabled ? "ativado" : "desativado")}");
    }

    /// <summary>
    /// Retorna se está transmitindo voz
    /// </summary>
    public bool IsTransmitting()
    {
        return isPushingToTalk;
    }

    /// <summary>
    /// Muda a tecla de push-to-talk
    /// </summary>
    public void SetPushToTalkKey(KeyCode newKey)
    {
        pushToTalkKey = newKey;
        Debug.Log($"Tecla de push-to-talk alterada para: {newKey}");
    }
}