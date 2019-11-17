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
        private ILogger<SnakeHub> logger;

        private IHubContext<SnakeHub> snakeContext = null;

        private ISnakeService service;

        public SnakeHub(IHubContext<SnakeHub> context, ILogger<SnakeHub> ilogger, ISnakeService service)
        {
            this.snakeContext = context;
            this.logger = ilogger;
            this.service = service;
            this.service.Game.OnGameStart += this.OnGameStart;
            this.service.Game.OnGameOver += this.OnGameOver;
            this.service.Game.OnMessageReceived += this.OnSendMessage;
            this.service.Game.OnContainerCreated += this.SendContainer;
        }

        public async void SendContainer(object sender, GameOBjectListEventArgs e)
        {
            List<ObjectPrintContainer> newContainer = new List<ObjectPrintContainer>();
            List<ObjectPrintContainer> oldContainer = new List<ObjectPrintContainer>();

            foreach (var gameObjectse in e.NewObjects)
            {
                newContainer.Add(new ObjectPrintContainer(gameObjectse.Icon.Character, new NetworkLibrary.Position(gameObjectse.Pos.X, gameObjectse.Pos.Y), gameObjectse.Color.ForeGroundColor));
            }

            foreach (var gameObjectse in e.OldObjects)
            {
                oldContainer.Add(new ObjectPrintContainer(gameObjectse.Icon.Character, new NetworkLibrary.Position(gameObjectse.Pos.X, gameObjectse.Pos.Y), gameObjectse.Color.ForeGroundColor));
            }

            await this.snakeContext.Clients.All.SendAsync("GameContainer", new ObjectListContainer(oldContainer, newContainer, new GameInformationContainer(e.SnakeLength, e.Score)));
        }

        public async void OnGameOver(object sender, EventArgs e)
        {
            this.service.Game = new Application();
            this.service.Game.OnGameStart += this.OnGameOver;
            this.service.Game.OnGameOver += this.OnGameStart;
            this.service.Game.OnMessageReceived += this.OnSendMessage;
            this.service.Game.OnContainerCreated += this.SendContainer;
            await this.snakeContext.Clients.All.SendAsync("Message", "Game over press any key to start new.");
        }

        public async void OnSendMessage(object sender, StringEventArgs stringEvent)
        {
            await this.snakeContext.Clients.All.SendAsync("Message", stringEvent.Text);
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
            this.service.ConnectedIds.Add(Context.ConnectionId);
            this.logger.LogInformation("Client with id " + Context.ConnectionId + " connected.");
            if (this.service.IsGameOver)
            {
                await base.Clients.Caller.SendAsync("Message", "Press any key to start the game.");
                
                return;
            }

            if (this.service.IsPaused)
            {
                this.logger.LogInformation("Game Resumed");
                this.service.IsPaused = false;
                var field = this.service.Game.GetField();
                await base.Clients.Caller.SendAsync("FieldMessage", new FieldPrintContainer(field.Width, field.Length, field.Icon.Character));
                this.service.Game.Resume(this, EventArgs.Empty);
                return;
            }
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            this.service.ConnectedIds.Remove(Context.ConnectionId);
            this.logger.LogInformation("Client with id " + Context.ConnectionId + " disconnected.");
            if (this.service.ConnectedIds.Count == 0 && !this.service.IsGameOver)
            {
                this.service.Game.Stop(this, EventArgs.Empty);
                this.service.IsPaused = true;
            }
            return base.OnDisconnectedAsync(exception);
        }

        public async Task OnInput(object e)
        {
            if (this.service.IsGameOver)
            {
                this.RestartGame();
                this.service.Game.Start(this, EventArgs.Empty);
                return;
            }

            MoveSnakeContainer z = (MoveSnakeContainer)e;
            this.logger.LogInformation(z.SnakeMoveCommand + " pressed by " + Context.ConnectionId);

            if (z.SnakeMoveCommand.Id == 999)
            {
                return;
            }
            else
            {
                DirectionEventArgs directionEvent = null;

                switch (z.SnakeMoveCommand.Id)
                {
                    case 0:
                        directionEvent = new DirectionEventArgs(new DirectionUp());
                        break;
                    case 1:
                        directionEvent = new DirectionEventArgs(new DirectionDown());
                        break;
                    case 2:
                        directionEvent = new DirectionEventArgs(new DirectionRight());
                        break;
                    case 3:
                        directionEvent = new DirectionEventArgs(new DirectionLeft());
                        break;
                    default:
                        directionEvent = new DirectionEventArgs(new DirectionDown());
                        break;
                }

                await Task.Factory.StartNew(() => this.service.Game.GetInput(this, directionEvent));
            }
        }

        public async  void OnGameStart(object sender, EventArgs e)
        {
            logger.LogInformation("Game start.");
            var field = this.service.Game.GetField();
            await this.snakeContext.Clients.All.SendAsync("FieldMessage", new FieldPrintContainer(field.Width, field.Length, field.Icon.Character));
        }

        private void RestartGame()
        {
            this.logger.LogInformation("Game restarted");
            this.service.Game = new Application();
            this.service.Game.OnGameStart += this.OnGameOver;
            this.service.Game.OnGameOver += this.OnGameStart;
            this.service.Game.OnMessageReceived += this.OnSendMessage;
            this.service.Game.OnContainerCreated += this.SendContainer;
            this.service.IsGameOver = false;
        }
    }
}
