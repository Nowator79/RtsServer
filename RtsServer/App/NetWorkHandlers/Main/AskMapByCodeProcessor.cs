using RtsServer.App.Adapters;
using RtsServer.App.Battle.MapBattle;
using RtsServer.App.FileSystem;
using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto;
using RtsServer.App.NetWorkDto.Response;
using RtsServer.App.NetWorkResponseSender;

namespace RtsServer.App.NetWorkHandlers.Main
{
    public class AskMapByCodeProcessor : IProcessor
    {
        public void Handler(MainResponse response, GameServer context, UserClientTcp clientTcp, CancellationToken cancellationToken)
        {
            var mapRequest = response.GetBody<NMapRequest>();
            MapFileManager mapFileManager = new();
            NMap map = MapAdapter.GetNMap(mapFileManager.LoadMapByCode(mapRequest.Code), mapRequest.Code);
            _ = new LoadMapSender(clientTcp).SetDate(map).SendAsync(cancellationToken);
        }
    }
}
