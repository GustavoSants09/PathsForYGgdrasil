using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Collections;

namespace QuantumHeist.Game
{
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
            originalSpeed = movementSpeed;

            if (photonView.IsMine)
            {
                InitializeScore();
                SetupCamera();
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                if (playerCamera != null)
                    playerCamera.enabled = false;

                // NOVO: Carrega score inicial de outros jogadores
                LoadScoreFromCustomProperties();
            }

            SetPlayerName();
        }

        private void Update()
        {
            if (!photonView.IsMine)
            {
                transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * 10f);
                transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation, Time.deltaTime * 10f);
                return;
            }

            HandleMouseLook();
            HandleMovement();

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        #endregion

        #region Setup

        /// <summary>
        /// Inicializa score do jogador nas CustomProperties do Photon
        /// </summary>
        private void InitializeScore()
        {
            ExitGames.Client.Photon.Hashtable initialProps = new ExitGames.Client.Photon.Hashtable
            {
                { "Score", 0 }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(initialProps);

            Debug.Log($"[PlayerController] Score inicializado para {PhotonNetwork.LocalPlayer.NickName}");
        }

        /// <summary>
        /// NOVO: Carrega score de outros jogadores das CustomProperties
        /// </summary>
        private void LoadScoreFromCustomProperties()
        {
            if (photonView.Owner.CustomProperties.TryGetValue("Score", out object scoreValue))
            {
                playerScore = (int)scoreValue;
                Debug.Log($"[PlayerController] Score carregado para {photonView.Owner.NickName}: {playerScore}");
            }
        }

        private void SetupCamera()
        {
            playerCamera = GetComponentInChildren<Camera>();

            if (playerCamera == null)
            {
                GameObject cameraObj = new GameObject("PlayerCamera");
                cameraObj.transform.SetParent(transform);
                cameraObj.transform.localPosition = new Vector3(0, 0.6f, 0);
                cameraObj.transform.localRotation = Quaternion.identity;

                playerCamera = cameraObj.AddComponent<Camera>();
                playerCamera.fieldOfView = 75f;

                if (FindObjectOfType<AudioListener>() == null)
                {
                    cameraObj.AddComponent<AudioListener>();
                }
            }

            playerCamera.enabled = true;
            HideLocalPlayerModel();
        }

        private void HideLocalPlayerModel()
        {
            MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();

            foreach (MeshRenderer renderer in renderers)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }
        }

        private void SetPlayerName()
        {
            GameObject nameTextObj = new GameObject("PlayerNameTag");
            nameTextObj.transform.SetParent(transform);
            nameTextObj.transform.localPosition = new Vector3(0, 1.2f, 0);

            TextMeshPro nameText = nameTextObj.AddComponent<TextMeshPro>();
            nameText.text = photonView.Owner.NickName;
            nameText.fontSize = 3;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.color = Color.white;

            Billboard billboard = nameTextObj.AddComponent<Billboard>();
        }

        #endregion

        #region Movement

        private void HandleMovement()
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            Vector3 moveDirection = transform.right * horizontal + transform.forward * vertical;
            moveDirection.Normalize();

            float currentSpeed = movementSpeed;
            if (Input.GetKey(KeyCode.LeftShift))
            {
                currentSpeed *= sprintMultiplier;
            }

            if (characterController.isGrounded)
            {
                verticalVelocity = -2f;
            }
            else
            {
                verticalVelocity += gravity * Time.deltaTime;
            }

            moveDirection.y = verticalVelocity;
            characterController.Move(moveDirection * currentSpeed * Time.deltaTime);
        }

        private void HandleMouseLook()
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            transform.Rotate(Vector3.up * mouseX);

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
        /// SINCRONIZA COM PHOTON CUSTOM PROPERTIES
        /// </summary>
        public void CollectCrystal(int points, float speedMultiplier, float duration)
        {
            if (!photonView.IsMine) return;

            // Atualiza score local
            playerScore += points;

            // CRÍTICO: Sincroniza com Photon CustomProperties
            ExitGames.Client.Photon.Hashtable scoreProps = new ExitGames.Client.Photon.Hashtable
            {
                { "Score", playerScore }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(scoreProps);

            Debug.Log($"[PlayerController] {photonView.Owner.NickName} coletou cristal! Score: {playerScore}");

            // Aplica boost de velocidade
            ApplySpeedBoost(speedMultiplier, duration);

            // Feedback visual
            ShowCollectFeedback(points);
        }

        private void ApplySpeedBoost(float multiplier, float duration)
        {
            if (speedBoostCoroutine != null)
            {
                StopCoroutine(speedBoostCoroutine);
            }

            speedBoostCoroutine = StartCoroutine(SpeedBoostRoutine(multiplier, duration));
        }

        private IEnumerator SpeedBoostRoutine(float multiplier, float duration)
        {
            movementSpeed = originalSpeed * multiplier;
            Debug.Log($"[PlayerController] Speed boost ativado! Velocidade: {movementSpeed}");

            yield return new WaitForSeconds(duration);

            movementSpeed = originalSpeed;
            Debug.Log($"[PlayerController] Speed boost terminou. Velocidade: {movementSpeed}");

            speedBoostCoroutine = null;
        }

        private void ShowCollectFeedback(int points)
        {
            // TODO: Implementar feedback visual
        }

        public int GetScore()
        {
            return playerScore;
        }

        #endregion

        #region Photon Callbacks

        /// <summary>
        /// NOVO: Callback chamado quando as CustomProperties de um jogador são alteradas
        /// Sincroniza o score em todos os clientes
        /// </summary>
        public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
        {
            // Verifica se é o dono deste PlayerController
            if (targetPlayer != photonView.Owner)
                return;

            // Verifica se o Score foi alterado
            if (changedProps.ContainsKey("Score"))
            {
                playerScore = (int)changedProps["Score"];
                Debug.Log($"[PlayerController] Score atualizado para {photonView.Owner.NickName}: {playerScore}");
            }
        }

        #endregion

        #region Photon Synchronization

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(transform.position);
                stream.SendNext(transform.rotation);
            }
            else
            {
                networkPosition = (Vector3)stream.ReceiveNext();
                networkRotation = (Quaternion)stream.ReceiveNext();
            }
        }

        #endregion
    }

    public class Billboard : MonoBehaviour
    {
        private Transform cameraTransform;

        private void Start()
        {
            StartCoroutine(FindMainCamera());
        }

        private IEnumerator FindMainCamera()
        {
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
                Camera mainCam = Camera.main;
                if (mainCam != null)
                    cameraTransform = mainCam.transform;
                return;
            }

            Vector3 directionToCamera = cameraTransform.position - transform.position;
            directionToCamera.y = 0;

            if (directionToCamera.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToCamera);
                transform.rotation = targetRotation;
            }
        }
    }
}