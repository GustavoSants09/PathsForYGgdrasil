using UnityEngine;
using Photon.Pun;
using Photon.Voice.Unity;
using UnityEngine.UI;

/// <summary>
/// Controla sistema Push-to-Talk para Photon Voice 2.
/// Gerencia apenas TransmitEnabled mantendo RecordingEnabled sempre ativo.
/// </summary>
[RequireComponent(typeof(Recorder))]
public class PushToTalkController : MonoBehaviourPun
{
    [Header("Configurações Push-to-Talk")]
    [SerializeField] private KeyCode pushToTalkKey = KeyCode.V;

    [Header("Referências")]
    [SerializeField] private Recorder voiceRecorder;

    [Header("UI Feedback (Opcional)")]
    [SerializeField] private Image voiceIndicator;
    [SerializeField] private Color transmittingColor = Color.green;
    [SerializeField] private Color mutedColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);

    private bool isTransmitting = false;

    private void Start()
    {
        // Obter Recorder se não atribuído
        if (voiceRecorder == null)
        {
            voiceRecorder = GetComponent<Recorder>();
        }

        // Apenas o jogador local controla seu próprio PTT
        if (!photonView.IsMine)
        {
            this.enabled = false;
            return;
        }

        // Configuração inicial para Push-to-Talk
        if (voiceRecorder != null)
        {
            voiceRecorder.RecordingEnabled = true;  // Sempre gravando
            voiceRecorder.TransmitEnabled = false;   // Não transmitindo por padrão

            Debug.Log($"✅ Push-to-Talk configurado. Tecla: {pushToTalkKey}");
        }
        else
        {
            Debug.LogError("❌ Recorder não encontrado!");
        }

        // Atualizar indicador visual inicial
        UpdateVoiceIndicator();
    }

    private void Update()
    {
        // Detectar pressão do botão PTT
        if (Input.GetKeyDown(pushToTalkKey))
        {
            StartTransmitting();
        }

        // Detectar liberação do botão PTT
        if (Input.GetKeyUp(pushToTalkKey))
        {
            StopTransmitting();
        }
    }

    /// <summary>
    /// Inicia a transmissão de áudio.
    /// </summary>
    private void StartTransmitting()
    {
        if (voiceRecorder == null || isTransmitting) return;

        voiceRecorder.TransmitEnabled = true;
        isTransmitting = true;

        UpdateVoiceIndicator();
        Debug.Log("🎤 Transmitindo áudio");
    }

    /// <summary>
    /// Para a transmissão de áudio.
    /// </summary>
    private void StopTransmitting()
    {
        if (voiceRecorder == null || !isTransmitting) return;

        voiceRecorder.TransmitEnabled = false;
        isTransmitting = false;

        UpdateVoiceIndicator();
        Debug.Log("🔇 Transmissão parada");
    }

    /// <summary>
    /// Atualiza indicador visual de transmissão.
    /// </summary>
    private void UpdateVoiceIndicator()
    {
        if (voiceIndicator == null) return;

        voiceIndicator.color = isTransmitting ? transmittingColor : mutedColor;
    }

    /// <summary>
    /// Retorna se está transmitindo atualmente.
    /// </summary>
    public bool IsTransmitting => isTransmitting;

    /// <summary>
    /// Permite alterar a tecla PTT em runtime.
    /// </summary>
    public void SetPushToTalkKey(KeyCode newKey)
    {
        pushToTalkKey = newKey;
        Debug.Log($"🔑 Tecla PTT alterada para: {newKey}");
    }

    /// <summary>
    /// Força parada da transmissão (útil para menus/pause).
    /// </summary>
    public void ForceStopTransmit()
    {
        if (isTransmitting)
        {
            StopTransmitting();
        }
    }
}