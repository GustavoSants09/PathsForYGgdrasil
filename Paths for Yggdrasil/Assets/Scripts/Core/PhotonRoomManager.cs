using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

/// <summary>
/// Gerencia a criação, entrada e saída de salas Photon.
/// </summary>
public class PhotonRoomManager : MonoBehaviourPunCallbacks
{
    [Header("Configurações da Sala")]
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private byte maxPlayersPerRoom = 2;

    /// <summary>
    /// Tenta entrar em uma sala aleatória ou cria uma nova se não existir.
    /// </summary>
    public void JoinOrCreateRoom()
    {
        if (PhotonNetwork.IsConnectedAndReady)
        {
            PhotonNetwork.JoinRandomRoom();
            Debug.Log("Procurando sala disponível...");
        }
        else
        {
            Debug.LogWarning("⚠️ Não conectado ao Photon. Conecte primeiro.");
        }
    }

    /// <summary>
    /// Cria uma nova sala com nome único.
    /// </summary>
    public void CreateRoom(string roomName = null)
    {
        if (string.IsNullOrEmpty(roomName))
        {
            // Gera nome único baseado em timestamp
            roomName = $"Room_{System.DateTime.Now.Ticks}";
        }

        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = maxPlayersPerRoom,
            IsVisible = true,
            IsOpen = true
        };

        PhotonNetwork.CreateRoom(roomName, roomOptions);
        Debug.Log($"Criando sala: {roomName}");
    }

    #region Photon Callbacks

    /// <summary>
    /// Chamado quando entra com sucesso em uma sala.
    /// </summary>
    public override void OnJoinedRoom()
    {
        Debug.Log($"✅ Entrou na sala: {PhotonNetwork.CurrentRoom.Name}");
        Debug.Log($"Jogadores na sala: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");

        // Carrega a cena do jogo quando todos os jogadores estiverem prontos
        if (PhotonNetwork.IsMasterClient)
        {
            // Master Client gerencia o carregamento de cena
            CheckIfReadyToStart();
        }
    }

    /// <summary>
    /// Chamado quando falha ao entrar em uma sala aleatória.
    /// Cria uma nova sala automaticamente.
    /// </summary>
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"⚠️ Nenhuma sala disponível. Criando nova sala...");
        CreateRoom();
    }

    /// <summary>
    /// Chamado quando um novo jogador entra na sala.
    /// </summary>
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"▶️ Jogador entrou: {newPlayer.NickName} (#{newPlayer.ActorNumber})");

        if (PhotonNetwork.IsMasterClient)
        {
            CheckIfReadyToStart();
        }
    }

    /// <summary>
    /// Verifica se a sala está pronta para iniciar o jogo.
    /// </summary>
    private void CheckIfReadyToStart()
    {
        if (PhotonNetwork.CurrentRoom.PlayerCount == maxPlayersPerRoom)
        {
            Debug.Log("🎮 Sala completa! Iniciando jogo...");
            // Sincroniza o carregamento de cena para todos os clientes
            PhotonNetwork.LoadLevel(gameSceneName);
        }
    }

    #endregion
}