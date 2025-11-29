using CargoClash.Core;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CargoClash.UI
{
    /// <summary>
    /// Gerencia a interface do lobby: conexão, criação e entrada em salas.
    /// Exibe status de conexão em tempo real.
    /// </summary>
    public class LobbyUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject roomPanel;

        [Header("Main Panel Elements")]
        [SerializeField] private TMP_InputField playerNameInput;
        [SerializeField] private Button quickPlayButton;
        [SerializeField] private Button createRoomButton;
        [SerializeField] private TMP_InputField roomNameInput;
        [SerializeField] private Button joinRoomButton;

        [Header("Room Panel Elements")]
        [SerializeField] private TextMeshProUGUI roomInfoText;
        [SerializeField] private TextMeshProUGUI playersListText;
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button leaveRoomButton;

        [Header("Status Display")]
        [SerializeField] private TextMeshProUGUI connectionStatusText;
        [SerializeField] private Image statusIndicator;
        [SerializeField] private Color connectingColor = Color.yellow;
        [SerializeField] private Color connectedColor = Color.green;
        [SerializeField] private Color errorColor = Color.red;

        private const string PLAYER_NAME_KEY = "PlayerName";

        private void Start()
        {
            // Configuração inicial
            ShowMainPanel();
            LoadPlayerName();
            SetupButtonListeners();
            SubscribeToNetworkEvents();

            // Conecta automaticamente ao iniciar
            NetworkManager.Instance.Connect();
        }

        private void OnDestroy()
        {
            UnsubscribeFromNetworkEvents();
        }

        private void SetupButtonListeners()
        {
            quickPlayButton.onClick.AddListener(OnQuickPlayClicked);
            createRoomButton.onClick.AddListener(OnCreateRoomClicked);
            joinRoomButton.onClick.AddListener(OnJoinRoomClicked);
            startGameButton.onClick.AddListener(OnStartGameClicked);
            leaveRoomButton.onClick.AddListener(OnLeaveRoomClicked);

            playerNameInput.onEndEdit.AddListener(OnPlayerNameChanged);
        }

        private void SubscribeToNetworkEvents()
        {
            NetworkManager.Instance.OnConnectionStatusChanged += UpdateConnectionStatus;
            NetworkManager.Instance.OnJoinedRoomSuccessfully += OnJoinedRoom;
            NetworkManager.Instance.OnConnectionFailed += OnConnectionError;
        }

        private void UnsubscribeFromNetworkEvents()
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnConnectionStatusChanged -= UpdateConnectionStatus;
                NetworkManager.Instance.OnJoinedRoomSuccessfully -= OnJoinedRoom;
                NetworkManager.Instance.OnConnectionFailed -= OnConnectionError;
            }
        }

        // ===== UI UPDATES =====

        private void UpdateConnectionStatus(string status)
        {
            connectionStatusText.text = $"Status: {status}";

            // Atualiza indicador visual
            if (status.Contains("Conectado") || status.Contains("Pronto"))
            {
                statusIndicator.color = connectedColor;
            }
            else if (status.Contains("Erro") || status.Contains("Falha"))
            {
                statusIndicator.color = errorColor;
            }
            else
            {
                statusIndicator.color = connectingColor;
            }
        }

        private void OnJoinedRoom()
        {
            ShowRoomPanel();
            UpdateRoomInfo();
        }

        private void OnConnectionError(string error)
        {
            Debug.LogError($"[LobbyUI] Erro de conexão: {error}");
            // Aqui você pode adicionar um popup de erro
        }

        private void UpdateRoomInfo()
        {
            if (!PhotonNetwork.InRoom) return;

            Room room = PhotonNetwork.CurrentRoom;
            roomInfoText.text = $"Sala: {room.Name}\nJogadores: {room.PlayerCount}/{room.MaxPlayers}";

            // Lista de jogadores
            playersListText.text = "Jogadores:\n";
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                string role = player.IsMasterClient ? " (Host)" : "";
                playersListText.text += $"• {player.NickName}{role}\n";
            }

            // Apenas o host pode iniciar o jogo
            startGameButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
            startGameButton.interactable = room.PlayerCount >= 2; // Mínimo 2 jogadores
        }

        // ===== BUTTON HANDLERS =====

        private void OnQuickPlayClicked()
        {
            SavePlayerName();
            NetworkManager.Instance.JoinRandomRoom();
        }

        private void OnCreateRoomClicked()
        {
            SavePlayerName();
            string roomName = string.IsNullOrEmpty(roomNameInput.text)
                ? null
                : roomNameInput.text;
            NetworkManager.Instance.CreateRoom(roomName);
        }

        private void OnJoinRoomClicked()
        {
            SavePlayerName();
            if (string.IsNullOrEmpty(roomNameInput.text))
            {
                Debug.LogWarning("[LobbyUI] Nome da sala não pode ser vazio");
                return;
            }
            NetworkManager.Instance.JoinRoom(roomNameInput.text);
        }

        private void OnStartGameClicked()
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.LogWarning("[LobbyUI] Apenas o host pode iniciar o jogo");
                return;
            }

            if (PhotonNetwork.CurrentRoom.PlayerCount < 2)
            {
                Debug.LogWarning("[LobbyUI] Necessário pelo menos 2 jogadores");
                return;
            }

            // Carrega a cena do jogo (sincroniza automaticamente com AutomaticallySyncScene = true)
            PhotonNetwork.LoadLevel("GameArena");
        }

        private void OnLeaveRoomClicked()
        {
            PhotonNetwork.LeaveRoom();
            ShowMainPanel();
        }

        // ===== PLAYER NAME =====

        private void LoadPlayerName()
        {
            string savedName = PlayerPrefs.GetString(PLAYER_NAME_KEY, $"Player_{Random.Range(100, 999)}");
            playerNameInput.text = savedName;
            PhotonNetwork.NickName = savedName;
        }

        private void SavePlayerName()
        {
            string name = string.IsNullOrEmpty(playerNameInput.text)
                ? $"Player_{Random.Range(100, 999)}"
                : playerNameInput.text;

            PlayerPrefs.SetString(PLAYER_NAME_KEY, name);
            PhotonNetwork.NickName = name;
        }

        private void OnPlayerNameChanged(string newName)
        {
            SavePlayerName();
        }

        // ===== PANEL MANAGEMENT =====

        private void ShowMainPanel()
        {
            mainPanel.SetActive(true);
            roomPanel.SetActive(false);
        }

        private void ShowRoomPanel()
        {
            mainPanel.SetActive(false);
            roomPanel.SetActive(true);
        }

        // ===== PHOTON CALLBACKS (para atualização em tempo real) =====

        // Nota: Esta classe pode herdar de MonoBehaviourPunCallbacks se preferir
        // Por simplicidade, estamos usando eventos do NetworkManager
        private void Update()
        {
            // Atualiza info da sala em tempo real quando estiver nela
            if (PhotonNetwork.InRoom && roomPanel.activeSelf)
            {
                UpdateRoomInfo();
            }
        }
    }
}