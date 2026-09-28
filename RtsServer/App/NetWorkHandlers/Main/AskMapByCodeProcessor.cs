using RtsServer.App.Adapters;
using RtsServer.App.FileSystem;
using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto;
using RtsServer.App.NetWorkDto.Response;
using RtsServer.App.NetWorkResponseSender;

namespace RtsServer.App.NetWorkHandlers.Main
{
    public class AskMapByCodeProcessor : IProcessor
    {
        public async Task Handler(MainResponse response, GameServer context, UserClientTcp clientTcp, CancellationToken cancellationToken)
        {
            var mapRequest = response.GetBody<NMapRequest>();
            MapFileManager mapFileManager = new();
            NMap map = MapAdapter.GetNMap(mapFileManager.LoadMapByCode(mapRequest.Code), mapRequest.Code);
            await new LoadMapSender(clientTcp).SetDate(map).SendAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
