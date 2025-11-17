using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBarUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image healthFillImage;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Image warningOverlay;

    [Header("Visual Settings")]
    [SerializeField] private Gradient healthGradient;
    [SerializeField] private float warningThreshold = 0.25f; // 25%
    [SerializeField] private float warningPulseSpeed = 3f;

    private bool showWarning = false;

    private void Start()
    {
        if (YggdrasilHealthSystem.Instance != null)
        {
            YggdrasilHealthSystem.Instance.OnHealthChanged.AddListener(UpdateHealthBar);
        }

        if (warningOverlay != null)
        {
            warningOverlay.enabled = false;
        }
    }

    private void Update()
    {
        if (showWarning && warningOverlay != null)
        {
            // Animação de pulso de alerta
            float alpha = Mathf.Lerp(0.2f, 0.6f, Mathf.PingPong(Time.time * warningPulseSpeed, 1f));
            Color color = warningOverlay.color;
            color.a = alpha;
            warningOverlay.color = color;
        }
    }

    private void UpdateHealthBar(float current, float max)
    {
        float percentage = current / max;

        // Atualiza barra de vida
        if (healthFillImage != null)
        {
            healthFillImage.fillAmount = percentage;
            healthFillImage.color = healthGradient.Evaluate(percentage);
        }

        // Atualiza texto
        if (healthText != null)
        {
            int minutes = Mathf.FloorToInt(current / 60f);
            int seconds = Mathf.FloorToInt(current % 60f);
            healthText.text = $"{minutes:00}:{seconds:00}";
        }

        // Ativa alerta se vida baixa
        if (percentage <= warningThreshold && !showWarning)
        {
            showWarning = true;
            if (warningOverlay != null)
            {
                warningOverlay.enabled = true;
            }
        }
        else if (percentage > warningThreshold && showWarning)
        {
            showWarning = false;
            if (warningOverlay != null)
            {
                warningOverlay.enabled = false;
            }
        }
    }

    private void OnDestroy()
    {
        if (YggdrasilHealthSystem.Instance != null)
        {
            YggdrasilHealthSystem.Instance.OnHealthChanged.RemoveListener(UpdateHealthBar);
        }
    }
}