using UnityEngine;
using Photon.Pun;
using System.Collections;

namespace QuantumHeist.Game
{
    /// <summary>
    /// Componente do cristal colecionável
    /// Gerencia coleta, efeitos visuais e sincronização via Photon
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    public class Crystal : MonoBehaviourPunCallbacks
    {
        [Header("Visual Settings")]
        [SerializeField] private float rotationSpeed = 50f;
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float bobHeight = 0.5f;
        [SerializeField] private ParticleSystem collectParticles;
        [SerializeField] private AudioClip collectSound;

        [Header("Crystal Materials")]
        [SerializeField] private Material normalMaterial; // Azul
        [SerializeField] private Material rareMaterial;   // Roxo
        [SerializeField] private Material epicMaterial;   // Dourado

        [Header("Glow Settings")]
        [SerializeField] private Light glowLight;
        [SerializeField] private float glowIntensity = 2f;
        [SerializeField] private float glowRange = 5f;

        // Estado do cristal
        private CrystalType crystalType;
        private int pointValue;
        private int spawnIndex;
        private bool isCollected = false;

        // Animação
        private Vector3 startPosition;
        private MeshRenderer meshRenderer;

        #region Initialization

        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }

        private void Start()
        {
            startPosition = transform.position;
            SetupGlow();
        }

        /// <summary>
        /// Inicializa o cristal com tipo e valor
        /// </summary>
        public void Initialize(CrystalType type, int value, int spawnIdx)
        {
            crystalType = type;
            pointValue = value;
            spawnIndex = spawnIdx;

            // Aplica visual baseado no tipo
            ApplyVisual();
        }

        /// <summary>
        /// Configura iluminação do cristal
        /// </summary>
        private void SetupGlow()
        {
            if (glowLight == null)
            {
                GameObject lightObj = new GameObject("CrystalGlow");
                lightObj.transform.SetParent(transform);
                lightObj.transform.localPosition = Vector3.zero;
                glowLight = lightObj.AddComponent<Light>();
                glowLight.type = LightType.Point;
            }

            glowLight.intensity = glowIntensity;
            glowLight.range = glowRange;
            UpdateGlowColor();
        }

        #endregion

        #region Visual

        /// <summary>
        /// Aplica material e cor baseado no tipo do cristal
        /// </summary>
        private void ApplyVisual()
        {
            if (meshRenderer == null)
                return;

            switch (crystalType)
            {
                case CrystalType.Normal:
                    if (normalMaterial != null)
                        meshRenderer.material = normalMaterial;
                    break;

                case CrystalType.Rare:
                    if (rareMaterial != null)
                        meshRenderer.material = rareMaterial;
                    break;

                case CrystalType.Epic:
                    if (epicMaterial != null)
                        meshRenderer.material = epicMaterial;
                    break;
            }

            UpdateGlowColor();
        }

        /// <summary>
        /// Atualiza cor da luz baseado no tipo
        /// </summary>
        private void UpdateGlowColor()
        {
            if (glowLight == null)
                return;

            switch (crystalType)
            {
                case CrystalType.Normal:
                    glowLight.color = new Color(0.3f, 0.6f, 1f); // Azul
                    break;

                case CrystalType.Rare:
                    glowLight.color = new Color(0.8f, 0.3f, 1f); // Roxo
                    break;

                case CrystalType.Epic:
                    glowLight.color = new Color(1f, 0.84f, 0f); // Dourado
                    break;
            }
        }

        #endregion

        #region Animation

        private void Update()
        {
            if (isCollected)
                return;

            // Rotação contínua
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

            // Movimento de flutuação (bobbing)
            float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(startPosition.x, newY, startPosition.z);
        }

        #endregion

        #region Collection

        private void OnTriggerEnter(Collider other)
        {
            if (isCollected)
                return;

            // Verifica se é um jogador
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null && player.photonView.IsMine)
            {
                // Coleta o cristal
                CollectCrystal(player);
            }
        }

        /// <summary>
        /// Processa a coleta do cristal pelo jogador
        /// </summary>
        private void CollectCrystal(PlayerController player)
        {
            if (isCollected)
                return;

            isCollected = true;

            // Adiciona pontos ao jogador
            player.CollectCrystal(pointValue);

            // Notifica spawner
            if (CrystalSpawner.Instance != null)
            {
                CrystalSpawner.Instance.OnCrystalCollected(gameObject, spawnIndex);
            }

            // Efeitos visuais e sonoros
            PlayCollectEffects();

            // Destrói o cristal via RPC para sincronizar com todos
            photonView.RPC("RPC_DestroyCrystal", RpcTarget.AllBuffered);
        }

        /// <summary>
        /// Reproduz efeitos de coleta
        /// </summary>
        private void PlayCollectEffects()
        {
            // Partículas
            if (collectParticles != null)
            {
                ParticleSystem particles = Instantiate(collectParticles, transform.position, Quaternion.identity);
                Destroy(particles.gameObject, 2f);
            }

            // Som
            if (collectSound != null)
            {
                AudioSource.PlayClipAtPoint(collectSound, transform.position);
            }
        }

        /// <summary>
        /// RPC que destroi o cristal em todos os clientes
        /// </summary>
        [PunRPC]
        private void RPC_DestroyCrystal()
        {
            // Desabilita colisão imediatamente
            Collider col = GetComponent<Collider>();
            if (col != null)
                col.enabled = false;

            // Animação de desaparecimento
            StartCoroutine(FadeOutAndDestroy());
        }

        /// <summary>
        /// Animação de fade out antes de destruir
        /// </summary>
        private IEnumerator FadeOutAndDestroy()
        {
            float duration = 0.3f;
            float elapsed = 0f;
            Vector3 originalScale = transform.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Diminui escala
                transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);

                // Diminui luz
                if (glowLight != null)
                    glowLight.intensity = Mathf.Lerp(glowIntensity, 0f, t);

                yield return null;
            }

            // Destroi o objeto via Photon
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.Destroy(gameObject);
            }
        }

        #endregion

        #region Debug

        private void OnDrawGizmos()
        {
            // Visualiza área de coleta no editor
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 1f);
        }

        #endregion
    }
}