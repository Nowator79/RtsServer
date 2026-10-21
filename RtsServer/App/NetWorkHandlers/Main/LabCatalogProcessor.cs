using RtsServer.App.Battle.Lab;
using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto.Response;
using RtsServer.App.NetWorkResponseSender;

namespace RtsServer.App.NetWorkHandlers.Main
{
    public class LabCatalogProcessor : IProcessor
    {
        public async Task Handler(MainResponse response, GameServer context, UserClientTcp clientTcp, CancellationToken cancellationToken)
        {
            await new LabCatalogSender(clientTcp)
                .SetDate(LabCatalogBuilder.Get())
                .SendAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
