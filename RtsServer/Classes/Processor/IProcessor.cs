using RtsServer.Classes.NetWork.Tcp;
using RtsServer.Classes.Processor.Response;


namespace RtsServer.Classes.Processor
{
    public interface IProcessor
    {
        public void Handler(MainResponse response, GameServer context, UserClientTcp clientTcp);
    }
}
