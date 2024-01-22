using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto.Response;

namespace RtsServer.App.NetWorkHandlers
{
    public interface IProcessor
    {
        public void Handler(MainResponse response, GameServer context, UserClientTcp clientTcp);
    }
}
