using UnityEngine;
using Photon.Pun;
using TMPro;

namespace QuantumHeist.Game
{
    /// <summary>
    /// Controla movimentação básica do jogador com sincronização via Photon
    /// Versão simplificada focada apenas em movimento WASD e mouse look
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PhotonView))]
    public class PlayerController : MonoBehaviourPunCallbacks, IPunObservable
    {
        [Header("Movement Settings")]
        [SerializeField] private float movementSpeed = 5f;
        [SerializeField] private float sprintMultiplier = 1.5f;
        [SerializeField] private float gravity = -9.81f;

        [Header("Mouse Look Settings")]
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float maxLookAngle = 80f;

        [Header("Components")]
        private CharacterController characterController;
        private Camera playerCamera;
        private PhotonView photonView;

        // State
        private float verticalVelocity = 0f;
        private float cameraPitch = 0f;

        // Sincronização de rede
        private Vector3 networkPosition;
        private Quaternion networkRotation;

        #region Unity Callbacks

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            photonView = GetComponent<PhotonView>();
        }

        private void Start()
        {
            // Configura apenas para o jogador local
            if (photonView.IsMine)
            {
                // Cria e configura câmera
                SetupCamera();

                // Trava e esconde cursor
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                // Desabilita câmera para jogadores remotos
                if (playerCamera != null)
                    playerCamera.enabled = false;
            }

            // Define nome do jogador acima da cabeça
            SetPlayerName();
        }

        private void Update()
        {
            if (!photonView.IsMine)
            {
                // Interpola posição de jogadores remotos
                transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * 10f);
                transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation, Time.deltaTime * 10f);
                return;
            }

            // Controles apenas para jogador local
            HandleMouseLook();
            HandleMovement();

            // Libera cursor ao pressionar ESC
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        #endregion

        #region Setup

        /// <summary>
        /// Configura a câmera para o jogador local
        /// </summary>
        private void SetupCamera()
        {
            // Procura câmera existente no player ou cria uma nova
            playerCamera = GetComponentInChildren<Camera>();

            if (playerCamera == null)
            {
                GameObject cameraObj = new GameObject("PlayerCamera");
                cameraObj.transform.SetParent(transform);
                cameraObj.transform.localPosition = new Vector3(0, 1.6f, 0); // Altura dos olhos
                cameraObj.transform.localRotation = Quaternion.identity;

                playerCamera = cameraObj.AddComponent<Camera>();
                playerCamera.fieldOfView = 75f;

                // Adiciona AudioListener se não existir
                if (FindObjectOfType<AudioListener>() == null)
                {
                    cameraObj.AddComponent<AudioListener>();
                }
            }

            playerCamera.enabled = true;
        }

        /// <summary>
        /// Cria texto com nome do jogador acima da cabeça
        /// </summary>
        private void SetPlayerName()
        {
            GameObject nameTextObj = new GameObject("PlayerNameTag");
            nameTextObj.transform.SetParent(transform);
            nameTextObj.transform.localPosition = new Vector3(0, 2.5f, 0);

            // Cria TextMeshPro 3D
            TextMeshPro nameText = nameTextObj.AddComponent<TextMeshPro>();
            nameText.text = photonView.Owner.NickName;
            nameText.fontSize = 4;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.color = Color.white;

            // Sempre olha para câmera principal
            Billboard billboard = nameTextObj.AddComponent<Billboard>();
        }

        #endregion

        #region Movement

        /// <summary>
        /// Processa input de movimentação WASD
        /// </summary>
        private void HandleMovement()
        {
            // Input de movimento
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            // Calcula direção de movimento baseado na rotação do jogador
            Vector3 moveDirection = transform.right * horizontal + transform.forward * vertical;
            moveDirection.Normalize();

            // Aplica velocidade
            float currentSpeed = movementSpeed;
            if (Input.GetKey(KeyCode.LeftShift))
            {
                currentSpeed *= sprintMultiplier;
            }

            // Aplica gravidade
            if (characterController.isGrounded)
            {
                verticalVelocity = -2f; // Pequena força para manter no chão
            }
            else
            {
                verticalVelocity += gravity * Time.deltaTime;
            }

            // Aplica movimento vertical (gravidade)
            moveDirection.y = verticalVelocity;

            // Move o personagem
            characterController.Move(moveDirection * currentSpeed * Time.deltaTime);
        }

        /// <summary>
        /// Processa input de mouse para rotação da câmera
        /// </summary>
        private void HandleMouseLook()
        {
            // Input do mouse
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            // Rotação horizontal (Y-axis) - rotaciona o corpo do jogador
            transform.Rotate(Vector3.up * mouseX);

            // Rotação vertical (X-axis) - rotaciona apenas a câmera
            cameraPitch -= mouseY;
            cameraPitch = Mathf.Clamp(cameraPitch, -maxLookAngle, maxLookAngle);

            if (playerCamera != null)
            {
                playerCamera.transform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
            }
        }

        #endregion

        #region Photon Synchronization

        /// <summary>
        /// Sincroniza posição e rotação via Photon
        /// </summary>
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                // Envia dados para rede
                stream.SendNext(transform.position);
                stream.SendNext(transform.rotation);
            }
            else
            {
                // Recebe dados da rede
                networkPosition = (Vector3)stream.ReceiveNext();
                networkRotation = (Quaternion)stream.ReceiveNext();
            }
        }

        #endregion
    }

    /// <summary>
    /// Componente que faz o texto sempre olhar para a câmera principal
    /// </summary>
    public class Billboard : MonoBehaviour
    {
        private Camera mainCamera;

        private void Start()
        {
            mainCamera = Camera.main;
        }

        private void LateUpdate()
        {
            if (mainCamera != null)
            {
                transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                                 mainCamera.transform.rotation * Vector3.up);
            }
        }
    }
}