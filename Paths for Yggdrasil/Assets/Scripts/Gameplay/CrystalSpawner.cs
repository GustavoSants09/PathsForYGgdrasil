using UnityEngine;
using Photon.Pun;
using System.Collections;

/// <summary>
/// Gerenciador de spawn de cristais em posições aleatórias
/// Apenas o MasterClient spawna para evitar duplicação
/// </summary>
public class CrystalSpawner : MonoBehaviourPunCallbacks
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject crystalPrefab;
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private int maxCrystalsInScene = 10;

    [Header("Spawn Area")]
    [SerializeField] private Vector3 spawnAreaCenter = Vector3.zero;
    [SerializeField] private Vector3 spawnAreaSize = new Vector3(50f, 0f, 50f);
    [SerializeField] private float spawnHeight = 1f;

    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float maxGroundCheckDistance = 100f;

    private int currentCrystalCount = 0;
    private bool isSpawning = false;

    private void Start()
    {
        // Apenas MasterClient spawna cristais
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(SpawnRoutine());
        }
    }

    /// <summary>
    /// Rotina de spawn contínuo de cristais
    /// </summary>
    private IEnumerator SpawnRoutine()
    {
        isSpawning = true;

        while (isSpawning)
        {
            yield return new WaitForSeconds(spawnInterval);

            // Só spawna se não atingiu o máximo
            if (currentCrystalCount < maxCrystalsInScene)
            {
                SpawnCrystal();
            }
        }
    }

    /// <summary>
    /// Spawna um cristal em posição aleatória válida
    /// </summary>
    private void SpawnCrystal()
    {
        Vector3 spawnPosition = GetRandomSpawnPosition();

        if (spawnPosition != Vector3.zero)
        {
            if (PhotonNetwork.IsConnected)
            {
                // Spawn via Photon
                GameObject crystal = PhotonNetwork.Instantiate(
                    crystalPrefab.name,
                    spawnPosition,
                    Quaternion.identity
                );

                currentCrystalCount++;

                Debug.Log($"[CrystalSpawner] Crystal spawned at {spawnPosition}. Total: {currentCrystalCount}");
            }
            else
            {
                // Modo offline
                GameObject crystal = Instantiate(crystalPrefab, spawnPosition, Quaternion.identity);
                currentCrystalCount++;
            }
        }
    }

    /// <summary>
    /// Calcula posição aleatória válida para spawn
    /// </summary>
    private Vector3 GetRandomSpawnPosition()
    {
        int maxAttempts = 10;

        for (int i = 0; i < maxAttempts; i++)
        {
            // Posição aleatória dentro da área
            float randomX = Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2);
            float randomZ = Random.Range(-spawnAreaSize.z / 2, spawnAreaSize.z / 2);

            Vector3 randomPosition = spawnAreaCenter + new Vector3(randomX, spawnHeight, randomZ);

            // Tenta encontrar o chão abaixo
            RaycastHit hit;
            if (Physics.Raycast(randomPosition + Vector3.up * 10f, Vector3.down, out hit, maxGroundCheckDistance, groundLayer))
            {
                // Spawna um pouco acima do chão
                return hit.point + Vector3.up * spawnHeight;
            }
        }

        Debug.LogWarning("[CrystalSpawner] Failed to find valid spawn position");
        return Vector3.zero;
    }

    /// <summary>
    /// Callback quando cristal é destruído
    /// </summary>
    public void OnCrystalDestroyed()
    {
        currentCrystalCount--;
        currentCrystalCount = Mathf.Max(0, currentCrystalCount);
    }

    /// <summary>
    /// Visualiza área de spawn no editor
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(spawnAreaCenter, spawnAreaSize);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(spawnAreaCenter, 1f);
    }

    private void OnDisable()
    {
        isSpawning = false;
    }
}