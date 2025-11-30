using UnityEngine;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;

namespace QuantumHeist.Game
{
    /// <summary>
    /// Gerencia o spawn de cristais na arena
    /// Apenas o Master Client spawna cristais para garantir sincronização
    /// </summary>
    public class CrystalSpawner : MonoBehaviourPunCallbacks
    {
        public static CrystalSpawner Instance { get; private set; }

        [Header("Spawn Settings")]
        [SerializeField] private GameObject crystalPrefab;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private int maxCrystalsInScene = 10;
        [SerializeField] private float spawnInterval = 3f;
        [SerializeField] private float initialSpawnDelay = 2f;

        [Header("Crystal Types")]
        [SerializeField] private int normalCrystalValue = 10;
        [SerializeField] private int rareCrystalValue = 25;
        [SerializeField] private int epicCrystalValue = 50;
        [SerializeField, Range(0f, 1f)] private float rareSpawnChance = 0.2f; // 20%
        [SerializeField, Range(0f, 1f)] private float epicSpawnChance = 0.05f; // 5%

        // Controle de cristais ativos
        private List<GameObject> activeCrystals = new List<GameObject>();
        private List<int> availableSpawnIndices = new List<int>();
        private bool isSpawning = false;

        #region Unity Callbacks

        private void Awake()
        {
            // Implementa Singleton
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Valida configuração
            if (crystalPrefab == null)
            {
                Debug.LogError("CrystalPrefab não configurado no CrystalSpawner!");
                enabled = false;
                return;
            }

            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogError("SpawnPoints não configurados no CrystalSpawner!");
                enabled = false;
                return;
            }

            // Inicializa lista de índices disponíveis
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                availableSpawnIndices.Add(i);
            }

            // Apenas Master Client spawna cristais
            if (PhotonNetwork.IsMasterClient)
            {
                StartCoroutine(SpawnRoutine());
            }
        }

        #endregion

        #region Spawn Logic

        /// <summary>
        /// Rotina principal de spawn de cristais
        /// </summary>
        private IEnumerator SpawnRoutine()
        {
            isSpawning = true;

            // Aguarda antes do primeiro spawn
            yield return new WaitForSeconds(initialSpawnDelay);

            // Spawn inicial para preencher a arena
            int initialSpawnCount = Mathf.Min(maxCrystalsInScene, spawnPoints.Length);
            for (int i = 0; i < initialSpawnCount; i++)
            {
                SpawnCrystal();
                yield return new WaitForSeconds(0.5f); // Pequeno delay entre spawns iniciais
            }

            // Loop contínuo de spawn
            while (isSpawning)
            {
                yield return new WaitForSeconds(spawnInterval);

                // Spawna novo cristal se houver espaço
                if (activeCrystals.Count < maxCrystalsInScene && availableSpawnIndices.Count > 0)
                {
                    SpawnCrystal();
                }
            }
        }

        /// <summary>
        /// Spawna um cristal em posição aleatória disponível
        /// </summary>
        private void SpawnCrystal()
        {
            if (availableSpawnIndices.Count == 0)
            {
                Debug.LogWarning("Nenhum spawn point disponível!");
                return;
            }

            // Escolhe spawn point aleatório dos disponíveis
            int randomIndex = Random.Range(0, availableSpawnIndices.Count);
            int spawnIndex = availableSpawnIndices[randomIndex];
            availableSpawnIndices.RemoveAt(randomIndex);

            Transform spawnPoint = spawnPoints[spawnIndex];

            // Determina tipo do cristal baseado em probabilidade
            CrystalType crystalType = DetermineCrystalType();
            int crystalValue = GetCrystalValue(crystalType);

            // Instancia cristal via Photon
            GameObject crystal = PhotonNetwork.Instantiate(
                crystalPrefab.name,
                spawnPoint.position,
                Quaternion.identity
            );

            // Configura o cristal
            Crystal crystalScript = crystal.GetComponent<Crystal>();
            if (crystalScript != null)
            {
                crystalScript.Initialize(crystalType, crystalValue, spawnIndex);
            }

            // Adiciona à lista de cristais ativos
            activeCrystals.Add(crystal);

            Debug.Log($"Cristal {crystalType} ({crystalValue} pontos) spawnado em {spawnPoint.position}");
        }

        /// <summary>
        /// Determina o tipo do cristal baseado em probabilidade
        /// </summary>
        private CrystalType DetermineCrystalType()
        {
            float roll = Random.value;

            if (roll <= epicSpawnChance)
            {
                return CrystalType.Epic;
            }
            else if (roll <= epicSpawnChance + rareSpawnChance)
            {
                return CrystalType.Rare;
            }
            else
            {
                return CrystalType.Normal;
            }
        }

        /// <summary>
        /// Retorna o valor em pontos do tipo de cristal
        /// </summary>
        private int GetCrystalValue(CrystalType type)
        {
            switch (type)
            {
                case CrystalType.Normal:
                    return normalCrystalValue;
                case CrystalType.Rare:
                    return rareCrystalValue;
                case CrystalType.Epic:
                    return epicCrystalValue;
                default:
                    return normalCrystalValue;
            }
        }

        #endregion

        #region Crystal Management

        /// <summary>
        /// Registra que um cristal foi coletado e libera o spawn point
        /// </summary>
        public void OnCrystalCollected(GameObject crystal, int spawnIndex)
        {
            if (activeCrystals.Contains(crystal))
            {
                activeCrystals.Remove(crystal);
            }

            // Libera o spawn point para uso futuro
            if (!availableSpawnIndices.Contains(spawnIndex))
            {
                availableSpawnIndices.Add(spawnIndex);
            }

            Debug.Log($"Cristal coletado. Cristais ativos: {activeCrystals.Count}/{maxCrystalsInScene}");
        }

        /// <summary>
        /// Remove cristal da lista sem respawn (usado para limpeza)
        /// </summary>
        public void RemoveCrystal(GameObject crystal, int spawnIndex)
        {
            if (activeCrystals.Contains(crystal))
            {
                activeCrystals.Remove(crystal);
            }

            if (!availableSpawnIndices.Contains(spawnIndex))
            {
                availableSpawnIndices.Add(spawnIndex);
            }
        }

        #endregion

        #region Photon Callbacks

        public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
        {
            // Se este cliente se tornou o novo Master Client, inicia spawn
            if (PhotonNetwork.IsMasterClient && !isSpawning)
            {
                StartCoroutine(SpawnRoutine());
            }
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            // Para a rotina de spawn
            isSpawning = false;
            StopAllCoroutines();
        }

        #endregion
    }

    /// <summary>
    /// Tipos de cristais disponíveis
    /// </summary>
    public enum CrystalType
    {
        Normal,  // 10 pontos - Cor: Azul
        Rare,    // 25 pontos - Cor: Roxo
        Epic     // 50 pontos - Cor: Dourado
    }
}