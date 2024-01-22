using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkHandlers;

namespace RtsServer.App.NetWork
{
    public interface INetWorkServer
    {
        public void Run();
        public void DisconectUser(UserClientTcp userClient);
        public MainProcessor GetProcessor();
        public void Exit();
    }
}
