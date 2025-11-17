using UnityEngine;
using Photon.Pun;
using Yggdrasil.Core;

namespace Yggdrasil.Puzzles.InvisibleMaze
{
    public class InvisibleMazePuzzle : BasePuzzle
    {
        [Header("Maze Settings")]
        [SerializeField] private MazeData mazeData;
        [SerializeField] private bool isPlayerARoom = false; // Player A vê mapa, Player B navega

        [Header("Player B Navigation")]
        [SerializeField] private Transform playerBSpawnPoint;
        [SerializeField] private LayerMask abyssLayer;

        private Vector2Int currentPlayerPosition;
        private Vector2Int targetPosition;

        public override void Initialize()
        {
            base.Initialize();

            if (isPlayerARoom)
            {
                // Player A: Renderiza mapa em Canvas UI
                DisplayMazeMap();
            }
            else
            {
                // Player B: Setup da navegação no abismo
                SetupPlayerBNavigation();
            }
        }

        private void DisplayMazeMap()
        {
            // Renderizar grid do labirinto em Canvas UI
            // Destacar caminho correto
            Debug.Log("Mapa do labirinto renderizado para Player A");
        }

        private void SetupPlayerBNavigation()
        {
            if (mazeData.correctPath.Count > 0)
            {
                MazeCell startCell = mazeData.correctPath.Find(c => c.isStart);
                currentPlayerPosition = new Vector2Int(startCell.x, startCell.y);
            }
        }

        public void MovePlayer(Vector2Int direction)
        {
            if (state != PuzzleState.Active) return;
            if (isPlayerARoom) return; // Apenas Player B pode se mover

            Vector2Int newPosition = currentPlayerPosition + direction;

            if (mazeData.IsValidMove(currentPlayerPosition.x, currentPlayerPosition.y,
                                     newPosition.x, newPosition.y))
            {
                currentPlayerPosition = newPosition;
                photonView.RPC("RPC_UpdatePlayerPosition", RpcTarget.All, currentPlayerPosition.x, currentPlayerPosition.y);

                // Verifica se chegou ao fim
                MazeCell endCell = mazeData.correctPath.Find(c => c.isEnd);
                if (currentPlayerPosition.x == endCell.x && currentPlayerPosition.y == endCell.y)
                {
                    CompletePuzzle();
                }
            }
            else
            {
                // Movimento inválido - Player B cai no abismo
                FailPuzzle();
            }
        }

        [PunRPC]
        private void RPC_UpdatePlayerPosition(int x, int y)
        {
            currentPlayerPosition = new Vector2Int(x, y);
        }

        public override void FailPuzzle()
        {
            base.FailPuzzle();
            // Respawn do Player B na posição inicial
            ResetPlayerPosition();
        }

        private void ResetPlayerPosition()
        {
            MazeCell startCell = mazeData.correctPath.Find(c => c.isStart);
            currentPlayerPosition = new Vector2Int(startCell.x, startCell.y);
        }
    }
}