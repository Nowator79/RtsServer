using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto.Response;
using System.Threading;

namespace RtsServer.App.NetWorkHandlers
{
    public class MainProcessor
    {
        public GameServer GameServer;
        public void Handler(MainResponse response, UserClientTcp clientTcp, CancellationToken cancellationToken)
        {
            GameServer.Router.Do(response, clientTcp, cancellationToken);
        } 

        public void SetContext(GameServer server)
        {
            GameServer = server;
        }
    }
}
