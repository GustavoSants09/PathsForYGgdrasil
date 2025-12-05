using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Collections.Generic;
using System.Collections;

namespace QuantumHeist.Network
{
    /// <summary>
    /// Gerencia toda a comunicação com Photon PUN2 para o sistema de lobby
    /// Responsável por conexão, criação/listagem de salas e callbacks de rede
    /// </summary>
    public class NetworkManager : MonoBehaviourPunCallbacks
    {
        [Header("Configurações de Conexão")]
        [SerializeField] private string gameVersion = "1.0";
        [SerializeField] private byte maxPlayersPerRoom = 2; // Quantum Heist é 1v1

        [Header("UI - Painéis")]
        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private GameObject roomPanel;

        [Header("UI - Lobby")]
        [SerializeField] private TMP_InputField nicknameInput;
        [SerializeField] private TMP_InputField roomNameInput;
        [SerializeField] private TMP_Text connectionStatusText;
        [SerializeField] private Transform roomListContent;
        [SerializeField] private GameObject roomListItemPrefab;

        [Header("UI - Room")]
        [SerializeField] private TMP_Text roomTitleText;
        [SerializeField] private TMP_Text playersInRoomText;
        [SerializeField] private GameObject startGameButton;
        [SerializeField] private Transform playerListContent;
        [SerializeField] private GameObject playerListItemPrefab;

        // Cache de salas disponíveis
        private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();

        // ✅ NOVO: Controle de saída de jogador
        private bool isKickingPlayer = false;

        #region Unity Callbacks

        private void Start()
        {
            // Carrega nickname salvo ou gera um aleatório
            if (PlayerPrefs.HasKey("PlayerNickname"))
            {
                nicknameInput.text = PlayerPrefs.GetString("PlayerNickname");
            }
            else
            {
                nicknameInput.text = "Player" + Random.Range(1000, 9999);
            }

            // Conecta automaticamente ao Photon
            ConnectToPhoton();
        }

        #endregion

        #region Conexão Photon

        /// <summary>
        /// Inicia conexão com servidores Photon
        /// </summary>
        private void ConnectToPhoton()
        {
            UpdateConnectionStatus("Conectando...");

            PhotonNetwork.AutomaticallySyncScene = true; // Sincroniza cenas automaticamente
            PhotonNetwork.GameVersion = gameVersion;
            PhotonNetwork.NickName = nicknameInput.text;

            PhotonNetwork.ConnectUsingSettings();
        }

        #endregion

        #region Métodos Públicos - UI Buttons

