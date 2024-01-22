using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto.Response;

namespace RtsServer.App.NetWorkHandlers
{
    public class MainProcessor
    {
        public GameServer GameServer;
        public void Handler(MainResponse response, UserClientTcp clientTcp)
        {
            GameServer.Router.Do(response, clientTcp);
        }

        public void SetContext(GameServer server)
        {
            GameServer = server;
        }
    }
}
