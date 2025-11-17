using UnityEngine;
using Photon.Pun;
using System;

namespace Yggdrasil.Puzzles.LeverParkour
{
    public class LeverController : MonoBehaviourPunCallbacks
    {
        [Header("Lever Settings")]
        [SerializeField] private bool isActivated = false;

        public event Action<LeverController> OnLeverActivated;

        public void ActivateLever()
        {
            if (isActivated) return;

            isActivated = true;
            photonView.RPC("RPC_ActivateLever", RpcTarget.All);
        }

        [PunRPC]
        private void RPC_ActivateLever()
        {
            isActivated = true;
            // Animação da alavanca
            OnLeverActivated?.Invoke(this);
        }

        public void ResetLever()
        {
            isActivated = false;
            // Reset da animação
        }
    }
}