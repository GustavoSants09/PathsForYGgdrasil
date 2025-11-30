using UnityEngine;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;

namespace QuantumHeist.Game
{
    /// <summary>
    /// Zona que aplica slow em jogadores inimigos
    /// Criada pela habilidade do jogador
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    public class SlowZone : MonoBehaviourPunCallbacks
    {
        [Header("Zone Settings")]
        [SerializeField] private float zoneRadius = 5f;
        [SerializeField] private LayerMask playerLayer;

        [Header("Visual Settings")]
        [SerializeField] private GameObject visualEffect;
        [SerializeField] private ParticleSystem slowParticles;
        [SerializeField] private Color zoneColor = new Color(0.5f, 0f, 0.8f, 0.3f);

        [Header("Audio")]
        [SerializeField] private AudioClip activateSound;
        [SerializeField] private AudioClip ambientSound;

        // Estado da zona
        private int ownerActorNumber;
        private float duration;
        private float slowPercent;
        private float lifetime;
        private bool isActive = true;

        // Controle de jogadores afetados
        private List<PlayerController> affectedPlayers = new List<PlayerController>();
        private HashSet<int> affectedPlayerIDs = new HashSet<int>();

        // Componentes visuais
        private SphereCollider zoneCollider;
        private MeshRenderer zoneRenderer;
        private AudioSource audioSource;

        #region Initialization

        private void Awake()
        {
            SetupComponents();
        }

        /// <summary>
        /// Configura componentes da zona
        /// </summary>
        private void SetupComponents()
        {
            // Collider para detectar jogadores
            zoneCollider = gameObject.AddComponent<SphereCollider>();
            zoneCollider.isTrigger = true;
            zoneCollider.radius = zoneRadius;

            // Visual da zona (esfera semi-transparente)
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(sphere.GetComponent<Collider>()); // Remove collider do visual
            sphere.transform.SetParent(transform);
            sphere.transform.localPosition = Vector3.zero;
            sphere.transform.localScale = Vector3.one * (zoneRadius * 2);

            zoneRenderer = sphere.GetComponent<MeshRenderer>();
            Material zoneMaterial = new Material(Shader.Find("Standard"));
            zoneMaterial.color = zoneColor;
            zoneMaterial.SetFloat("_Mode", 3); // Transparent mode
            zoneMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            zoneMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            zoneMaterial.SetInt("_ZWrite", 0);
            zoneMaterial.DisableKeyword("_ALPHATEST_ON");
            zoneMaterial.EnableKeyword("_ALPHABLEND_ON");
            zoneMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            zoneMaterial.renderQueue = 3000;
            zoneRenderer.material = zoneMaterial;

            // Audio
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D sound
            audioSource.maxDistance = zoneRadius * 2;
            audioSource.loop = true;
        }

        /// <summary>
        /// Inicializa a zona com parâmetros
        /// </summary>
        public void Initialize(int ownerActor, float zoneDuration, float slowAmount)
        {
            ownerActorNumber = ownerActor;
            duration = zoneDuration;
            slowPercent = slowAmount;
            lifetime = 0f;

            // Ativa efeitos
            ActivateZone();
        }

        private void Start()
        {
            // Começa a contar tempo de vida
            StartCoroutine(LifetimeCoroutine());
        }

        #endregion

        #region Zone Activation

        /// <summary>
        /// Ativa efeitos visuais e sonoros da zona
        /// </summary>
        private void ActivateZone()
        {
            // Partículas
            if (slowParticles != null)
            {
                slowParticles.Play();
            }

            // Som de ativação
            if (activateSound != null)
            {
                AudioSource.PlayClipAtPoint(activateSound, transform.position);
            }

            // Som ambiente
            if (ambientSound != null && audioSource != null)
            {
                audioSource.clip = ambientSound;
                audioSource.Play();
            }

            // Animação de expansão
            StartCoroutine(ExpandAnimation());
        }

        /// <summary>
        /// Animação de expansão inicial da zona
        /// </summary>
        private IEnumerator ExpandAnimation()
        {
            float expandDuration = 0.5f;
            float elapsed = 0f;
            Vector3 targetScale = Vector3.one * (zoneRadius * 2);

            transform.localScale = Vector3.zero;

            while (elapsed < expandDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / expandDuration;
                transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, t);
                yield return null;
            }

            transform.localScale = targetScale;
        }

        #endregion

        #region Lifetime Management

