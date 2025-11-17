using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RadioUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image transmissionIndicator;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Color idleColor = Color.gray;
    [SerializeField] private Color transmittingColor = Color.green;
    [SerializeField] private Color listeningColor = Color.yellow;

    [Header("Animation")]
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseMin = 0.5f;
    [SerializeField] private float pulseMax = 1f;

    private PlayerRole localPlayerRole;
    private bool isLocalTransmitting = false;

    private void Start()
    {
        GameEvents.OnRoleAssigned += SetLocalRole;

        if (RadioVOIPManager.Instance != null)
        {
            RadioVOIPManager.Instance.OnTransmissionStart.AddListener(OnTransmissionStarted);
            RadioVOIPManager.Instance.OnTransmissionEnd.AddListener(OnTransmissionEnded);
        }

        UpdateUI(PlayerRole.None);
    }

    private void Update()
    {
        if (isLocalTransmitting)
        {
            // Animação de pulso durante transmissão
            float scale = Mathf.Lerp(pulseMin, pulseMax, Mathf.PingPong(Time.time * pulseSpeed, 1f));
            transmissionIndicator.transform.localScale = Vector3.one * scale;
        }
        else
        {
            transmissionIndicator.transform.localScale = Vector3.one;
        }
    }

    private void SetLocalRole(PlayerRole role)
    {
        localPlayerRole = role;
    }

    private void OnTransmissionStarted(PlayerRole transmitter)
    {
        if (transmitter == localPlayerRole)
        {
            // Jogador local está transmitindo
            isLocalTransmitting = true;
            transmissionIndicator.color = transmittingColor;
            statusText.text = "TRANSMITTING";
        }
        else
        {
            // Outro jogador está transmitindo (escutando)
            isLocalTransmitting = false;
            transmissionIndicator.color = listeningColor;
            statusText.text = $"LISTENING TO {transmitter}";
        }
    }

    private void OnTransmissionEnded(PlayerRole transmitter)
    {
        isLocalTransmitting = false;
        UpdateUI(PlayerRole.None);
    }

    private void UpdateUI(PlayerRole transmitter)
    {
        transmissionIndicator.color = idleColor;
        statusText.text = "RADIO IDLE - HOLD [T] TO TALK";
    }

    private void OnDestroy()
    {
        GameEvents.OnRoleAssigned -= SetLocalRole;

        if (RadioVOIPManager.Instance != null)
        {
            RadioVOIPManager.Instance.OnTransmissionStart.RemoveListener(OnTransmissionStarted);
            RadioVOIPManager.Instance.OnTransmissionEnd.RemoveListener(OnTransmissionEnded);
        }
    }
}