using RtsServer.Classes.NetWork.Tcp;
using RtsServer.Classes.Processor.Response;

namespace RtsServer.Classes.Processor
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
