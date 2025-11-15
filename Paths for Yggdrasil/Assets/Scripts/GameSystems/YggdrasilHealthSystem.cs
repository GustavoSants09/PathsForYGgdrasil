using UnityEngine;
using Photon.Pun;
using Yggdrasil.Core;
using Yggdrasil.Managers;

namespace Yggdrasil.GameSystems
{
    public class YggdrasilHealthSystem : MonoBehaviourPunCallbacks
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float healthDrainRate = 0.5f; // HP por segundo
        [SerializeField] private float altarHealAmount = 30f;

        private float currentHealth;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public float HealthPercentage => currentHealth / maxHealth;

        private void Start()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                currentHealth = maxHealth;
                SyncHealth(currentHealth);
            }
        }

        private void Update()
        {
            if (!PhotonNetwork.IsMasterClient) return;
            if (!GameManager.Instance.IsGameActive) return;

            // Drena vida continuamente
            currentHealth -= healthDrainRate * Time.deltaTime;
            currentHealth = Mathf.Max(0, currentHealth);

            SyncHealth(currentHealth);

            if (currentHealth <= 0)
            {
                OnYggdrasilDied();
            }
        }

        public void HealFromAltar()
        {
            if (!PhotonNetwork.IsMasterClient) return;

            currentHealth = Mathf.Min(currentHealth + altarHealAmount, maxHealth);
            SyncHealth(currentHealth);

            EventSystem.Instance.TriggerEvent(GameEvents.YGGDRASIL_HEALED, currentHealth);
        }

        private void SyncHealth(float health)
        {
            photonView.RPC("RPC_UpdateHealth", RpcTarget.All, health);
        }

        [PunRPC]
        private void RPC_UpdateHealth(float health)
        {
            currentHealth = health;
            EventSystem.Instance.TriggerEvent(GameEvents.YGGDRASIL_HEALTH_CHANGED, currentHealth);
        }

        private void OnYggdrasilDied()
        {
            EventSystem.Instance.TriggerEvent(GameEvents.YGGDRASIL_DIED, null);
            GameManager.Instance.GameOver("Yggdrasil morreu!");
        }
    }
}