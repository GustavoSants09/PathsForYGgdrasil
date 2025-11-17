using UnityEngine;
using Photon.Pun;
using Yggdrasil.Core;

namespace Yggdrasil.Puzzles
{
    public abstract class BasePuzzle : MonoBehaviourPunCallbacks, IPuzzle
    {
        [Header("Puzzle Settings")]
        [SerializeField] protected int puzzleIndex;
        [SerializeField] protected string puzzleName;

        protected PuzzleState state = PuzzleState.NotStarted;

        public PuzzleState State => state;
        public int PuzzleIndex => puzzleIndex;

        protected virtual void Awake()
        {
            Initialize();
        }

        public virtual void Initialize()
        {
            state = PuzzleState.NotStarted;
            EventSystem.Instance.Subscribe(GameEvents.PUZZLE_STARTED, OnPuzzleStartedEvent);
        }

        protected virtual void OnDestroy()
        {
            EventSystem.Instance.Unsubscribe(GameEvents.PUZZLE_STARTED, OnPuzzleStartedEvent);
        }

        private void OnPuzzleStartedEvent(object data)
        {
            int startedPuzzleIndex = (int)data;
            if (startedPuzzleIndex == puzzleIndex)
            {
                StartPuzzle();
            }
        }

        public virtual void StartPuzzle()
        {
            state = PuzzleState.Active;
            Debug.Log($"Puzzle {puzzleIndex} ({puzzleName}) iniciado!");
        }

        public virtual void CompletePuzzle()
        {
            if (state != PuzzleState.Active) return;

            state = PuzzleState.Completed;
            Debug.Log($"Puzzle {puzzleIndex} ({puzzleName}) completado!");

            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC("RPC_PuzzleCompleted", RpcTarget.All);
            }
        }

        [PunRPC]
        protected virtual void RPC_PuzzleCompleted()
        {
            state = PuzzleState.Completed;
            EventSystem.Instance.TriggerEvent(GameEvents.PUZZLE_COMPLETED, puzzleIndex);
        }

        public virtual void FailPuzzle()
        {
            if (state != PuzzleState.Active) return;

            state = PuzzleState.Failed;
            Debug.Log($"Puzzle {puzzleIndex} ({puzzleName}) falhou!");
        }

        public virtual void ResetPuzzle()
        {
            state = PuzzleState.NotStarted;
        }
    }
}