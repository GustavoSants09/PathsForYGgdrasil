using UnityEngine;
using Photon.Pun;
using Photon.Voice.Unity;
using Photon.Voice.PUN;
using Yggdrasil.Core;

namespace Yggdrasil.Network
{
    public class RadioVoiceController : MonoBehaviourPunCallbacks
    {
        [Header("Voice Components")]
        [SerializeField] private PhotonVoiceNetwork voiceNetwork;
        [SerializeField] private Recorder recorder;

        [Header("Radio Settings")]
        [SerializeField] private KeyCode pushToTalkKey = KeyCode.V;
        [SerializeField] private bool turnBasedSystem = true;

        private bool isTransmitting = false;
        private bool isMyTurn = true; // Apenas um player pode falar por vez
        private int currentSpeakerId = -1;

        private void Start()
        {
            if (recorder == null)
            {
                recorder = GetComponent<Recorder>();
            }

            // Configura recorder para Push-to-Talk
            recorder.TransmitEnabled = false;
            recorder.VoiceDetection = false; // Desativa detecção automática
        }

        private void Update()
        {
            HandleRadioInput();
        }

        private void HandleRadioInput()
        {
            if (Input.GetKeyDown(pushToTalkKey) && CanTransmit())
            {
                StartTransmitting();
            }

            if (Input.GetKeyUp(pushToTalkKey) && isTransmitting)
            {
                StopTransmitting();
            }
        }

        private bool CanTransmit()
        {
            if (!turnBasedSystem) return true;

            // Sistema de turnos: só pode transmitir se ninguém está falando ou se é sua vez
            return currentSpeakerId == -1 || currentSpeakerId == photonView.ViewID;
        }

        private void StartTransmitting()
        {
            isTransmitting = true;
            recorder.TransmitEnabled = true;

            if (turnBasedSystem)
            {
                photonView.RPC("RPC_SetSpeaker", RpcTarget.AllBuffered, photonView.ViewID);
            }

            EventSystem.Instance.TriggerEvent(GameEvents.RADIO_TALK_START, photonView.ViewID);
            Debug.Log("Rádio ativado - Transmitindo");
        }

        private void StopTransmitting()
        {
            isTransmitting = false;
            recorder.TransmitEnabled = false;

            if (turnBasedSystem)
            {
                photonView.RPC("RPC_ClearSpeaker", RpcTarget.AllBuffered);
            }

            EventSystem.Instance.TriggerEvent(GameEvents.RADIO_TALK_END, photonView.ViewID);
            Debug.Log("Rádio desativado");
        }

        [PunRPC]
        private void RPC_SetSpeaker(int speakerId)
        {
            currentSpeakerId = speakerId;
            EventSystem.Instance.TriggerEvent(GameEvents.RADIO_TURN_CHANGED, speakerId);
        }

        [PunRPC]
        private void RPC_ClearSpeaker()
        {
            currentSpeakerId = -1;
            EventSystem.Instance.TriggerEvent(GameEvents.RADIO_TURN_CHANGED, -1);
        }

        public bool IsCurrentlyTransmitting()
        {
            return isTransmitting;
        }

        public bool IsSomeoneElseTalking()
        {
            return turnBasedSystem && currentSpeakerId != -1 && currentSpeakerId != photonView.ViewID;
        }
    }
}