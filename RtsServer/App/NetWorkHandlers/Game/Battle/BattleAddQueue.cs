using RtsServer.App.Adapters;
using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto.Response;

namespace RtsServer.App.NetWorkHandlers.Game.Battle
{
    public class BattleAddQueue : IProcessor
    {
        public void Handler(MainResponse response, GameServer context, UserClientTcp clientTcp)
        {
            clientTcp.User.Status.SetInSearch();
            context.BattleManager.AddUserForSearch(clientTcp.User);
        }
    }
}

