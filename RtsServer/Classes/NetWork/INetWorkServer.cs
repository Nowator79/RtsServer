using RtsServer.Classes.NetWork.Tcp;
using RtsServer.Classes.Processor;
using RtsServer.Classes.Processor.Response;

namespace RtsServer.Classes.NetWork
{
    public interface INetWorkServer
    {
        public void Run();
        public void DisconectUser(UserClientTcp userClient);
        public MainProcessor GetProcessor();
        public void Exit();
    }
}
