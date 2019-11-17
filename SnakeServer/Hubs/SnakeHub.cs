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
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;

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

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(new ObjectListContainer(oldContainer, newContainer, new GameInformationContainer(e.SnakeLength, e.Score)));
            await this.snakeContext.Clients.All.SendAsync("GameContainer", json);
        }

        public async void OnGameOver(object sender, EventArgs e)
        {
            this.service.Game = new Application();
            this.service.Game.OnGameOver += this.OnGameOver;
            this.service.Game.OnMessageReceived += this.OnSendMessage;
            this.service.Game.OnContainerCreated += this.SendContainer;
            this.service.IsGameOver = true;
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
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(new FieldPrintContainer(field.Width, field.Length, field.Icon.Character));
                await base.Clients.Caller.SendAsync("FieldMessage", json);
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
                try
                {
                    this.service.Game.Stop(this, EventArgs.Empty);
                    this.service.IsPaused = true;
                }
                catch (Exception)
                {
                }

            }
            return base.OnDisconnectedAsync(exception);
        }

        public async Task OnInput(string e)
        {
            if (this.service.IsGameOver)
            {
                this.RestartGame();
                this.service.Game.Start(this, EventArgs.Empty);
                return;
            }

            MoveSnakeContainer mv;
            try
            {



                List<byte> bb = new List<byte>();
                foreach (var item in e.Split(","))
                {
                    bb.Add(byte.Parse(item));
                }

                

                using (MemoryStream ms = new MemoryStream(bb.ToArray()))
                {
                    BinaryFormatter bin = new BinaryFormatter();
                    mv = (MoveSnakeContainer)bin.Deserialize(ms);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogInformation(ex.Message);
                return;
            }
            
            //MoveSnakeContainer z = Newtonsoft.Json.JsonConvert.DeserializeObject<MoveSnakeContainer>(e);
            this.logger.LogInformation(mv.SnakeMoveCommand + " pressed by " + Context.ConnectionId);

            if (mv.SnakeMoveCommand.Id == 999)
            {
                return;
            }
            else
            {
                DirectionEventArgs directionEvent = null;

                switch (mv.SnakeMoveCommand.Id)
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
                
                //await Task.Factory.StartNew(() => this.service.Game.GetInput(this, directionEvent));
                this.service.Game.GetInput(this, directionEvent);
                this.logger.LogInformation(Context.ConnectionId + "command went through");
            }
        }

        private async void RestartGame()
        {
            this.logger.LogInformation("Game restarted");
            this.service.Game = new Application();
            this.service.Game.OnGameOver += this.OnGameOver;
            this.service.Game.OnMessageReceived += this.OnSendMessage;
            this.service.Game.OnContainerCreated += this.SendContainer;
            this.service.IsGameOver = false;
            logger.LogInformation("Game start.");
            var field = this.service.Game.GetField();
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(new FieldPrintContainer(field.Width, field.Length, field.Icon.Character));

            await this.snakeContext.Clients.All.SendAsync("FieldMessage", json);
        }
    }
}
