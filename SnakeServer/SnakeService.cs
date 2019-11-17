using Snake_V_0_3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeServer
{
    public class SnakeService : ISnakeService
    {
        private static HashSet<string> connectedIds = new HashSet<string>();

        private static Application game;

        private static bool isRunning;

        private static bool isPaused;

        public HashSet<string> ConnectedIds { get => connectedIds; set => connectedIds = value; }
        public Application Game { get => game; set => game = value; }
        public bool IsGameOver { get => isRunning; set => isRunning = value; }
        public bool IsPaused { get => isPaused; set => isPaused = value; }

        public SnakeService()
        {
            IsGameOver = true;
            IsPaused = false;
            Game = new Application();
        }

        public void AddId(string s)
        {
            ConnectedIds.Add(s);
        }

        public void StartNewGame()
        {
            Game = new Application();
        }

        public void ChangeIsRunning()
        {
            if (IsGameOver)
            {
                IsGameOver = false;
            }
            else
            {
                IsGameOver = true;
            }
        }

        public void ChangeIsPaused()
        {
            if (IsPaused)
            {
                IsPaused = false;
            }
            else
            {
                IsPaused = true;
            }
        }
    }
}
