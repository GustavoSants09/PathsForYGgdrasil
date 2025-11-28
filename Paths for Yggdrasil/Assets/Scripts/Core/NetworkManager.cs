using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

namespace CargoClash.Core
{
    /// <summary>
    /// Gerenciador central de networking usando Photon PUN 2.
    /// Implementa padrão Singleton para acesso global.
    /// Responsável por conexão, criação/entrada em salas e callbacks de rede.
    /// </summary>
    public class NetworkManager : MonoBehaviourPunCallbacks
    {
        public static NetworkManager Instance { get; private set; }

        [Header("Connection Settings")]
        [Tooltip("Versão do jogo - separa diferentes versões em lobbies distintos")]
        [SerializeField] private string gameVersion = "1.0";

        [Tooltip("Máximo de jogadores por sala")]
        [SerializeField] private byte maxPlayersPerRoom = 2;

        [Header("Status")]
        [SerializeField] private bool isConnecting = false;

        // Eventos para UI reagir
        public System.Action<string> OnConnectionStatusChanged;
        public System.Action OnJoinedRoomSuccessfully;
        public System.Action<string> OnConnectionFailed;

        private void Awake()
        {
            // Singleton pattern com proteção contra duplicatas
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Configuração crítica: sincroniza cena automaticamente
            PhotonNetwork.AutomaticallySyncScene = true;
        }

        /// <summary>
        /// Conecta aos servidores Photon.
        /// </summary>
        public void Connect()
        {
            if (isConnecting) return;

            isConnecting = true;
            OnConnectionStatusChanged?.Invoke("Conectando...");

            if (PhotonNetwork.IsConnected)
            {
                // Já conectado, vai direto para lobby
                JoinLobby();
            }
            else
            {
                // Inicia conexão
                PhotonNetwork.GameVersion = gameVersion;
                PhotonNetwork.ConnectUsingSettings();
            }
        }

        /// <summary>
        /// Cria uma sala com nome único.
        /// </summary>
        public void CreateRoom(string roomName)
        {
            if (string.IsNullOrEmpty(roomName))
            {
                roomName = $"Room_{Random.Range(1000, 9999)}";
            }

            OnConnectionStatusChanged?.Invoke($"Criando sala: {roomName}");

            RoomOptions roomOptions = new RoomOptions
            {
                MaxPlayers = maxPlayersPerRoom,
                IsVisible = true,
                IsOpen = true
            };

            PhotonNetwork.CreateRoom(roomName, roomOptions);
        }

        /// <summary>
        /// Entra em sala aleatória ou cria uma se não houver disponível.
        /// </summary>
        public void JoinRandomRoom()
        {
            OnConnectionStatusChanged?.Invoke("Procurando sala...");
            PhotonNetwork.JoinRandomRoom();
        }

        /// <summary>
        /// Entra em sala específica pelo nome.
        /// </summary>
        public void JoinRoom(string roomName)
        {
            OnConnectionStatusChanged?.Invoke($"Entrando em: {roomName}");
            PhotonNetwork.JoinRoom(roomName);
        }

        /// <summary>
        /// Desconecta e retorna ao menu inicial.
        /// </summary>
        public void Disconnect()
        {
            PhotonNetwork.Disconnect();
            OnConnectionStatusChanged?.Invoke("Desconectando...");
        }

        // ===== PHOTON CALLBACKS =====

        public override void OnConnectedToMaster()
        {
            Debug.Log("[NetworkManager] Conectado ao Master Server");
            OnConnectionStatusChanged?.Invoke("Conectado");
            isConnecting = false;
            JoinLobby();
        }

        public override void OnJoinedLobby()
        {
            Debug.Log("[NetworkManager] Entrou no Lobby");
            OnConnectionStatusChanged?.Invoke("No Lobby - Pronto");
        }

        public override void OnCreatedRoom()
        {
            Debug.Log($"[NetworkManager] Sala criada: {PhotonNetwork.CurrentRoom.Name}");
        }

        public override void OnJoinedRoom()
        {
            Debug.Log($"[NetworkManager] Entrou na sala: {PhotonNetwork.CurrentRoom.Name}");
            Debug.Log($"[NetworkManager] Jogadores na sala: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");

            OnConnectionStatusChanged?.Invoke($"Na sala: {PhotonNetwork.CurrentRoom.Name}");
            OnJoinedRoomSuccessfully?.Invoke();
        }

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            Debug.Log("[NetworkManager] Nenhuma sala encontrada. Criando nova...");
            CreateRoom(null); // Nome aleatório
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            Debug.LogError($"[NetworkManager] Falha ao entrar na sala: {message}");
            OnConnectionFailed?.Invoke($"Erro: {message}");
            OnConnectionStatusChanged?.Invoke("Falha ao entrar");
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            Debug.LogError($"[NetworkManager] Falha ao criar sala: {message}");
            OnConnectionFailed?.Invoke($"Erro: {message}");
            OnConnectionStatusChanged?.Invoke("Falha ao criar sala");
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            Debug.LogWarning($"[NetworkManager] Desconectado: {cause}");
            OnConnectionStatusChanged?.Invoke($"Desconectado: {cause}");
            isConnecting = false;
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            Debug.Log($"[NetworkManager] Jogador entrou: {newPlayer.NickName}");
            OnConnectionStatusChanged?.Invoke($"Jogador entrou: {newPlayer.NickName}");
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            Debug.Log($"[NetworkManager] Jogador saiu: {otherPlayer.NickName}");
            OnConnectionStatusChanged?.Invoke($"Jogador saiu: {otherPlayer.NickName}");
        }

        private void JoinLobby()
        {
            if (!PhotonNetwork.InLobby)
            {
                PhotonNetwork.JoinLobby();
            }
        }

        // ===== UTILITY METHODS =====

        /// <summary>
        /// Verifica se o jogador local é o Master Client (host).
        /// </summary>
        public bool IsMasterClient()
        {
            return PhotonNetwork.IsMasterClient;
        }

        /// <summary>
        /// Retorna informações da sala atual.
        /// </summary>
        public string GetRoomInfo()
        {
            if (!PhotonNetwork.InRoom) return "Não está em uma sala";

            return $"{PhotonNetwork.CurrentRoom.Name} ({PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers})";
        }
    }
}