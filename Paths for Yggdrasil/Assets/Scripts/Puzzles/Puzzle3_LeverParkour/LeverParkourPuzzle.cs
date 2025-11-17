using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

namespace Yggdrasil.Puzzles.LeverParkour
{
    [System.Serializable]
    public class LeverPlatformPair
    {
        public LeverController lever;
        public PlatformController platform;
        public int sequenceIndex;
    }

    public class LeverParkourPuzzle : BasePuzzle
    {
        [Header("Lever Parkour Settings")]
        [SerializeField] private List<LeverPlatformPair> leverPlatformPairs;
        [SerializeField] private Transform playerBStartPoint;
        [SerializeField] private Transform playerBEndPoint;
        [SerializeField] private bool isPlayerARoom = false;

        private int currentSequenceIndex = 0;

        public override void Initialize()
        {
            base.Initialize();
            SetupLeversAndPlatforms();
        }

        private void SetupLeversAndPlatforms()
        {
            foreach (var pair in leverPlatformPairs)
            {
                pair.lever.OnLeverActivated += OnLeverActivated;
                pair.platform.LowerPlatform(); // Todas começam abaixadas
            }
        }

        public override void StartPuzzle()
        {
            base.StartPuzzle();
            currentSequenceIndex = 0;

            if (!isPlayerARoom)
            {
                // Teleporta Player B para início do parkour
                // playerBController.transform.position = playerBStartPoint.position;
            }
        }

        private void OnLeverActivated(LeverController lever)
        {
            if (state != PuzzleState.Active) return;
            if (!isPlayerARoom) return; // Apenas Player A pode ativar alavancas

            LeverPlatformPair pair = leverPlatformPairs.Find(p => p.lever == lever);

            if (pair != null && pair.sequenceIndex == currentSequenceIndex)
            {
                // Sequência correta
                RaisePlatform(pair.sequenceIndex);
                currentSequenceIndex++;

                if (currentSequenceIndex >= leverPlatformPairs.Count)
                {
                    // Todas as plataformas ativadas
                    CheckPuzzleCompletion();
                }
            }
            else
            {
                // Sequência errada
                Debug.Log("Alavanca errada ativada!");
                ResetSequence();
            }
        }

        private void RaisePlatform(int index)
        {
            photonView.RPC("RPC_RaisePlatform", RpcTarget.All, index);
        }

        [PunRPC]
        private void RPC_RaisePlatform(int index)
        {
            LeverPlatformPair pair = leverPlatformPairs.Find(p => p.sequenceIndex == index);
            if (pair != null)
            {
                pair.platform.RaisePlatform();
            }
        }

        private void ResetSequence()
        {
            currentSequenceIndex = 0;
            photonView.RPC("RPC_ResetAllPlatforms", RpcTarget.All);
        }

        [PunRPC]
        private void RPC_ResetAllPlatforms()
        {
            foreach (var pair in leverPlatformPairs)
            {
                pair.platform.LowerPlatform();
                pair.lever.ResetLever();
            }
        }

        private void CheckPuzzleCompletion()
        {
            // Verifica se Player B chegou ao final
            // Isso pode ser feito via trigger no ponto final
        }

        public void OnPlayerReachedEnd()
        {
            if (currentSequenceIndex >= leverPlatformPairs.Count)
            {
                CompletePuzzle();
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            foreach (var pair in leverPlatformPairs)
            {
                pair.lever.OnLeverActivated -= OnLeverActivated;
            }
        }
    }
}