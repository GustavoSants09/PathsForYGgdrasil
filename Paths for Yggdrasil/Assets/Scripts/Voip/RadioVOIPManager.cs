using Photon.Pun;
using Photon.Voice.Unity;
using UnityEngine;
using UnityEngine.Events;

public class RadioVOIPManager : MonoBehaviourPun
{
    public static RadioVOIPManager Instance { get; private set; }

    [Header("Voice Settings")]
    [SerializeField] private Recorder voiceRecorder;
    [SerializeField] private KeyCode pushToTalkKey = KeyCode.T;

    [Header("Turn Control")]
    private PlayerRole currentTransmitter = PlayerRole.None;
    private float transmissionTimeout = 10f; // Timeout automático
    private float currentTransmissionTime = 0f;

    [Header("Events")]
    public UnityEvent<PlayerRole> OnTransmissionStart;
    public UnityEvent<PlayerRole> OnTransmissionEnd;

    private PlayerRole localPlayerRole;
    private bool isTransmitting = false;

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
        // Desabilita recorder por padrão
        if (voiceRecorder != null)
        {
            voiceRecorder.TransmitEnabled = false;
        }

        GameEvents.OnRoleAssigned += SetLocalPlayerRole;
    }

    private void Update()
    {
        if (localPlayerRole == PlayerRole.None) return;

        // Detecta input de push-to-talk
        if (Input.GetKeyDown(pushToTalkKey))
        {
            RequestTransmission();
        }
        else if (Input.GetKeyUp(pushToTalkKey))
        {
            EndTransmission();
        }

        // Timeout automático
        if (isTransmitting)
        {
            currentTransmissionTime += Time.deltaTime;
            if (currentTransmissionTime >= transmissionTimeout)
            {
                Debug.LogWarning("[Radio] Transmission timeout!");
                EndTransmission();
            }
        }
    }

    private void SetLocalPlayerRole(PlayerRole role)
    {
        localPlayerRole = role;
        Debug.Log($"[Radio] Local player role set to: {role}");
    }

    private void RequestTransmission()
    {
        // Verifica se outro jogador já está transmitindo
        if (currentTransmitter != PlayerRole.None && currentTransmitter != localPlayerRole)
        {
            Debug.Log($"[Radio] Cannot transmit. {currentTransmitter} is currently speaking.");
            // TODO: Feedback visual/áudio de "canal ocupado"
            return;
        }

        // Ativa transmissão
        isTransmitting = true;
        currentTransmissionTime = 0f;

        if (voiceRecorder != null)
        {
            voiceRecorder.TransmitEnabled = true;
        }

        // Sincroniza com rede
        photonView.RPC("RPC_SetTransmitter", RpcTarget.All, localPlayerRole);

        Debug.Log($"[Radio] {localPlayerRole} started transmitting");
    }

    private void EndTransmission()
    {
        if (!isTransmitting) return;

        isTransmitting = false;
        currentTransmissionTime = 0f;

        if (voiceRecorder != null)
        {
            voiceRecorder.TransmitEnabled = false;
        }

        // Sincroniza com rede
        photonView.RPC("RPC_ClearTransmitter", RpcTarget.All);

        Debug.Log($"[Radio] {localPlayerRole} stopped transmitting");
    }

    [PunRPC]
    private void RPC_SetTransmitter(PlayerRole transmitter)
    {
        currentTransmitter = transmitter;
        OnTransmissionStart?.Invoke(transmitter);
    }

    [PunRPC]
    private void RPC_ClearTransmitter()
    {
        PlayerRole previousTransmitter = currentTransmitter;
        currentTransmitter = PlayerRole.None;
        OnTransmissionEnd?.Invoke(previousTransmitter);
    }

    private void OnDestroy()
    {
        GameEvents.OnRoleAssigned -= SetLocalPlayerRole;
    }
}