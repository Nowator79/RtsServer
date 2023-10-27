using RtsServer.Classes.NetWork.Tcp;
using RtsServer.Classes.Processor.Response;

namespace RtsServer.Classes.Processor.Game
{
    public class GetGameForUser : ProcessorBase
    {  
        public override void Handler(MainResponse response, GameServer context, UserClientTcp clientTcp)
        {
            MainResponse responseGame = new(response.type, response.action, response.status);
            Buttle.Game game = context.ButtleManager.Games.First();

            DtoNetWorkRts.ButtleData.Game GameNetWork = new();

            responseGame.SetBody(game);
            clientTcp.Write(responseGame);
        }
    }
}
