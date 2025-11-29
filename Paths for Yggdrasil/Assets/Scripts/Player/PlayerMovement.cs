using UnityEngine;
using Photon.Pun;

/// <summary>
/// Controla a movimentação do jogador incluindo andar, correr e pulo
/// Funciona apenas para o jogador local (photonView.IsMine)
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviourPun
{
    [Header("Configurações de Movimentação")]
    [Tooltip("Velocidade normal de caminhada")]
    [SerializeField] private float walkSpeed = 3f;

    [Tooltip("Velocidade ao correr")]
    [SerializeField] private float runSpeed = 6f;

    [Tooltip("Força do pulo")]
    [SerializeField] private float jumpForce = 8f;

    [Tooltip("Gravidade aplicada ao jogador")]
    [SerializeField] private float gravity = -20f;

    // Componentes
    private CharacterController controller;
    private Transform playerTransform;

    // Variáveis de controle de movimento
    private Vector3 velocity;
    private bool isGrounded;

    // Propriedades públicas para sincronização
    public float CurrentSpeed { get; private set; }
    public bool IsRunning { get; private set; }
    public bool IsMoving { get; private set; }

    /// <summary>
    /// Inicialização dos componentes
    /// </summary>
    void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerTransform = transform;
    }

    /// <summary>
    /// Atualização a cada frame
    /// </summary>
    void Update()
    {
        // Só processa input se este é o jogador local E estamos conectados
        // OU se não estamos conectados (para testes offline)
        if (photonView.IsMine == false && PhotonNetwork.IsConnected == true)
        {
            return;
        }

        // Verifica se está no chão
        CheckGroundStatus();

        // Processa movimentação
        ProcessMovement();

        // Processa pulo
        ProcessJump();

        // Aplica gravidade
        ApplyGravity();

        // Move o personagem
        controller.Move(velocity * Time.deltaTime);
    }

    /// <summary>
    /// Verifica se o jogador está tocando o chão
    /// </summary>
    private void CheckGroundStatus()
    {
        // Verifica se está no chão usando o CharacterController
        isGrounded = controller.isGrounded;

        // Reseta velocidade vertical quando está no chão
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Pequeno valor negativo para manter no chão
        }
    }

    /// <summary>
    /// Processa o input de movimentação horizontal
    /// </summary>
    private void ProcessMovement()
    {
        // Captura input do jogador
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // Verifica se está correndo (Shift pressionado)
        IsRunning = Input.GetKey(KeyCode.LeftShift);

        // Define velocidade atual baseado em correr ou andar
        CurrentSpeed = IsRunning ? runSpeed : walkSpeed;

        // Calcula direção do movimento relativa à câmera
        Vector3 moveDirection = CalculateMoveDirection(horizontal, vertical);

        // Verifica se está se movendo
        IsMoving = moveDirection.magnitude > 0.1f;

        // Aplica movimento horizontal
        velocity.x = moveDirection.x * CurrentSpeed;
        velocity.z = moveDirection.z * CurrentSpeed;
    }

    /// <summary>
    /// Calcula direção do movimento baseado no input e orientação da câmera
    /// </summary>
    private Vector3 CalculateMoveDirection(float horizontal, float vertical)
    {
        // Cria vetor de movimento local
        Vector3 direction = new Vector3(horizontal, 0f, vertical);

        // Se não há movimento, retorna zero
        if (direction.magnitude < 0.1f)
        {
            return Vector3.zero;
        }

        // Normaliza para evitar movimento mais rápido na diagonal
        direction.Normalize();

        // Converte para direção mundial baseado na rotação do jogador
        direction = playerTransform.TransformDirection(direction);

        return direction;
    }

    /// <summary>
    /// Processa o input de pulo
    /// </summary>
    private void ProcessJump()
    {
        // Verifica se pressionou o botão de pulo E está no chão
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            // Aplica força de pulo
            velocity.y = jumpForce;
        }
    }

    /// <summary>
    /// Aplica gravidade ao jogador
    /// </summary>
    private void ApplyGravity()
    {
        // Aplica gravidade continuamente quando não está no chão
        if (!isGrounded)
        {
            velocity.y += gravity * Time.deltaTime;
        }
    }

    /// <summary>
    /// Retorna se o jogador está no chão (útil para outros scripts)
    /// </summary>
    public bool IsGrounded()
    {
        return isGrounded;
    }
}