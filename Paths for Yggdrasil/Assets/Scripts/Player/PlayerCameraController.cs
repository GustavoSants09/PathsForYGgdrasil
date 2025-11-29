using UnityEngine;
using Photon.Pun;

/// <summary>
/// Controla a rotação da câmera em primeira pessoa com o mouse
/// Rotaciona o corpo do jogador horizontalmente e a câmera verticalmente
/// </summary>
public class PlayerCameraController : MonoBehaviourPun
{
    [Header("Referências")]
    [Tooltip("Transform da câmera que será rotacionada verticalmente")]
    [SerializeField] private Transform cameraTransform;

    [Header("Configurações de Sensibilidade")]
    [Tooltip("Sensibilidade horizontal do mouse")]
    [SerializeField] private float mouseSensitivityX = 2f;

    [Tooltip("Sensibilidade vertical do mouse")]
    [SerializeField] private float mouseSensitivityY = 2f;

    [Header("Limites de Rotação")]
    [Tooltip("Ângulo máximo para olhar para cima")]
    [SerializeField] private float maxLookUpAngle = 80f;

    [Tooltip("Ângulo máximo para olhar para baixo")]
    [SerializeField] private float maxLookDownAngle = -80f;

    // Variáveis de controle
    private float rotationX = 0f; // Rotação vertical acumulada
    private Transform playerTransform;
    private bool isCameraControlEnabled = true;

    /// <summary>
    /// Inicialização
    /// </summary>
    void Awake()
    {
        playerTransform = transform;

        // Trava e esconde o cursor
        if (photonView.IsMine)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    /// <summary>
    /// Atualização a cada frame
    /// </summary>
    void Update()
    {
        // Só processa se for o jogador local
        if (photonView.IsMine == false || PhotonNetwork.IsConnected == true && photonView.IsMine == false)
        {
            return;
        }

        // Processa rotação da câmera
        if (isCameraControlEnabled)
        {
            ProcessCameraRotation();
        }

        // Toggle do cursor com ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleCursorLock();
        }
    }

    /// <summary>
    /// Processa a rotação da câmera baseado no movimento do mouse
    /// </summary>
    private void ProcessCameraRotation()
    {
        // Captura input do mouse
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivityX;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivityY;

        // Rotação horizontal - rotaciona o corpo do jogador
        playerTransform.Rotate(Vector3.up * mouseX);

        // Rotação vertical - rotaciona apenas a câmera
        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, maxLookDownAngle, maxLookUpAngle);

        cameraTransform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
    }

    /// <summary>
    /// Alterna entre cursor travado e livre
    /// </summary>
    private void ToggleCursorLock()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            isCameraControlEnabled = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            isCameraControlEnabled = true;
        }
    }

    /// <summary>
    /// Habilita ou desabilita o controle da câmera (útil para pausar ou abrir menus)
    /// </summary>
    public void SetCameraControlEnabled(bool enabled)
    {
        isCameraControlEnabled = enabled;
    }
}