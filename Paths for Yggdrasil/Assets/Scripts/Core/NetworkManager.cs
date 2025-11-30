using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Collections.Generic;

namespace QuantumHeist.Network
{
    /// <summary>
    /// Gerencia toda a comunicação com Photon PUN2
    /// Responsável por conexão, criação/listagem de salas e callbacks de rede
    /// </summary>
    public class NetworkManager : MonoBehaviourPunCallbacks
    {
        [Header("Configurações de Conexão")]
        [SerializeField] private string gameVersion = "1.0";
        [SerializeField] private byte maxPlayersPerRoom = 4;

        [Header("UI References")]
        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private GameObject roomPanel;
        [SerializeField] private TMP_InputField roomNameInput;
        [SerializeField] private TMP_InputField playerNameInput;
        [SerializeField] private TMP_Text connectionStatusText;
        [SerializeField] private TMP_Text roomStatusText;
        [SerializeField] private Transform roomListContent;
        [SerializeField] private GameObject roomListItemPrefab;
        [SerializeField] private Transform playerListContent;
        [SerializeField] private GameObject playerListItemPrefab;
        [SerializeField] private GameObject startGameButton;

        private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();

        #region Unity Callbacks

        private void Start()
        {
            // Define o nome do jogador baseado em PlayerPrefs ou valor padrão
            if (PlayerPrefs.HasKey("PlayerName"))
            {
                playerNameInput.text = PlayerPrefs.GetString("PlayerName");
            }
            else
            {
                playerNameInput.text = "Player" + Random.Range(1000, 9999);
            }

            // Inicia conexão automática ao servidor Photon
            ConnectToPhoton();
        }

        #endregion

        #region Conexão Photon

        /// <summary>
        /// Inicia conexão com servidores Photon
        /// </summary>
        public void ConnectToPhoton()
        {
            UpdateConnectionStatus("Conectando ao servidor...");

            PhotonNetwork.AutomaticallySyncScene = true; // Sincroniza cenas automaticamente
            PhotonNetwork.GameVersion = gameVersion;
            PhotonNetwork.NickName = playerNameInput.text;

            PhotonNetwork.ConnectUsingSettings();
        }

        /// <summary>
        /// Desconecta do servidor Photon
        /// </summary>
        public void DisconnectFromPhoton()
        {
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Disconnect();
                UpdateConnectionStatus("Desconectando...");
            }
        }

        #endregion

        #region Gerenciamento de Salas

        /// <summary>
        /// Cria uma nova sala com o nome especificado
        /// </summary>
        public void CreateRoom()
        {
            if (string.IsNullOrEmpty(roomNameInput.text))
            {
                UpdateConnectionStatus("ERRO: Nome da sala não pode estar vazio!");
                return;
            }

            // Salva o nome do jogador
            PlayerPrefs.SetString("PlayerName", playerNameInput.text);
            PhotonNetwork.NickName = playerNameInput.text;

            RoomOptions roomOptions = new RoomOptions
            {
                MaxPlayers = maxPlayersPerRoom,
                IsVisible = true,
                IsOpen = true
            };

            UpdateConnectionStatus($"Criando sala '{roomNameInput.text}'...");
            PhotonNetwork.CreateRoom(roomNameInput.text, roomOptions);
        }

        /// <summary>
        /// Entra em uma sala específica pelo nome
        /// </summary>
        public void JoinRoom(string roomName)
        {
            PlayerPrefs.SetString("PlayerName", playerNameInput.text);
            PhotonNetwork.NickName = playerNameInput.text;

            UpdateConnectionStatus($"Entrando na sala '{roomName}'...");
            PhotonNetwork.JoinRoom(roomName);
        }

        /// <summary>
        /// Sai da sala atual e retorna ao lobby
        /// </summary>
        public void LeaveRoom()
        {
            UpdateConnectionStatus("Saindo da sala...");
            PhotonNetwork.LeaveRoom();
        }

