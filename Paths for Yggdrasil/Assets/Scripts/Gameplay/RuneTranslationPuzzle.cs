using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using System.Collections.Generic;

/// <summary>
/// Puzzle de tradução de runas nórdicas.
/// Explorer vê runas, Guide vê alfabeto normal e traduz por voz.
/// </summary>
public class RuneTranslationPuzzle : PuzzleBase
{
    [Header("Puzzle Elements")]
    [SerializeField] private TextMeshProUGUI runeDisplayText;
    [SerializeField] private TMP_InputField answerInputField;
    [SerializeField] private Button submitButton;

    [Header("Rune Configuration")]
    [SerializeField] private string correctAnswer = "THOR";
    [SerializeField] private int maxAttempts = 3;

    [Header("Visual Feedback")]
    [SerializeField] private Color correctColor = Color.green;
    [SerializeField] private Color incorrectColor = Color.red;

    private PhotonView photonView;
    private int currentAttempts = 0;

    // Dicionário de conversão letra → runa
    private Dictionary<char, string> runeMap = new Dictionary<char, string>()
    {
        {'A', "ᚨ"}, {'B', "ᛒ"}, {'C', "ᚲ"}, {'D', "ᛞ"}, {'E', "ᛖ"},
        {'F', "ᚠ"}, {'G', "ᚷ"}, {'H', "ᚺ"}, {'I', "ᛁ"}, {'J', "ᛃ"},
        {'K', "ᚲ"}, {'L', "ᛚ"}, {'M', "ᛗ"}, {'N', "ᚾ"}, {'O', "ᛟ"},
        {'P', "ᛈ"}, {'Q', "ᚲ"}, {'R', "ᚱ"}, {'S', "ᛊ"}, {'T', "ᛏ"},
        {'U', "ᚢ"}, {'V', "ᚡ"}, {'W', "ᚹ"}, {'X', "ᛪ"}, {'Y', "ᛃ"},
        {'Z', "ᛉ"}
    };

    #region Unity Lifecycle

    protected override void Awake()
    {
        base.Awake();
        photonView = GetComponent<PhotonView>();
    }

    protected override void Start()
    {
        base.Start();

        if (submitButton != null)
        {
            submitButton.onClick.AddListener(OnSubmitButtonClicked);
        }
    }

    #endregion

    #region Initialization

    protected override void InitializePuzzle()
    {
        LogDebug("Inicializando puzzle de runas");

        // Determina qual versão mostrar baseado no papel do player
        DeterminePlayerView();

        // Desabilita input se for Guide
        if (answerInputField != null)
        {
            answerInputField.interactable = IsExplorer();
        }
    }

    /// <summary>
    /// Determina qual versão do puzzle mostrar baseado no papel
    /// </summary>
    private void DeterminePlayerView()
    {
        GameObject localPlayer = PlayerManager.Instance?.GetLocalPlayer();
        if (localPlayer == null) return;

        NetworkPlayer networkPlayer = localPlayer.GetComponent<NetworkPlayer>();
        if (networkPlayer == null) return;

        if (networkPlayer.IsExplorer())
        {
            ShowRunicVersion();
        }
        else if (networkPlayer.IsGuide())
        {
            ShowNormalVersion();
        }
    }

    /// <summary>
    /// Mostra versão com runas (para Explorer)
    /// </summary>
    private void ShowRunicVersion()
    {
        if (runeDisplayText != null)
        {
            string runicText = ConvertToRunes(correctAnswer);
            runeDisplayText.text = runicText;
            LogDebug($"Mostrando versão rúnica: {runicText}");
        }
    }

    /// <summary>
    /// Mostra versão normal (para Guide)
    /// </summary>
    private void ShowNormalVersion()
    {
        if (runeDisplayText != null)
        {
            runeDisplayText.text = correctAnswer;
            LogDebug($"Mostrando versão normal: {correctAnswer}");
        }
    }

    /// <summary>
    /// Converte texto para runas
    /// </summary>
    private string ConvertToRunes(string text)
    {
        string result = "";
        foreach (char c in text.ToUpper())
        {
            if (runeMap.ContainsKey(c))
            {
                result += runeMap[c];
            }
            else
            {
                result += c;
            }
        }
        return result;
    }

    #endregion

    #region Solution Check

    protected override bool CheckSolution()
    {
        if (answerInputField == null) return false;

        string playerAnswer = answerInputField.text.Trim().ToUpper();
        bool isCorrect = playerAnswer == correctAnswer.ToUpper();

        LogDebug($"Verificando resposta: '{playerAnswer}' vs '{correctAnswer}' = {isCorrect}");

        return isCorrect;
    }

    #endregion

    #region Input Handling

    /// <summary>
    /// Handler do botão de submit
    /// </summary>
    private void OnSubmitButtonClicked()
    {
        currentAttempts++;

        if (CheckSolution())
        {
            OnCorrectAnswer();
        }
        else
        {
            OnIncorrectAnswer();
        }
    }

    /// <summary>
    /// Callback de resposta correta
    /// </summary>
    private void OnCorrectAnswer()
    {
        LogDebug("✓ Resposta correta!");

        if (runeDisplayText != null)
        {
            runeDisplayText.color = correctColor;
        }

        CompletePuzzle();
    }

    /// <summary>
    /// Callback de resposta incorreta
    /// </summary>
    private void OnIncorrectAnswer()
    {
        LogDebug($"✗ Resposta incorreta (Tentativa {currentAttempts}/{maxAttempts})");

        if (runeDisplayText != null)
        {
            runeDisplayText.color = incorrectColor;
        }

        // Registra erro no GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterError();
        }

        // Verifica se excedeu tentativas máximas
        if (currentAttempts >= maxAttempts)
        {
            LogDebug("Tentativas máximas excedidas!");
            // Pode adicionar penalidade ou forçar reset
        }

        // Limpa input para nova tentativa
        if (answerInputField != null)
        {
            answerInputField.text = "";
        }

        OnPuzzleFailed?.Invoke();
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Verifica se o player local é Explorer
    /// </summary>
    private bool IsExplorer()
    {
        GameObject localPlayer = PlayerManager.Instance?.GetLocalPlayer();
        if (localPlayer == null) return false;

        NetworkPlayer networkPlayer = localPlayer.GetComponent<NetworkPlayer>();
        return networkPlayer != null && networkPlayer.IsExplorer();
    }

    #endregion

    #region Reset

    public override void ResetPuzzle()
    {
        currentAttempts = 0;
        isSolved = false;
        isActive = false;

        if (answerInputField != null)
        {
            answerInputField.text = "";
        }

        if (runeDisplayText != null)
        {
            runeDisplayText.color = Color.white;
        }

        InitializePuzzle();
        LogDebug("Puzzle resetado");
    }

    #endregion
}