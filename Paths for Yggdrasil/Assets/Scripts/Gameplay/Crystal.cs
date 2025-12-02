using UnityEngine;
using Photon.Pun;
using QuantumHeist.Game;

/// <summary>
/// Sistema de cristal coletável que concede pontos e boost de velocidade
/// Sincronizado via Photon para multiplayer
/// </summary>
public class Crystal : MonoBehaviourPunCallbacks
{
    [Header("Crystal Settings")]
    [SerializeField] private int pointsValue = 10;
    [SerializeField] private float speedBoostMultiplier = 1.5f;
    [SerializeField] private float speedBoostDuration = 3f;
    [SerializeField] private float rotationSpeed = 50f;

    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem collectEffect;
    [SerializeField] private AudioClip collectSound;

    private void Update()
    {
        // Rotação visual do cristal
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Apenas o jogador local pode coletar (evita coleta duplicada)
        if (!PhotonNetwork.IsConnected || photonView.IsMine || PhotonNetwork.IsMasterClient)
        {
            PlayerController player = other.GetComponent<PlayerController>();

            if (player != null)
            {
                // Chama coleta no jogador
                player.CollectCrystal(pointsValue, speedBoostMultiplier, speedBoostDuration);

                // Efeitos visuais/sonoros
                PlayCollectEffects();

                // Destroi o cristal na rede
                if (PhotonNetwork.IsConnected && photonView.IsMine)
                {
                    PhotonNetwork.Destroy(gameObject);
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }
    }

    /// <summary>
    /// Reproduz efeitos de coleta
    /// </summary>
    private void PlayCollectEffects()
    {
        // Partículas
        if (collectEffect != null)
        {
            ParticleSystem effect = Instantiate(collectEffect, transform.position, Quaternion.identity);
            Destroy(effect.gameObject, 2f);
        }

        // Som
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
        }
    }
}