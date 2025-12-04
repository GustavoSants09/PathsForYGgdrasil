using UnityEngine;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;

namespace QuantumHeist.Game
{
    public class SlowZone : MonoBehaviourPunCallbacks
    {
        [Header("Zone Settings")]
        [SerializeField] private float slowPercentage = 0.5f; // 50% da velocidade
        [SerializeField] private float zoneDuration = 6f;
        [SerializeField] private float zoneRadius = 3f;

        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem zoneParticles;
        [SerializeField] private Light zoneLight;
        [SerializeField] private Material zoneMaterial;
        [SerializeField] private Color zoneColor = new Color(0.5f, 0f, 1f, 0.5f); // Roxo translúcido

        [Header("Audio")]
        [SerializeField] private AudioClip deploySound;
        [SerializeField] private AudioClip ambientSound;
        [SerializeField] private AudioClip expireSound;

        private PhotonView photonView;
        private AudioSource audioSource;
        private SphereCollider zoneTrigger;
        private int ownerViewID;
        private HashSet<PlayerController> affectedPlayers = new HashSet<PlayerController>();
        private MeshRenderer zoneRenderer;

        #region Initialization

        private void Awake()
        {
            photonView = GetComponent<PhotonView>();

            // Setup collider
            zoneTrigger = gameObject.AddComponent<SphereCollider>();
            zoneTrigger.isTrigger = true;
            zoneTrigger.radius = zoneRadius;

            // Setup audio
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // Som 3D
            audioSource.maxDistance = 20f;
            audioSource.loop = true;

            // Setup visual
            SetupVisuals();
        }

        private void SetupVisuals()
        {
            // Cria esfera visual
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.SetParent(transform);
            sphere.transform.localPosition = Vector3.zero;
            sphere.transform.localScale = Vector3.one * (zoneRadius * 2);

            // Remove collider da esfera visual
            Destroy(sphere.GetComponent<SphereCollider>());

            zoneRenderer = sphere.GetComponent<MeshRenderer>();

            if (zoneMaterial != null)
            {
                zoneRenderer.material = zoneMaterial;
            }
            else
            {
                // Material padrão translúcido
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = zoneColor;
                mat.SetFloat("_Mode", 3); // Transparent mode
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
                zoneRenderer.material = mat;
            }

            // Setup luz
            if (zoneLight != null)
            {
                zoneLight.color = zoneColor;
                zoneLight.range = zoneRadius * 1.5f;
                zoneLight.intensity = 2f;
            }
            else
            {
                GameObject lightObj = new GameObject("ZoneLight");
                lightObj.transform.SetParent(transform);
                lightObj.transform.localPosition = Vector3.zero;

                zoneLight = lightObj.AddComponent<Light>();
                zoneLight.type = LightType.Point;
                zoneLight.color = zoneColor;
                zoneLight.range = zoneRadius * 1.5f;
                zoneLight.intensity = 2f;
            }

            // Setup partículas
            if (zoneParticles == null)
            {
                CreateDefaultParticles();
            }
        }

        private void CreateDefaultParticles()
        {
            GameObject particlesObj = new GameObject("ZoneParticles");
            particlesObj.transform.SetParent(transform);
            particlesObj.transform.localPosition = Vector3.zero;

            zoneParticles = particlesObj.AddComponent<ParticleSystem>();

            var main = zoneParticles.main;
            main.startColor = zoneColor;
            main.startSize = 0.2f;
            main.startLifetime = 2f;
            main.maxParticles = 50;
            main.loop = true;

            var emission = zoneParticles.emission;
            emission.rateOverTime = 20;

            var shape = zoneParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = zoneRadius;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Inicializa a zona com o owner
        /// </summary>
        public void Initialize(int ownerPhotonViewID)
        {
            ownerViewID = ownerPhotonViewID;

            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC("RPC_ActivateZone", RpcTarget.AllBuffered, ownerPhotonViewID);
            }
        }

        #endregion

        #region RPC Methods

        [PunRPC]
        private void RPC_ActivateZone(int ownerID)
        {
            ownerViewID = ownerID;

            Debug.Log($"[SlowZone] 🟣 Zona de desaceleração ativada por ViewID: {ownerViewID}");

            // Efeitos sonoros
            if (audioSource != null && deploySound != null)
            {
                audioSource.PlayOneShot(deploySound);
            }

            if (audioSource != null && ambientSound != null)
            {
                audioSource.clip = ambientSound;
                audioSource.Play();
            }

            // Inicia partículas
            if (zoneParticles != null)
            {
                zoneParticles.Play();
            }

            // Inicia timer de expiração
            StartCoroutine(ZoneDurationTimer());
        }

        #endregion

        #region Zone Logic

        private IEnumerator ZoneDurationTimer()
        {
            float elapsed = 0f;

            while (elapsed < zoneDuration)
            {
                elapsed += Time.deltaTime;

                // Efeito de pulsação visual
                float pulseScale = 1f + Mathf.Sin(elapsed * 3f) * 0.1f;
                if (zoneLight != null)
                {
                    zoneLight.intensity = 2f * pulseScale;
                }

                yield return null;
            }

            // Zona expirou
            DestroyZone();
        }

        private void DestroyZone()
        {
            Debug.Log($"[SlowZone] ⏱️ Zona expirada");

            // Remove efeitos de todos os jogadores afetados
            foreach (PlayerController player in affectedPlayers)
            {
                if (player != null)
                {
                    player.RemoveSlowEffect(this);
                }
            }
            affectedPlayers.Clear();

            // Efeito sonoro de expiração
            if (expireSound != null)
            {
                AudioSource.PlayClipAtPoint(expireSound, transform.position);
            }

            // Destroi objeto
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.Destroy(gameObject);
            }
        }

        #endregion

        #region Trigger Events

        private void OnTriggerEnter(Collider other)
        {
            PlayerController player = other.GetComponent<PlayerController>();

            if (player == null || !player.photonView.IsMine)
                return;

            // Não afeta o dono da zona
            if (player.photonView.ViewID == ownerViewID)
            {
                Debug.Log($"[SlowZone] ℹ️ Dono da zona não é afetado");
                return;
            }

            // Aplica efeito de desaceleração
            if (!affectedPlayers.Contains(player))
            {
                affectedPlayers.Add(player);
                player.ApplySlowEffect(slowPercentage, this);

                Debug.Log($"[SlowZone] 🐌 {player.photonView.Owner.NickName} entrou na zona de desaceleração");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            PlayerController player = other.GetComponent<PlayerController>();

            if (player == null || !player.photonView.IsMine)
                return;

            if (affectedPlayers.Contains(player))
            {
                affectedPlayers.Remove(player);
                player.RemoveSlowEffect(this);

                Debug.Log($"[SlowZone] ✅ {player.photonView.Owner.NickName} saiu da zona de desaceleração");
            }
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            // Garante que todos os efeitos sejam removidos
            foreach (PlayerController player in affectedPlayers)
            {
                if (player != null)
                {
                    player.RemoveSlowEffect(this);
                }
            }
        }

        #endregion

        #region Editor Helpers

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.5f, 0f, 1f, 0.3f);
            Gizmos.DrawSphere(transform.position, zoneRadius);

            Gizmos.color = new Color(0.5f, 0f, 1f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, zoneRadius);
        }

        #endregion
    }
}