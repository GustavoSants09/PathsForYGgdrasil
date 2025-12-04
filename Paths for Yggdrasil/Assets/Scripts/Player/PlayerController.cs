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

        [Header("Dash Settings")]
        [SerializeField] private float dashDistance = 10f;
        [SerializeField] private float dashDuration = 0.2f;
        [SerializeField] private float dashCooldown = 4f;

        [Header("PvP Settings")]
        [SerializeField] private float stealPercentage = 0.2f; // 20% dos pontos
        [SerializeField] private float stunDuration = 1.5f;
        [SerializeField] private int minPointsToSteal = 10; // Mínimo para roubar

        [Header("Crystal Collection")]
        [SerializeField] private int playerScore = 0;
        private float originalSpeed;
        private Coroutine speedBoostCoroutine;

        [Header("Visual Feedback")]
        [SerializeField] private ParticleSystem dashParticles;
        [SerializeField] private AudioClip dashSound;
        [SerializeField] private ParticleSystem stealEffect; // Efeito de roubo
        [SerializeField] private AudioClip stealSound;
        [SerializeField] private ParticleSystem stunEffect; // Efeito de atordoamento
        [SerializeField] private AudioClip stunSound;

        [Header("Components")]
        private CharacterController characterController;
        private Camera playerCamera;
        private PhotonView photonView;
        private AudioSource audioSource;

        // State
        private float verticalVelocity = 0f;
        private float cameraPitch = 0f;

        // Dash State
        private bool canDash = true;
        private bool isDashing = false;
        private float dashCooldownTimer = 0f;
        private Vector3 lastMoveDirection = Vector3.forward;

        // Stun State
        private bool isStunned = false;
        private Coroutine stunCoroutine;

        // Sincronização de rede
        private Vector3 networkPosition;
        private Quaternion networkRotation;

        #region Unity Callbacks

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            photonView = GetComponent<PhotonView>();

            // Adiciona AudioSource se necessário
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f; // Som 3D
                audioSource.maxDistance = 20f;
            }
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

                // Carrega score inicial de outros jogadores
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
            HandleDash();
            UpdateDashTimer();

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        #endregion

        #region Setup

        private void InitializeScore()
        {
            ExitGames.Client.Photon.Hashtable initialProps = new ExitGames.Client.Photon.Hashtable
            {
                { "Score", 0 }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(initialProps);

            Debug.Log($"[PlayerController] Score inicializado para {PhotonNetwork.LocalPlayer.NickName}");
        }

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
            // ✅ Bloqueia movimento durante dash OU atordoamento
            if (isDashing || isStunned)
                return;

            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            Vector3 moveDirection = transform.right * horizontal + transform.forward * vertical;
            moveDirection.Normalize();

            // Armazena última direção para o dash
            if (moveDirection != Vector3.zero)
            {
                lastMoveDirection = moveDirection;
            }

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
            // ✅ Bloqueia rotação da câmera durante atordoamento
            if (isStunned)
                return;

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

        #region Dash System

        private void HandleDash()
        {
            // ✅ Bloqueia dash durante atordoamento
            if (Input.GetKeyDown(KeyCode.Space) && canDash && !isDashing && !isStunned && characterController.isGrounded)
            {
                StartCoroutine(PerformDash());
            }
        }

        private IEnumerator PerformDash()
        {
            isDashing = true;
            canDash = false;
            dashCooldownTimer = dashCooldown;

            // Sincroniza efeitos visuais com outros jogadores
            photonView.RPC("RPC_PlayDashEffect", RpcTarget.AllBuffered);

            // Calcula direção do dash
            Vector3 dashDirection = lastMoveDirection.normalized;
            float dashSpeed = dashDistance / dashDuration;
            float elapsedTime = 0f;

            Debug.Log($"[PlayerController] 💨 DASH iniciado! Direção: {dashDirection}");

            // Movimento do dash com detecção de colisão PvP
            while (elapsedTime < dashDuration)
            {
                if (!photonView.IsMine)
                    yield break;

                float step = dashSpeed * Time.deltaTime;
                characterController.Move(dashDirection * step);

                // ✅ DETECÇÃO DE COLISÃO COM OUTROS JOGADORES
                CheckDashCollision();

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            isDashing = false;
            Debug.Log($"[PlayerController] ✅ Dash finalizado! Cooldown: {dashCooldown}s");
        }

        /// <summary>
        /// ✅ NOVO: Detecta colisão do dash com outros jogadores
        /// </summary>
        private void CheckDashCollision()
        {
            // Raycast esférico para detectar jogadores próximos
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, 1.5f);

            foreach (Collider hit in hitColliders)
            {
                // Ignora a si mesmo
                if (hit.gameObject == gameObject)
                    continue;

                // Verifica se é outro jogador
                PlayerController otherPlayer = hit.GetComponent<PlayerController>();
                if (otherPlayer != null && otherPlayer.photonView != null)
                {
                    // ✅ Processa roubo de pontos
                    ProcessPointSteal(otherPlayer);
                    break; // Apenas um roubo por dash
                }
            }
        }

        /// <summary>
        /// ✅ NOVO: Processa o roubo de pontos
        /// </summary>
        private void ProcessPointSteal(PlayerController victim)
        {
            if (!photonView.IsMine) return;

            // Obtém score da vítima das CustomProperties
            int victimScore = 0;
            if (victim.photonView.Owner.CustomProperties.TryGetValue("Score", out object scoreValue))
            {
                victimScore = (int)scoreValue;
            }

            // Verifica se a vítima tem pontos suficientes
            if (victimScore < minPointsToSteal)
            {
                Debug.Log($"[PlayerController] 🚫 {victim.photonView.Owner.NickName} não tem pontos suficientes para roubar");
                return;
            }

            // Calcula pontos roubados (20%)
            int stolenPoints = Mathf.FloorToInt(victimScore * stealPercentage);
            stolenPoints = Mathf.Max(stolenPoints, minPointsToSteal); // Mínimo de 10 pontos

            Debug.Log($"[PlayerController] 💰 Roubando {stolenPoints} pontos de {victim.photonView.Owner.NickName}!");

            // ✅ Chama RPC para sincronizar roubo
            photonView.RPC("RPC_StealPoints", RpcTarget.AllBuffered,
                victim.photonView.ViewID,
                stolenPoints);
        }

        /// <summary>
        /// ✅ RPC que sincroniza o roubo de pontos
        /// </summary>
        [PunRPC]
        private void RPC_StealPoints(int victimViewID, int stolenPoints)
        {
            // Encontra a vítima
            PhotonView victimView = PhotonView.Find(victimViewID);
            if (victimView == null) return;

            PlayerController victim = victimView.GetComponent<PlayerController>();
            if (victim == null) return;

            Player attacker = photonView.Owner;
            Player victimPlayer = victimView.Owner;

            Debug.Log($"[PlayerController] 🎯 RPC_StealPoints: {attacker.NickName} roubou {stolenPoints} de {victimPlayer.NickName}");

            // ✅ Atualiza scores nas CustomProperties (apenas Master Client)
            if (PhotonNetwork.IsMasterClient)
            {
                // Remove pontos da vítima
                int victimCurrentScore = 0;
                if (victimPlayer.CustomProperties.TryGetValue("Score", out object victimScoreObj))
                {
                    victimCurrentScore = (int)victimScoreObj;
                }

                int newVictimScore = Mathf.Max(0, victimCurrentScore - stolenPoints);

                ExitGames.Client.Photon.Hashtable victimProps = new ExitGames.Client.Photon.Hashtable
                {
                    { "Score", newVictimScore }
                };
                victimPlayer.SetCustomProperties(victimProps);

                // Adiciona pontos ao atacante
                int attackerCurrentScore = 0;
                if (attacker.CustomProperties.TryGetValue("Score", out object attackerScoreObj))
                {
                    attackerCurrentScore = (int)attackerScoreObj;
                }

                int newAttackerScore = attackerCurrentScore + stolenPoints;

                ExitGames.Client.Photon.Hashtable attackerProps = new ExitGames.Client.Photon.Hashtable
                {
                    { "Score", newAttackerScore }
                };
                attacker.SetCustomProperties(attackerProps);

                Debug.Log($"[PlayerController] 📊 Scores atualizados: {victimPlayer.NickName}={newVictimScore}, {attacker.NickName}={newAttackerScore}");
            }

            // ✅ Aplica atordoamento na vítima
            if (victim.photonView.IsMine)
            {
                victim.ApplyStun();
            }

            // ✅ Efeitos visuais
            PlayStealEffects(victim.transform.position);

            // Atualiza UI
            UpdateAllScoreboards();
        }

        private void UpdateDashTimer()
        {
            if (!canDash)
            {
                dashCooldownTimer -= Time.deltaTime;
                if (dashCooldownTimer <= 0)
                {
                    canDash = true;
                    dashCooldownTimer = 0f;
                    Debug.Log("[PlayerController] ⚡ Dash disponível!");
                }
            }
        }

        [PunRPC]
        private void RPC_PlayDashEffect()
        {
            if (dashParticles != null)
            {
                dashParticles.Play();
            }

            if (audioSource != null && dashSound != null)
            {
                audioSource.PlayOneShot(dashSound);
            }

            Debug.Log($"[PlayerController] 🎨 Efeito de dash reproduzido para {photonView.Owner.NickName}");
        }

        public float GetDashCooldownPercent()
        {
            return Mathf.Clamp01(1f - (dashCooldownTimer / dashCooldown));
        }

        public bool IsDashAvailable()
        {
            return canDash && characterController.isGrounded && !isStunned;
        }

        public float GetDashCooldownRemaining()
        {
            return Mathf.Max(0f, dashCooldownTimer);
        }

        #endregion

        #region Stun System

        /// <summary>
        /// ✅ NOVO: Aplica atordoamento no jogador
        /// </summary>
        public void ApplyStun()
        {
            if (!photonView.IsMine) return;

            if (stunCoroutine != null)
            {
                StopCoroutine(stunCoroutine);
            }

            stunCoroutine = StartCoroutine(StunRoutine());
        }

        /// <summary>
        /// ✅ NOVO: Corrotina de atordoamento
        /// </summary>
        private IEnumerator StunRoutine()
        {
            isStunned = true;

            Debug.Log($"[PlayerController] 😵 {photonView.Owner.NickName} foi ATORDOADO por {stunDuration}s!");

            // Efeitos visuais
            if (stunEffect != null)
            {
                stunEffect.Play();
            }

            if (audioSource != null && stunSound != null)
            {
                audioSource.PlayOneShot(stunSound);
            }

            // Aguarda duração do atordoamento
            yield return new WaitForSeconds(stunDuration);

            isStunned = false;

            // Para efeitos
            if (stunEffect != null)
            {
                stunEffect.Stop();
            }

            Debug.Log($"[PlayerController] ✅ {photonView.Owner.NickName} recuperado do atordoamento!");

            stunCoroutine = null;
        }

        /// <summary>
        /// Retorna se o jogador está atordoado
        /// </summary>
        public bool IsStunned()
        {
            return isStunned;
        }

        #endregion

        #region Visual Effects

        /// <summary>
        /// ✅ NOVO: Efeitos visuais de roubo
        /// </summary>
        private void PlayStealEffects(Vector3 position)
        {
            // Partículas de roubo
            if (stealEffect != null)
            {
                ParticleSystem effect = Instantiate(stealEffect, position, Quaternion.identity);
                Destroy(effect.gameObject, 3f);
            }

            // Som de roubo
            if (stealSound != null)
            {
                AudioSource.PlayClipAtPoint(stealSound, position);
            }
        }

        #endregion

        #region Crystal Collection System

        public void CollectCrystal(int points, float speedMultiplier, float duration)
        {
            if (!photonView.IsMine) return;

            playerScore += points;

            ExitGames.Client.Photon.Hashtable scoreProps = new ExitGames.Client.Photon.Hashtable
            {
                { "Score", playerScore }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(scoreProps);

            Debug.Log($"[PlayerController] {photonView.Owner.NickName} coletou cristal! Novo Score: {playerScore}");

            ApplySpeedBoost(speedMultiplier, duration);
            ShowCollectFeedback(points);
            UpdateAllScoreboards();
        }

        private void UpdateAllScoreboards()
        {
            UIManager[] uiManagers = FindObjectsOfType<UIManager>();
            foreach (UIManager ui in uiManagers)
            {
                ui.UpdateAllPlayerScores();
            }
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

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
        {
            if (targetPlayer != photonView.Owner)
                return;

            if (changedProps.ContainsKey("Score"))
            {
                int oldScore = playerScore;
                playerScore = (int)changedProps["Score"];
                Debug.Log($"[PlayerController] ✅ Score atualizado para {photonView.Owner.NickName}: {oldScore} → {playerScore}");

                UpdateAllScoreboards();
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
                stream.SendNext(playerScore);
                stream.SendNext(isDashing);
                stream.SendNext(dashCooldownTimer);
                stream.SendNext(isStunned); // ✅ Sincroniza estado de stun
            }
            else
            {
                networkPosition = (Vector3)stream.ReceiveNext();
                networkRotation = (Quaternion)stream.ReceiveNext();
                playerScore = (int)stream.ReceiveNext();
                isDashing = (bool)stream.ReceiveNext();
                dashCooldownTimer = (float)stream.ReceiveNext();
                isStunned = (bool)stream.ReceiveNext(); // ✅ Recebe estado de stun
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