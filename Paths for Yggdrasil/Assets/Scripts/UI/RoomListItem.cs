using UnityEngine;
using UnityEngine.UI;
using Photon.Realtime;
using TMPro;

namespace QuantumHeist.Network
{
    /// <summary>
    /// Componente que representa um item individual na lista de salas
    /// Exibe informações da sala e permite entrar ao clicar
    /// </summary>
    public class RoomListItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text roomNameText;
        [SerializeField] private TMP_Text playerCountText;
        [SerializeField] private Button joinButton;

        private RoomInfo roomInfo;
        private NetworkManager networkManager;

        /// <summary>
        /// Configura o item com informações da sala
        /// </summary>
        public void SetupRoom(RoomInfo room, NetworkManager manager)
        {
            roomInfo = room;
            networkManager = manager;

            // Atualiza textos
            roomNameText.text = room.Name;
            playerCountText.text = $"{room.PlayerCount}/{room.MaxPlayers}";

            // Configura botão
            joinButton.onClick.RemoveAllListeners();
            joinButton.onClick.AddListener(OnJoinButtonClicked);
        }

        /// <summary>
        /// Callback quando botão de entrar é clicado
        /// </summary>
        private void OnJoinButtonClicked()
        {
            if (networkManager != null && roomInfo != null)
            {
                networkManager.JoinRoom(roomInfo.Name);
            }
        }
    }
}