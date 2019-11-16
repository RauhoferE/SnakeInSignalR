using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeServer
{
    using Microsoft.Extensions.Logging;
    using NetworkLibrary;
    using Snake_V_0_3;

    public class SnakeHub : Hub
    {
        private Application game;

        private ILogger<SnakeHub> logger;

        private bool isGameOver;

        private bool isGamePaused;

        public static HashSet<string> ConnectedIds = new HashSet<string>();

        public SnakeHub(ILogger<SnakeHub> ilogger)
        {
            this.game = new Application();
            this.logger = ilogger;
            this.isGameOver = true;
            this.isGamePaused = false;
            this.game.OnGameStart += this.OnGameOver;
            this.game.OnGameOver += this.OnGameStart;
            this.game.OnMessageReceived += this.OnSendMessage;
            this.game.OnContainerCreated += this.SendContainer;
        }

        public async void SendContainer(object sender, GameOBjectListEventArgs e)
        {
            await base.Clients.All.SendAsync("GameContainer", e);
        }

        public async void OnGameOver(object sender, EventArgs e)
        {
            this.game = new Application();
            this.game.OnGameStart += this.OnGameOver;
            this.game.OnGameOver += this.OnGameStart;
            this.game.OnMessageReceived += this.OnSendMessage;
            this.game.OnContainerCreated += this.SendContainer;
            await this.Clients.All.SendAsync("Message", "Game over press any key to start new.");
        }

        public async void OnSendMessage(object sender, StringEventArgs stringEvent)
        {
            await base.Clients.All.SendAsync("Message", stringEvent.Text);
        }

        public override async Task OnConnectedAsync()
        {

            ConnectedIds.Add(Context.ConnectionId);

            if (this.isGameOver)
            {
                await base.Clients.Caller.SendAsync("Message", "Press any key to start the game.");
                return;
            }

            if (this.isGamePaused)
            {
                this.logger.LogInformation("Game Resumed");
                this.game.Resume(this, EventArgs.Empty);
                return;
            }
            var field = this.game.GetField();
            await base.Clients.Caller.SendAsync("FieldMessage", new FieldPrintContainer(field.Width, field.Length, field.Icon.Character));
            this.logger.LogInformation("Client with id " + Context.ConnectionId);

            await base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            ConnectedIds.Remove(Context.ConnectionId);
            this.logger.LogInformation("Client with id " + Context.ConnectionId + " disconnected.");
            if (ConnectedIds.Count == 0)
            {
                this.game.Stop(this, EventArgs.Empty);
            }
            return base.OnDisconnectedAsync(exception);
        }

        public async Task OnInput(DirectionEventArgs e)
        {
            if (this.isGameOver)
            {
                this.RestartGame();
                this.game.Start(this, EventArgs.Empty);
                return;
            }

            this.logger.LogInformation(e.Direction + " pressed by " + Context.ConnectionId);
            await Task.Factory.StartNew(() => this.game.GetInput(this, e));
        }

        public async  void OnGameStart(object sender, EventArgs e)
        {
            logger.LogInformation("Game start.");
            var field = this.game.GetField();
            await base.Clients.All.SendAsync("FieldMessage", new FieldPrintContainer(field.Width, field.Length, field.Icon.Character));
        }

        private void RestartGame()
        {
            this.logger.LogInformation("Game restarted");
            this.game = new Application();
            this.game.OnGameStart += this.OnGameOver;
            this.game.OnGameOver += this.OnGameStart;
            this.game.OnMessageReceived += this.OnSendMessage;
            this.game.OnContainerCreated += this.SendContainer;
        }
    }
}
