using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeServer
{
    using Microsoft.Extensions.Logging;
    using Snake_V_0_3;

    public class SnakeHub : Hub
    {
        private Application game;

        private ILogger<SnakeHub> logger;

        public SnakeHub(ILogger<SnakeHub> ilogger)
        {
            this.game = new Application();
            this.logger = ilogger;
            //Events
        }

        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            return base.OnDisconnectedAsync(exception);
        }

        public async Task OnInput()
        {

        }
    }
}
