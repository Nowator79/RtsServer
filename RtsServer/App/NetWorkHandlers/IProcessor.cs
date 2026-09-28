using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto.Response;

namespace RtsServer.App.NetWorkHandlers
{
    public interface IProcessor
    {
        Task Handler(MainResponse response, GameServer context, UserClientTcp clientTcp, CancellationToken cancellationToken);
    }
}
