using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

namespace QuantumHeist.Network
{
    /// <summary>
    /// Valida e gerencia configurações de conexão Photon
    /// Deve ser anexado a um GameObject na primeira cena
    /// </summary>
    public class PhotonConnectionSettings : MonoBehaviour
    {
        [Header("Debug Options")]
        [SerializeField] private bool showDebugLogs = true;
        [SerializeField] private bool autoConnect = true;

        [Header("Connection Settings")]
        [SerializeField] private string gameVersion = "1.0";
        [SerializeField] private AppSettings customAppSettings;

        private void Awake()
        {
            // Garante que o objeto persista entre cenas
            DontDestroyOnLoad(gameObject);

            // Configura logging
            PhotonNetwork.LogLevel = showDebugLogs ? PunLogLevel.Full : PunLogLevel.ErrorsOnly;

            // Valida configurações
            ValidatePhotonSettings();
        }

        private void Start()
        {
            if (autoConnect && !PhotonNetwork.IsConnected)
            {
                ConnectToPhoton();
            }
        }

        /// <summary>
        /// Valida configurações do Photon
        /// </summary>
        private void ValidatePhotonSettings()
        {
            if (PhotonNetwork.PhotonServerSettings == null)
            {
                Debug.LogError("PhotonServerSettings não encontrado! " +
                    "Configure em Window > Photon Unity Networking > Highlight Server Settings");
                return;
            }

            // Valida App ID
            if (string.IsNullOrEmpty(PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime))
            {
                Debug.LogError("App ID do Photon não configurado! " +
                    "Adicione seu App ID em PhotonServerSettings");
                return;
            }

            // Aplica configurações customizadas se fornecidas
            if (customAppSettings != null)
            {
                PhotonNetwork.PhotonServerSettings.AppSettings = customAppSettings;
            }

            Debug.Log("✓ Configurações do Photon validadas com sucesso");
        }

        /// <summary>
        /// Conecta ao Photon
        /// </summary>
        public void ConnectToPhoton()
        {
            if (PhotonNetwork.IsConnected)
            {
                Debug.LogWarning("Já conectado ao Photon");
                return;
            }

            PhotonNetwork.AutomaticallySyncScene = true;
            PhotonNetwork.GameVersion = gameVersion;

            Debug.Log($"Conectando ao Photon... Versão: {gameVersion}");
            PhotonNetwork.ConnectUsingSettings();
        }

        /// <summary>
        /// Desconecta do Photon
        /// </summary>
        public void DisconnectFromPhoton()
        {
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Disconnect();
            }
        }
    }
}