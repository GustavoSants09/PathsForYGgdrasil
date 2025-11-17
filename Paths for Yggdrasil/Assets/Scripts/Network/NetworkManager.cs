using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using System.Collections;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public static NetworkManager Instance { get; private set; }

    [Header("Connection Settings")]
    [SerializeField] private string gameVersion = "1.0";
    [SerializeField] private byte maxPlayersPerRoom = 2;

    [Header("Room Settings")]
    [SerializeField] private string roomNamePrefix = "YggdrasilRoom_";

    public bool IsConnected => PhotonNetwork.IsConnected;
    public bool IsInRoom => PhotonNetwork.InRoom;
    public int PlayerCount => PhotonNetwork.CurrentRoom?.PlayerCount ?? 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        ConnectToPhoton();
    }

    public void ConnectToPhoton()
    {
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.GameVersion = gameVersion;
            PhotonNetwork.ConnectUsingSettings();
            Debug.Log("[Network] Connecting to Photon...");
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("[Network] Connected to Master Server");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("[Network] Joined Lobby. Attempting to join or create room...");
        JoinOrCreateRoom();
    }

    private void JoinOrCreateRoom()
    {
        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = maxPlayersPerRoom,
            IsVisible = true,
            IsOpen = true
        };

        PhotonNetwork.JoinOrCreateRoom(
            roomNamePrefix + Random.Range(1000, 9999),
            roomOptions,
            TypedLobby.Default
        );
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"[Network] Joined Room: {PhotonNetwork.CurrentRoom.Name}");
        Debug.Log($"[Network] Players in room: {PhotonNetwork.CurrentRoom.PlayerCount}/{maxPlayersPerRoom}");

        // Spawna o jogador local
        SpawnPlayer();
    }

    private void SpawnPlayer()
    {
        Vector3 spawnPosition = PhotonNetwork.IsMasterClient
            ? new Vector3(0, 1, 0)  // Player A spawn
            : new Vector3(0, 1, 10); // Player B spawn

        GameObject player = PhotonNetwork.Instantiate(
            "PhotonPlayer",
            spawnPosition,
            Quaternion.identity
        );

        Debug.Log($"[Network] Player spawned at {spawnPosition}");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"[Network] Player {newPlayer.NickName} entered the room");

        if (PhotonNetwork.CurrentRoom.PlayerCount == maxPlayersPerRoom)
        {
            Debug.Log("[Network] Room full! Starting game...");
            StartCoroutine(StartGameCountdown());
        }
    }

    private IEnumerator StartGameCountdown()
    {
        yield return new WaitForSeconds(2f);

        if (PhotonNetwork.IsMasterClient)
        {
            // Master Client inicia o jogo para todos
            photonView.RPC("RPC_StartGame", RpcTarget.All);
        }
    }

    [PunRPC]
    private void RPC_StartGame()
    {
        Debug.Log("[Network] Game starting!");
        GameEvents.OnGameStart?.Invoke();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"[Network] Player {otherPlayer.NickName} left the room");

        // Se um jogador sair, pausar/encerrar o jogo
        GameEvents.OnPlayerDisconnected?.Invoke();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"[Network] Disconnected from Photon: {cause}");
    }
}