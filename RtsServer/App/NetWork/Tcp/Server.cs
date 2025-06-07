using Microsoft.Extensions.Logging;
using RtsServer.App.NetWorkHandlers;

namespace RtsServer.App.NetWork.Tcp
{
    public class Server : Base
    {
        public Server(int port, MainProcessor processor, ILogger<GameServer> logger) : base(port, processor, logger)
        {

        }

        public new void Run()
        {
            base.Run();
        }
    }
}