        /// <summary>
        /// Gerencia tempo de vida da zona
        /// </summary>
        private IEnumerator LifetimeCoroutine()
        {
            while (lifetime < duration)
            {
                lifetime += Time.deltaTime;

                // Efeito de pulsação nos últimos segundos
                if (duration - lifetime < 2f)
                {
                    PulseEffect();
                }

                yield return null;
            }

            // Destroi a zona quando tempo acabar
            DeactivateZone();
        }

        /// <summary>
        /// Efeito visual de pulsação (aviso de expiração)
        /// </summary>
        private void PulseEffect()
        {
            if (zoneRenderer == null)
                return;

            float pulse = Mathf.PingPong(Time.time * 3f, 1f);
            Color currentColor = zoneColor;
            currentColor.a = Mathf.Lerp(0.1f, 0.5f, pulse);
            zoneRenderer.material.color = currentColor;
        }

        /// <summary>
        /// Desativa a zona e remove efeitos de todos os jogadores
        /// </summary>
        private void DeactivateZone()
        {
            if (!isActive)
                return;

            isActive = false;

            // Remove slow de todos os jogadores afetados
            foreach (PlayerController player in affectedPlayers)
            {
                if (player != null)
                {
                    player.RemoveSlowZone();
                }
            }

            affectedPlayers.Clear();
            affectedPlayerIDs.Clear();

            // Animação de desaparecimento
            StartCoroutine(ShrinkAndDestroy());
        }

        /// <summary>
        /// Animação de encolhimento antes de destruir
        /// </summary>
        private IEnumerator ShrinkAndDestroy()
        {
            float shrinkDuration = 0.5f;
            float elapsed = 0f;
            Vector3 originalScale = transform.localScale;

            // Para partículas
            if (slowParticles != null)
                slowParticles.Stop();

            // Fade out do som
            if (audioSource != null && audioSource.isPlaying)
            {
                float originalVolume = audioSource.volume;
                while (elapsed < shrinkDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / shrinkDuration;

                    transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
                    audioSource.volume = Mathf.Lerp(originalVolume, 0f, t);

                    yield return null;
                }
            }
            else
            {
                while (elapsed < shrinkDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / shrinkDuration;
                    transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
                    yield return null;
                }
            }

            // Destroi via Photon
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.Destroy(gameObject);
            }
        }

        #endregion

        #region Collision Detection

        private void OnTriggerEnter(Collider other)
        {
            if (!isActive)
                return;

            // Detecta jogador entrando na zona
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null && player.photonView.Owner.ActorNumber != ownerActorNumber)
            {
                ApplySlowToPlayer(player);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!isActive)
                return;

            // Detecta jogador saindo da zona
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null && affectedPlayerIDs.Contains(player.photonView.Owner.ActorNumber))
            {
                RemoveSlowFromPlayer(player);
            }
        }

        #endregion

        #region Slow Application

        /// <summary>
        /// Aplica slow em um jogador
        /// </summary>
        private void ApplySlowToPlayer(PlayerController player)
        {
            int actorNumber = player.photonView.Owner.ActorNumber;

            if (affectedPlayerIDs.Contains(actorNumber))
                return;

            affectedPlayerIDs.Add(actorNumber);
            affectedPlayers.Add(player);

            // Aplica slow
            player.ApplySlowZone(slowPercent);

            Debug.Log($"Slow aplicado em {player.photonView.Owner.NickName} ({slowPercent * 100}% velocidade)");
        }

        /// <summary>
        /// Remove slow de um jogador
        /// </summary>
        private void RemoveSlowFromPlayer(PlayerController player)
        {
            int actorNumber = player.photonView.Owner.ActorNumber;

            if (!affectedPlayerIDs.Contains(actorNumber))
                return;

            affectedPlayerIDs.Remove(actorNumber);
            affectedPlayers.Remove(player);

            // Remove slow
            player.RemoveSlowZone();

            Debug.Log($"Slow removido de {player.photonView.Owner.NickName}");
        }

        #endregion

        #region Debug

        private void OnDrawGizmos()
        {
            // Visualiza raio da zona no editor
            Gizmos.color = new Color(0.5f, 0f, 0.8f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, zoneRadius);
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            // Remove slow de todos os jogadores ao destruir
            foreach (PlayerController player in affectedPlayers)
            {
                if (player != null)
                {
                    player.RemoveSlowZone();
                }
            }
        }

        #endregion
    }
}