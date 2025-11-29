using UnityEngine;
using Photon.Pun;

/// <summary>
/// Componente que deve estar no prefab do jogador para permitir respawn
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerRespawn : MonoBehaviourPun
{
    private CharacterController controller;

    /// <summary>
    /// Inicialização
    /// </summary>
    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    /// <summary>
    /// RPC para respawnar o jogador
    /// </summary>
    [PunRPC]
    private void RPC_Respawn(Vector3 position, Quaternion rotation)
    {
        // Desabilita controller temporariamente para poder mudar posição
        controller.enabled = false;

        transform.position = position;
        transform.rotation = rotation;

        // Reabilita controller
        controller.enabled = true;

        Debug.Log($"Jogador {photonView.Owner.ActorNumber} respawnou em {position}");
    }
}