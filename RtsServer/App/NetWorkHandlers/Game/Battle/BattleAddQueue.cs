using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto;
using RtsServer.App.NetWorkDto.Response;

namespace RtsServer.App.NetWorkHandlers.Game.Battle
{
    public class BattleAddQueue : IProcessor
    {
        public void Handler(MainResponse response, GameServer context, UserClientTcp clientTcp, CancellationToken cancellationToken)
        {
            if (clientTcp.User != null)
            {
                clientTcp.User.Status.SetInSearch();
                NStartBattleData startBattleData = response.GetBody<NStartBattleData>();
                
                context.BattleManager.AddUserToQueue(new App.Battle.Queue(clientTcp.User, startBattleData.Type));
            }
        }
    }
}