        /// <summary>
        /// Inicia o jogo (apenas Master Client pode executar)
        /// </summary>
        public void StartGame()
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.LogWarning("Apenas o Master Client pode iniciar o jogo!");
                return;
            }

            // Fecha a sala para novos jogadores
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.CurrentRoom.IsVisible = false;

            // Carrega a cena do jogo para todos os clientes
            PhotonNetwork.LoadLevel("GameScene");
        }

        #endregion

        #region Callbacks Photon - Conexão

        public override void OnConnectedToMaster()
        {
            UpdateConnectionStatus("Conectado ao servidor!");
            Debug.Log($"Conectado ao servidor Photon. Região: {PhotonNetwork.CloudRegion}");

            // Entra automaticamente no lobby
            PhotonNetwork.JoinLobby();
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            UpdateConnectionStatus($"Desconectado: {cause}");
            Debug.LogWarning($"Desconectado do Photon. Causa: {cause}");

            // Volta para o painel de lobby
            lobbyPanel.SetActive(true);
            roomPanel.SetActive(false);

            // Limpa cache de salas
            cachedRoomList.Clear();
        }

        public override void OnJoinedLobby()
        {
            UpdateConnectionStatus("No lobby - Pronto para jogar!");
            Debug.Log("Entrou no lobby");

            lobbyPanel.SetActive(true);
            roomPanel.SetActive(false);
        }

        #endregion

        #region Callbacks Photon - Salas

        public override void OnCreatedRoom()
        {
            Debug.Log($"Sala '{PhotonNetwork.CurrentRoom.Name}' criada com sucesso!");
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            UpdateConnectionStatus($"ERRO ao criar sala: {message}");
            Debug.LogError($"Falha ao criar sala. Código: {returnCode}, Mensagem: {message}");
        }

        public override void OnJoinedRoom()
        {
            UpdateConnectionStatus($"Na sala: {PhotonNetwork.CurrentRoom.Name}");
            UpdateRoomStatus();

            Debug.Log($"Entrou na sala '{PhotonNetwork.CurrentRoom.Name}'. Jogadores: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");

            // Muda para painel da sala
            lobbyPanel.SetActive(false);
            roomPanel.SetActive(true);

            // Atualiza lista de jogadores
            UpdatePlayerList();

            // Mostra botão de start apenas para Master Client
            startGameButton.SetActive(PhotonNetwork.IsMasterClient);
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            UpdateConnectionStatus($"ERRO ao entrar na sala: {message}");
            Debug.LogError($"Falha ao entrar na sala. Código: {returnCode}, Mensagem: {message}");
        }

        public override void OnLeftRoom()
        {
            UpdateConnectionStatus("Saiu da sala");
            Debug.Log("Saiu da sala");

            lobbyPanel.SetActive(true);
            roomPanel.SetActive(false);
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            Debug.Log($"Jogador '{newPlayer.NickName}' entrou na sala");
            UpdatePlayerList();
            UpdateRoomStatus();
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            Debug.Log($"Jogador '{otherPlayer.NickName}' saiu da sala");
            UpdatePlayerList();
            UpdateRoomStatus();
        }

        public override void OnMasterClientSwitched(Player newMasterClient)
        {
            Debug.Log($"Novo Master Client: {newMasterClient.NickName}");

            // Atualiza visibilidade do botão de start
            startGameButton.SetActive(PhotonNetwork.IsMasterClient);
        }

        #endregion

        #region Callbacks Photon - Listagem de Salas

        public override void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            // Atualiza cache de salas
            foreach (RoomInfo room in roomList)
            {
                if (room.RemovedFromList)
                {
                    cachedRoomList.Remove(room.Name);
                }
                else
                {
                    cachedRoomList[room.Name] = room;
                }
            }

            // Atualiza UI da lista de salas
            UpdateRoomList();
        }

        #endregion

        #region Atualização de UI

        /// <summary>
        /// Atualiza o texto de status de conexão
        /// </summary>
        private void UpdateConnectionStatus(string status)
        {
            if (connectionStatusText != null)
            {
                connectionStatusText.text = status;
            }
        }

        /// <summary>
        /// Atualiza o texto de status da sala atual
        /// </summary>
        private void UpdateRoomStatus()
        {
            if (roomStatusText != null && PhotonNetwork.InRoom)
            {
                string masterClientName = PhotonNetwork.MasterClient.NickName;
                int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;
                int maxPlayers = PhotonNetwork.CurrentRoom.MaxPlayers;

                roomStatusText.text = $"Sala: {PhotonNetwork.CurrentRoom.Name}\n" +
                                     $"Host: {masterClientName}\n" +
                                     $"Jogadores: {playerCount}/{maxPlayers}";
            }
        }

        /// <summary>
        /// Atualiza a lista visual de salas disponíveis
        /// </summary>
        private void UpdateRoomList()
        {
            // Limpa lista atual
            foreach (Transform child in roomListContent)
            {
                Destroy(child.gameObject);
            }

            // Cria item para cada sala disponível
            foreach (var roomEntry in cachedRoomList)
            {
                RoomInfo room = roomEntry.Value;

                // Ignora salas fechadas ou cheias
                if (!room.IsOpen || room.PlayerCount >= room.MaxPlayers)
                    continue;

                GameObject roomItem = Instantiate(roomListItemPrefab, roomListContent);
                RoomListItem roomListItem = roomItem.GetComponent<RoomListItem>();

                if (roomListItem != null)
                {
                    roomListItem.SetupRoom(room, this);
                }
            }
        }

        /// <summary>
        /// Atualiza a lista visual de jogadores na sala
        /// </summary>
        private void UpdatePlayerList()
        {
            if (!PhotonNetwork.InRoom)
                return;

            // Limpa lista atual
            foreach (Transform child in playerListContent)
            {
                Destroy(child.gameObject);
            }

            // Cria item para cada jogador na sala
            foreach (var playerEntry in PhotonNetwork.CurrentRoom.Players)
            {
                Player player = playerEntry.Value;

                GameObject playerItem = Instantiate(playerListItemPrefab, playerListContent);
                TMP_Text playerText = playerItem.GetComponentInChildren<TMP_Text>();

                if (playerText != null)
                {
                    string prefix = player.IsMasterClient ? "[HOST] " : "";
                    playerText.text = prefix + player.NickName;
                }
            }
        }

        #endregion
    }
}