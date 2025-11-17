using UnityEngine;
using System.Collections.Generic;

namespace Yggdrasil.Puzzles.InvisibleMaze
{
    [System.Serializable]
    public class MazeCell
    {
        public int x;
        public int y;
        public bool isPath;
        public bool isStart;
        public bool isEnd;
    }

    [CreateAssetMenu(fileName = "MazeData", menuPath = "Yggdrasil/Puzzles/Maze Data")]
    public class MazeData : ScriptableObject
    {
        public int width = 10;
        public int height = 10;
        public List<MazeCell> correctPath;

        public bool IsValidMove(int fromX, int fromY, int toX, int toY)
        {
            MazeCell current = correctPath.Find(c => c.x == fromX && c.y == fromY);
            MazeCell next = correctPath.Find(c => c.x == toX && c.y == toY);

            if (current == null || next == null) return false;

            int currentIndex = correctPath.IndexOf(current);
            int nextIndex = correctPath.IndexOf(next);

            return nextIndex == currentIndex + 1;
        }
    }
}