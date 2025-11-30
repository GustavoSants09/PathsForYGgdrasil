using UnityEngine;
using Photon.Pun;
using TMPro;
using System.Collections;

namespace QuantumHeist.Game
{
    /// <summary>
    /// Controla o jogador: movimentação, dash, zonas de slow e colisões
    /// Sincroniza ações via Photon RPCs e PhotonView
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PhotonView))]
    public class PlayerController : MonoBehaviourPunCallbacks, IPunObservable
    {
        [Header("Movement Settings")]
        [SerializeField] private float baseMovementSpeed = 5f;
        [SerializeField] private float sprintMultiplier = 1.5f;

        [Header("Dash Settings")]
        [SerializeField] private float dashDistance = 10f;
        [SerializeField] private float dashDuration = 0.2f;
        [SerializeField] private float dashCooldown = 4f;

        [Header("Slow Zone Settings")]
        [SerializeField] private GameObject slowZonePrefab;
        [SerializeField] private float slowZoneCooldown = 8f;
        [SerializeField] private float slowZoneDuration = 6f;
        [SerializeField] private float slowZoneSlowPercent = 0.5f; // 50% de velocidade

        [Header("Speed Boost Settings")]
        [SerializeField] private float speedBoostMultiplier = 1.5f;
        [SerializeField] private float speedBoostDuration = 3f;

        [Header("UI References")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text dashCooldownText;
        [SerializeField] private TMP_Text slowZoneCooldownText;
        [SerializeField] private GameObject speedBoostIndicator;

        [Header("Visual Feedback")]
        [SerializeField] private ParticleSystem dashParticles;
        [SerializeField] private ParticleSystem stealParticles;

        // Componentes
        private CharacterController characterController;
        private Camera playerCamera;

        // Estado de movimento
        private Vector3 moveDirection;
        private float currentSpeed;
        private bool isInSlowZone = false;
        private float slowZoneSpeedMultiplier = 1f;

        // Estado de habilidades
        private bool canDash = true;
        private bool isDashing = false;
        private float dashCooldownTimer = 0f;

        private bool canPlaceSlowZone = true;
        private float slowZoneCooldownTimer = 0f;
        private GameObject activeSlowZone;

        private bool hasSpeedBoost = false;
        private float speedBoostTimer = 0f;

        // Pontuação local
        private int currentScore = 0;

        // Sincronização de rede
        private Vector3 networkPosition;
        private Quaternion networkRotation;

        #region Unity Callbacks

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
        }

        private void Start()
        {
            // Configura apenas para o jogador local
            if (photonView.IsMine)
            {
                // Cria e configura câmera
                SetupCamera();

                // Inicializa UI
                UpdateScoreUI();
                UpdateCooldownUI();
            }
            else
            {
                // Desabilita componentes desnecessários para jogadores remotos
                if (playerCamera != null)
                    playerCamera.enabled = false;
            }

            // Define nome do jogador
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
            HandleMovement();
            HandleDash();
            HandleSlowZone();
            UpdateTimers();
            UpdateCooldownUI();
        }

        #endregion

        #region Setup

        /// <summary>
        /// Configura a câmera para o jogador local
        /// </summary>
        private void SetupCamera()
        {
            GameObject cameraObj = new GameObject("PlayerCamera");
            playerCamera = cameraObj.AddComponent<Camera>();
            cameraObj.transform.SetParent(transform);
            cameraObj.transform.localPosition = new Vector3(0, 15, -10);
            cameraObj.transform.localRotation = Quaternion.Euler(60, 0, 0);
        }

        /// <summary>
        /// Define o nome visível do jogador
        /// </summary>
        private void SetPlayerName()
        {
            // Cria texto 3D com o nome do jogador
            GameObject nameTextObj = new GameObject("NameText");
            nameTextObj.transform.SetParent(transform);
            nameTextObj.transform.localPosition = new Vector3(0, 2, 0);

            TextMeshPro nameText = nameTextObj.AddComponent<TextMeshPro>();
            nameText.text = photonView.Owner.NickName;
            nameText.fontSize = 4;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.color = Color.white;
        }

        #endregion

        #region Movement

        /// <summary>
        /// Processa input de movimentação
        /// </summary>
        private void HandleMovement()
        {
            if (isDashing)
                return;

            // Input de movimento
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            moveDirection = new Vector3(horizontal, 0, vertical).normalized;

            // Calcula velocidade considerando todos os modificadores
            currentSpeed = baseMovementSpeed;

            // Sprint (Shift)
            if (Input.GetKey(KeyCode.LeftShift))
            {
                currentSpeed *= sprintMultiplier;
            }

            // Speed boost de cristal
            if (hasSpeedBoost)
            {
                currentSpeed *= speedBoostMultiplier;
            }

            // Slow zone
            if (isInSlowZone)
            {
                currentSpeed *= slowZoneSpeedMultiplier;
            }

            // Move o personagem
            characterController.Move(moveDirection * currentSpeed * Time.deltaTime);

            // Rotaciona para direção do movimento
            if (moveDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(moveDirection),
                    Time.deltaTime * 10f
                );
            }
        }

        /// <summary>
        /// Aplica efeito de slow zone no jogador
        /// </summary>
        public void ApplySlowZone(float slowMultiplier)
        {
            isInSlowZone = true;
            slowZoneSpeedMultiplier = slowMultiplier;
        }

        /// <summary>
        /// Remove efeito de slow zone do jogador
        /// </summary>
        public void RemoveSlowZone()
        {
            isInSlowZone = false;
            slowZoneSpeedMultiplier = 1f;
        }

        #endregion

        #region Dash Ability

        /// <summary>
        /// Processa input de dash
        /// </summary>
        private void HandleDash()
        {
            if (Input.GetKeyDown(KeyCode.Space) && canDash && !isDashing)
            {
                StartCoroutine(PerformDash());
            }
        }

        /// <summary>
        /// Executa o dash
        /// </summary>
        private IEnumerator PerformDash()
        {
            isDashing = true;
            canDash = false;
            dashCooldownTimer = dashCooldown;

            // Ativa partículas
            if (dashParticles != null)
                dashParticles.Play();

            // Calcula direção do dash
            Vector3 dashDirection = moveDirection != Vector3.zero ? moveDirection : transform.forward;
            Vector3 startPosition = transform.position;
            Vector3 targetPosition = startPosition + dashDirection * dashDistance;

            float elapsedTime = 0f;

            // Movimento do dash
            while (elapsedTime < dashDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / dashDuration;

                Vector3 newPosition = Vector3.Lerp(startPosition, targetPosition, t);
                characterController.Move(newPosition - transform.position);

                yield return null;
            }

            isDashing = false;
        }

        #endregion

        #region Slow Zone Ability

        /// <summary>
        /// Processa input de zona de slow
        /// </summary>
        private void HandleSlowZone()
        {
            if (Input.GetKeyDown(KeyCode.E) && canPlaceSlowZone)
            {
                PlaceSlowZone();
            }
        }

        /// <summary>
        /// Coloca uma zona de slow no chão
        /// </summary>
        private void PlaceSlowZone()
        {
            if (slowZonePrefab == null)
                return;

            canPlaceSlowZone = false;
            slowZoneCooldownTimer = slowZoneCooldown;

            // Instancia zona via Photon
            Vector3 spawnPosition = transform.position;
            activeSlowZone = PhotonNetwork.Instantiate(
                slowZonePrefab.name,
                spawnPosition,
                Quaternion.identity
            );

            // Configura a zona
            SlowZone slowZoneScript = activeSlowZone.GetComponent<SlowZone>();
            if (slowZoneScript != null)
            {
                slowZoneScript.Initialize(photonView.Owner.ActorNumber, slowZoneDuration, slowZoneSlowPercent);
            }

            Debug.Log($"Zona de slow colocada em {spawnPosition}");
        }

        #endregion

        #region Timers

        /// <summary>
        /// Atualiza timers de cooldowns e buffs
        /// </summary>
        private void UpdateTimers()
        {
            // Timer de dash
            if (!canDash)
            {
                dashCooldownTimer -= Time.deltaTime;
                if (dashCooldownTimer <= 0)
                {
                    canDash = true;
                    dashCooldownTimer = 0f;
                }
            }

            // Timer de slow zone
            if (!canPlaceSlowZone)
            {
                slowZoneCooldownTimer -= Time.deltaTime;
                if (slowZoneCooldownTimer <= 0)
                {
                    canPlaceSlowZone = true;
                    slowZoneCooldownTimer = 0f;
                }
            }

            // Timer de speed boost
            if (hasSpeedBoost)
            {
                speedBoostTimer -= Time.deltaTime;
                if (speedBoostTimer <= 0)
                {
                    hasSpeedBoost = false;
                    if (speedBoostIndicator != null)
                        speedBoostIndicator.SetActive(false);
                }
            }
        }

        #endregion

        #region Score & Crystals

        /// <summary>
        /// Coleta um cristal
        /// </summary>
        public void CollectCrystal(int points)
        {
            // Adiciona pontos
            GameManager.Instance.AddScore(photonView.Owner.ActorNumber, points);
            currentScore += points;

            // Ativa speed boost
            ActivateSpeedBoost();

            UpdateScoreUI();

            Debug.Log($"Cristal coletado! +{points} pontos. Total: {currentScore}");
        }

        /// <summary>
        /// Ativa o boost de velocidade temporário
        /// </summary>
        private void ActivateSpeedBoost()
        {
            hasSpeedBoost = true;
            speedBoostTimer = speedBoostDuration;

            if (speedBoostIndicator != null)
                speedBoostIndicator.SetActive(true);
        }

        /// <summary>
        /// Rouba pontos de outro jogador ao colidir durante dash
        /// </summary>
        public void StealPointsFrom(int targetActorNumber)
        {
            int targetScore = GameManager.Instance.GetScore(targetActorNumber);
            int stolenPoints = Mathf.RoundToInt(targetScore * 0.2f); // 20% dos pontos

            if (stolenPoints <= 0)
                return;

            // Remove pontos do alvo
            GameManager.Instance.AddScore(targetActorNumber, -stolenPoints);

            // Adiciona pontos ao atacante
            GameManager.Instance.AddScore(photonView.Owner.ActorNumber, stolenPoints);
            currentScore += stolenPoints;

            // Efeito visual
            if (stealParticles != null)
                stealParticles.Play();

            // Notifica o alvo via RPC
            photonView.RPC("RPC_GetStunned", RpcTarget.All, targetActorNumber);

            UpdateScoreUI();

            Debug.Log($"Roubou {stolenPoints} pontos!");
        }

        /// <summary>
        /// RPC que aplica stun no jogador atingido
        /// </summary>
        [PunRPC]
        private void RPC_GetStunned(int targetActorNumber)
        {
            if (photonView.Owner.ActorNumber == targetActorNumber && photonView.IsMine)
            {
                StartCoroutine(StunCoroutine());
            }
        }

        /// <summary>
        /// Aplica efeito de stun
        /// </summary>
        private IEnumerator StunCoroutine()
        {
            float originalSpeed = baseMovementSpeed;
            baseMovementSpeed = 0f;

            yield return new WaitForSeconds(1.5f);

            baseMovementSpeed = originalSpeed;
        }

        #endregion

        #region Collision Detection

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (!photonView.IsMine || !isDashing)
                return;

            // Detecta colisão com outro jogador durante dash
            PlayerController otherPlayer = hit.gameObject.GetComponent<PlayerController>();
            if (otherPlayer != null && otherPlayer.photonView.Owner.ActorNumber != photonView.Owner.ActorNumber)
            {
                StealPointsFrom(otherPlayer.photonView.Owner.ActorNumber);
            }
        }

        #endregion

        #region UI Updates

        /// <summary>
        /// Atualiza texto de pontuação
        /// </summary>
        private void UpdateScoreUI()
        {
            if (scoreText != null)
            {
                scoreText.text = $"Pontos: {currentScore}";
            }
        }

        /// <summary>
        /// Atualiza textos de cooldown
        /// </summary>
        private void UpdateCooldownUI()
        {
            if (dashCooldownText != null)
            {
                if (canDash)
                {
                    dashCooldownText.text = "Dash: PRONTO";
                    dashCooldownText.color = Color.green;
                }
                else
                {
                    dashCooldownText.text = $"Dash: {dashCooldownTimer:F1}s";
                    dashCooldownText.color = Color.red;
                }
            }

            if (slowZoneCooldownText != null)
            {
                if (canPlaceSlowZone)
                {
                    slowZoneCooldownText.text = "Slow Zone: PRONTO";
                    slowZoneCooldownText.color = Color.green;
                }
                else
                {
                    slowZoneCooldownText.text = $"Slow Zone: {slowZoneCooldownTimer:F1}s";
                    slowZoneCooldownText.color = Color.red;
                }
            }
        }

        #endregion

        #region Photon Synchronization

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                // Envia dados para rede
                stream.SendNext(transform.position);
                stream.SendNext(transform.rotation);
                stream.SendNext(currentScore);
            }
            else
            {
                // Recebe dados da rede
                networkPosition = (Vector3)stream.ReceiveNext();
                networkRotation = (Quaternion)stream.ReceiveNext();
                currentScore = (int)stream.ReceiveNext();
            }
        }

        #endregion
    }
}