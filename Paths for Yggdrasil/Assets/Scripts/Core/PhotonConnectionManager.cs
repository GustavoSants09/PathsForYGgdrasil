using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// Gerencia a conexão inicial com o Photon e callbacks de rede.
/// Implementa MonoBehaviourPunCallbacks para receber eventos automaticamente.
/// </summary>
public class PhotonConnectionManager : MonoBehaviourPunCallbacks
{
    [Header("Configurações de Conexão")]
    [SerializeField] private string gameVersion = "1.0";
    [SerializeField] private byte maxPlayersPerRoom = 2;

    private void Start()
    {
        ConnectToPhoton();
    }

    /// <summary>
    /// Conecta ao Photon usando as configurações do PhotonServerSettings.
    /// </summary>
    private void ConnectToPhoton()
    {
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.GameVersion = gameVersion;
            PhotonNetwork.ConnectUsingSettings();
            Debug.Log("Conectando ao Photon Cloud...");
        }
    }

    #region Photon Callbacks

    /// <summary>
    /// Chamado quando a conexão com o Master Server é estabelecida.
    /// </summary>
    public override void OnConnectedToMaster()
    {
        Debug.Log($"✅ Conectado ao Master Server! Região: {PhotonNetwork.CloudRegion}");
        Debug.Log($"Ping: {PhotonNetwork.GetPing()}ms");
    }

    /// <summary>
    /// Chamado quando a conexão com o Photon falha.
    /// </summary>
    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogError($"❌ Desconectado do Photon. Causa: {cause}");
    }

    #endregion
}