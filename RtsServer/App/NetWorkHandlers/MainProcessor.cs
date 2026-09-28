using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto.Response;

namespace RtsServer.App.NetWorkHandlers
{
    public class MainProcessor
    {
        public GameServer GameServer;

        public Task Handler(MainResponse response, UserClientTcp clientTcp, CancellationToken cancellationToken)
        {
            return GameServer.Router.DoAsync(response, clientTcp, cancellationToken);
        }

        public void SetContext(GameServer server)
        {
            GameServer = server;
        }
    }
}
