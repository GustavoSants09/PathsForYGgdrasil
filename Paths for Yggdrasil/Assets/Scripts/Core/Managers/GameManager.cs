using UnityEngine;
using Photon.Pun;
using Yggdrasil.Core;

namespace Yggdrasil.Managers
{
    public enum GameState
    {
        MainMenu,
        Lobby,
        Loading,
        Playing,
        Paused,
        GameOver,
        Victory
    }

    public class GameManager : Singleton<GameManager>
    {
        [Header("Game Settings")]
        [SerializeField] private float globalTimeLimit = 300f; // 5 minutos
        [SerializeField] private int totalPuzzles = 3;

        // State
        private GameState currentState;
        private int currentPuzzleIndex = 0;
        private float elapsedTime = 0f;
        private bool isGameActive = false;

        // Properties
        public GameState CurrentState => currentState;
        public int CurrentPuzzleIndex => currentPuzzleIndex;
        public float ElapsedTime => elapsedTime;
        public float RemainingTime => globalTimeLimit - elapsedTime;
        public bool IsGameActive => isGameActive;

        protected override void Awake()
        {
            base.Awake();
            ChangeState(GameState.MainMenu);
        }

        private void Update()
        {
            if (isGameActive && currentState == GameState.Playing)
            {
                elapsedTime += Time.deltaTime;

                // Verifica timeout
                if (RemainingTime <= 0)
                {
                    GameOver("Tempo esgotado!");
                }
            }
        }

        public void StartGame()
        {
            if (!PhotonNetwork.IsMasterClient) return;

            isGameActive = true;
            elapsedTime = 0f;
            currentPuzzleIndex = 0;
            ChangeState(GameState.Playing);

            EventSystem.Instance.TriggerEvent(GameEvents.PUZZLE_STARTED, currentPuzzleIndex);
        }

        public void ChangeState(GameState newState)
        {
            currentState = newState;
            Debug.Log($"Game State Changed: {newState}");

            switch (newState)
            {
                case GameState.Playing:
                    Time.timeScale = 1f;
                    break;
                case GameState.Paused:
                    Time.timeScale = 0f;
                    break;
                case GameState.GameOver:
                case GameState.Victory:
                    isGameActive = false;
                    Time.timeScale = 0f;
                    break;
            }
        }

        public void OnPuzzleCompleted()
        {
            if (!PhotonNetwork.IsMasterClient) return;

            currentPuzzleIndex++;

            if (currentPuzzleIndex >= totalPuzzles)
            {
                Victory();
            }
            else
            {
                EventSystem.Instance.TriggerEvent(GameEvents.PUZZLE_COMPLETED, currentPuzzleIndex - 1);
                EventSystem.Instance.TriggerEvent(GameEvents.PUZZLE_STARTED, currentPuzzleIndex);
            }
        }

        private void Victory()
        {
            ChangeState(GameState.Victory);
            Debug.Log("Todos os puzzles completados! Vitória!");
        }

        public void GameOver(string reason)
        {
            ChangeState(GameState.GameOver);
            Debug.Log($"Game Over: {reason}");
        }

        public void PauseGame()
        {
            if (currentState == GameState.Playing)
            {
                ChangeState(GameState.Paused);
            }
        }

        public void ResumeGame()
        {
            if (currentState == GameState.Paused)
            {
                ChangeState(GameState.Playing);
            }
        }
    }
}