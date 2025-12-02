using UnityEngine;
using Photon.Pun;
using TMPro;
using System.Collections;

namespace QuantumHeist.Game
{
    /// <summary>
    /// Controla movimentação básica do jogador com sincronização via Photon
    /// Inclui sistema de coleta de cristais com pontuação individual e boost de velocidade
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

        [Header("Crystal Collection")]
        [SerializeField] private int playerScore = 0;
        private float originalSpeed;
        private Coroutine speedBoostCoroutine;

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
            // Salva velocidade original para boost temporário
            originalSpeed = movementSpeed;

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
        /// Configura a câmera em primeira pessoa para o jogador local
        /// </summary>
        private void SetupCamera()
        {
            // Procura câmera existente no player ou cria uma nova
            playerCamera = GetComponentInChildren<Camera>();

            if (playerCamera == null)
            {
                GameObject cameraObj = new GameObject("PlayerCamera");
                cameraObj.transform.SetParent(transform);

                // Posiciona câmera DENTRO do modelo (primeira pessoa)
                cameraObj.transform.localPosition = new Vector3(0, 0.6f, 0); // Altura da "cabeça"
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

            // Esconde o modelo visual do próprio jogador (opcional para primeira pessoa)
            HideLocalPlayerModel();
        }

        /// <summary>
        /// Esconde o modelo do jogador local para não aparecer na câmera de primeira pessoa
        /// </summary>
        private void HideLocalPlayerModel()
        {
            // Procura todos os MeshRenderers no jogador
            MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();

            foreach (MeshRenderer renderer in renderers)
            {
                // Desabilita apenas para a câmera local (Layer)
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }
        }

        /// <summary>
        /// Cria texto com nome do jogador acima da cabeça
        /// CORRIGIDO: Billboard agora rotaciona corretamente
        /// </summary>
        private void SetPlayerName()
        {
            GameObject nameTextObj = new GameObject("PlayerNameTag");
            nameTextObj.transform.SetParent(transform);
            nameTextObj.transform.localPosition = new Vector3(0, 1.2f, 0); // Acima da "cabeça"

            // Cria TextMeshPro 3D
            TextMeshPro nameText = nameTextObj.AddComponent<TextMeshPro>();
            nameText.text = photonView.Owner.NickName;
            nameText.fontSize = 3;
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

            // Aplica velocidade (usa movementSpeed que pode estar com boost)
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

        #region Crystal Collection System

        /// <summary>
        /// Chamado quando jogador coleta um cristal
        /// </summary>
        public void CollectCrystal(int points, float speedMultiplier, float duration)
        {
            // Adiciona pontos
            playerScore += points;

            Debug.Log($"[PlayerController] {photonView.Owner.NickName} collected crystal! Score: {playerScore}");

            // Atualiza UI (se houver)
            UpdateScoreUI();

            // Aplica boost de velocidade
            ApplySpeedBoost(speedMultiplier, duration);

            // Feedback visual
            ShowCollectFeedback(points);
        }

        /// <summary>
        /// Aplica boost temporário de velocidade
        /// </summary>
        private void ApplySpeedBoost(float multiplier, float duration)
        {
            // Cancela boost anterior se existir
            if (speedBoostCoroutine != null)
            {
                StopCoroutine(speedBoostCoroutine);
            }

            speedBoostCoroutine = StartCoroutine(SpeedBoostRoutine(multiplier, duration));
        }

        /// <summary>
        /// Coroutine do boost de velocidade
        /// </summary>
        private IEnumerator SpeedBoostRoutine(float multiplier, float duration)
        {
            // Aplica boost
            movementSpeed = originalSpeed * multiplier;

            Debug.Log($"[PlayerController] Speed boost activated! Speed: {movementSpeed}");

            // Aguarda duração
            yield return new WaitForSeconds(duration);

            // Restaura velocidade
            movementSpeed = originalSpeed;

            Debug.Log($"[PlayerController] Speed boost ended. Speed: {movementSpeed}");

            speedBoostCoroutine = null;
        }

        /// <summary>
        /// Atualiza UI de pontuação
        /// </summary>
        private void UpdateScoreUI()
        {
            // TODO: Implementar quando houver UI
            // Exemplo: scoreText.text = $"Score: {playerScore}";
        }

        /// <summary>
        /// Mostra feedback visual de coleta
        /// </summary>
        private void ShowCollectFeedback(int points)
        {
            // TODO: Implementar feedback visual
            // Exemplo: Texto flutuante "+10", flash na tela, etc.
        }

        /// <summary>
        /// Getter para pontuação (útil para UI e placar)
        /// </summary>
        public int GetScore()
        {
            return playerScore;
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
    /// Componente que faz o texto sempre aparecer de frente para quem está olhando
    /// Corrige o problema de texto aparecer de lado ou invertido
    /// </summary>
    public class Billboard : MonoBehaviour
    {
        private Transform cameraTransform;

        private void Start()
        {
            // Aguarda a câmera principal ser criada
            StartCoroutine(FindMainCamera());
        }

        private IEnumerator FindMainCamera()
        {
            // Aguarda até que a câmera principal exista
            while (Camera.main == null)
            {
                yield return new WaitForSeconds(0.1f);
            }

            cameraTransform = Camera.main.transform;
        }

        private void LateUpdate()
        {
            if (cameraTransform == null)
            {
                // Tenta encontrar a câmera novamente se perdeu a referência
                Camera mainCam = Camera.main;
                if (mainCam != null)
                    cameraTransform = mainCam.transform;
                return;
            }

            // Faz o texto sempre olhar diretamente para a câmera
            // Calcula direção da câmera para o texto
            Vector3 directionToCamera = cameraTransform.position - transform.position;

            // Mantém apenas rotação horizontal (Y-axis), ignora vertical
            directionToCamera.y = 0;

            // Se houver direção válida, rotaciona para ela
            if (directionToCamera.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToCamera);
                transform.rotation = targetRotation;
            }
        }
    }
}