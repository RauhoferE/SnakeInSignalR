using System.Collections.Generic;
using Snake_V_0_3;

namespace SnakeServer
{
    public interface ISnakeService
    {
        HashSet<string> ConnectedIds { get; set; }
        Application Game { get; set; }
        bool IsPaused { get; set; }
        bool IsGameOver { get; set; }

        void AddId(string s);
        void ChangeIsPaused();
        void ChangeIsRunning();
        void StartNewGame();
    }
}