        /// <summary>
        /// Cria uma sala com nome customizado
        /// </summary>
        public void CreateRoom()
        {
            if (string.IsNullOrEmpty(roomNameInput.text))
            {
                UpdateConnectionStatus("ERRO: Digite um nome para a sala!");
                return;
            }

            // Salva nickname
            PlayerPrefs.SetString("PlayerNickname", nicknameInput.text);
            PhotonNetwork.NickName = nicknameInput.text;

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
        /// Entra em uma sala específica digitada pelo jogador
        /// </summary>
        public void JoinSpecificRoom()
        {
            if (string.IsNullOrEmpty(roomNameInput.text))
            {
                UpdateConnectionStatus("ERRO: Digite o nome da sala!");
                return;
            }

            // Salva nickname
            PlayerPrefs.SetString("PlayerNickname", nicknameInput.text);
            PhotonNetwork.NickName = nicknameInput.text;

            UpdateConnectionStatus($"Procurando sala '{roomNameInput.text}'...");
            PhotonNetwork.JoinRoom(roomNameInput.text);
        }

        /// <summary>
        /// Entra em uma sala específica pelo nome
        /// </summary>
        public void JoinRoom(string roomName)
        {
            PlayerPrefs.SetString("PlayerNickname", nicknameInput.text);
            PhotonNetwork.NickName = nicknameInput.text;

            UpdateConnectionStatus($"Entrando na sala '{roomName}'...");
            PhotonNetwork.JoinRoom(roomName);
        }

        /// <summary>
        /// Entra em uma sala aleatória disponível
        /// </summary>
        public void JoinRandomRoom()
        {
            PlayerPrefs.SetString("PlayerNickname", nicknameInput.text);
            PhotonNetwork.NickName = nicknameInput.text;

            UpdateConnectionStatus("Procurando sala disponível...");
            PhotonNetwork.JoinRandomRoom();
        }

        /// <summary>
        /// Sai da sala atual e retorna ao lobby
        /// </summary>
        public void LeaveRoom()
        {
            UpdateConnectionStatus("Saindo da sala...");

            // ✅ NOVO: Se estiver em jogo, notifica outros jogadores antes de sair
            if (PhotonNetwork.InRoom)
            {
                StartCoroutine(LeaveRoomSequence());
            }
        }

        /// <summary>
        /// ✅ NOVO: Sequência de saída da sala com notificação
        /// </summary>
        private IEnumerator LeaveRoomSequence()
        {
            // Notifica outros jogadores via RPC
            if (PhotonNetwork.CurrentRoom != null)
            {
                PhotonView photonView = GetComponent<PhotonView>();
                if (photonView != null)
                {
                    photonView.RPC("RPC_PlayerLeavingRoom", RpcTarget.OthersBuffered, PhotonNetwork.LocalPlayer.NickName);
                }
            }

            // Aguarda um frame para RPC ser enviado
            yield return new WaitForSeconds(0.1f);

            // Sai da sala
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

            if (PhotonNetwork.CurrentRoom.PlayerCount < 2)
            {
                UpdateConnectionStatus("Aguardando segundo jogador...");
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
            UpdateConnectionStatus("Conectado! Entrando no lobby...");
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
            Debug.Log($"Entrou na sala '{PhotonNetwork.CurrentRoom.Name}'. Jogadores: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");

            // Troca para painel de sala
            lobbyPanel.SetActive(false);
            roomPanel.SetActive(true);

            // Atualiza informações da sala
            UpdateRoomUI();
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            UpdateConnectionStatus($"ERRO ao entrar na sala: {message}");
            Debug.LogError($"Falha ao entrar na sala. Código: {returnCode}, Mensagem: {message}");
        }

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            UpdateConnectionStatus("Nenhuma sala disponível. Crie uma!");
            Debug.LogWarning($"Nenhuma sala aleatória disponível. Mensagem: {message}");
        }

        public override void OnLeftRoom()
        {
            Debug.Log("Saiu da sala");

            // Volta para o lobby
            lobbyPanel.SetActive(true);
            roomPanel.SetActive(false);
            UpdateConnectionStatus("No lobby - Pronto para jogar!");
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            Debug.Log($"Jogador entrou: {newPlayer.NickName}");
            UpdateRoomUI();
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            Debug.Log($"Jogador saiu: {otherPlayer.NickName}");

            // ✅ NOVO: Se outro jogador saiu e ainda estamos na sala, também saímos
            if (PhotonNetwork.InRoom && !isKickingPlayer)
            {
                isKickingPlayer = true;
                StartCoroutine(KickRemainingPlayer(otherPlayer.NickName));
            }
            else
            {
                UpdateRoomUI();
            }
        }

        public override void OnMasterClientSwitched(Player newMasterClient)
        {
            Debug.Log($"Novo Master Client: {newMasterClient.NickName}");

            // Atualiza visibilidade do botão de start
            startGameButton.SetActive(PhotonNetwork.IsMasterClient);
        }

        #endregion

        #region ✅ NOVO: Sistema de Expulsão Automática

        /// <summary>
        /// ✅ NOVO: RPC para notificar jogador que outro está saindo
        /// </summary>
        [PunRPC]
        private void RPC_PlayerLeavingRoom(string playerName)
        {
            Debug.Log($"[NetworkManager] 🚪 {playerName} está saindo da sala!");
            UpdateConnectionStatus($"{playerName} saiu da sala");
        }

        /// <summary>
        /// ✅ NOVO: Expulsa o jogador restante quando outro sai
        /// </summary>
        private IEnumerator KickRemainingPlayer(string leftPlayerName)
        {
            Debug.Log($"[NetworkManager] ⚠️ {leftPlayerName} saiu! Expulsando jogador restante...");

            // Mostra mensagem na UI
            UpdateConnectionStatus($"{leftPlayerName} saiu. Retornando ao lobby...");

            // Aguarda 2 segundos para o jogador ver a mensagem
            yield return new WaitForSeconds(2f);

            // Sai da sala
            if (PhotonNetwork.InRoom)
            {
                PhotonNetwork.LeaveRoom();
            }

            // Reseta flag
            isKickingPlayer = false;
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

            // Atualiza UI
            UpdateRoomListUI();
        }

        #endregion

        #region UI Updates

        /// <summary>
        /// Atualiza o texto de status de conexão
        /// </summary>
        private void UpdateConnectionStatus(string status)
        {
            if (connectionStatusText != null)
            {
                connectionStatusText.text = $"Status: {status}";
            }
        }

        /// <summary>
        /// Atualiza a lista visual de salas disponíveis
        /// </summary>
        private void UpdateRoomListUI()
        {
            // Limpa lista atual
            foreach (Transform child in roomListContent)
            {
                Destroy(child.gameObject);
            }

            // Cria item para cada sala disponível
            foreach (RoomInfo room in cachedRoomList.Values)
            {
                if (room.PlayerCount < room.MaxPlayers && room.IsOpen)
                {
                    GameObject listItem = Instantiate(roomListItemPrefab, roomListContent);
                    RoomListItem itemScript = listItem.GetComponent<RoomListItem>();
                    itemScript.SetupRoom(room, this);
                }
            }
        }

        /// <summary>
        /// Atualiza informações da sala atual
        /// </summary>
        private void UpdateRoomUI()
        {
            if (!PhotonNetwork.InRoom) return;

            // Atualiza título da sala
            roomTitleText.text = $"Sala: {PhotonNetwork.CurrentRoom.Name}";

            // Atualiza contador de jogadores
            playersInRoomText.text = $"Jogadores: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}";

            // Atualiza visibilidade do botão Start (apenas Master Client vê)
            startGameButton.SetActive(PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount >= 2);

            // Atualiza lista de jogadores
            UpdatePlayerListUI();
        }

        /// <summary>
        /// Atualiza a lista de jogadores na sala
        /// </summary>
        private void UpdatePlayerListUI()
        {
            // Limpa lista atual
            foreach (Transform child in playerListContent)
            {
                Destroy(child.gameObject);
            }

            // Cria item para cada jogador
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                GameObject listItem = Instantiate(playerListItemPrefab, playerListContent);
                TMP_Text playerText = listItem.GetComponent<TMP_Text>();

                string prefix = player.IsMasterClient ? "[HOST] " : "";
                playerText.text = prefix + player.NickName;
            }
        }

        #endregion
    }
}