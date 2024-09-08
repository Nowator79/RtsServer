using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto.Response;

namespace RtsServer.App.NetWorkHandlers.Game.Battle
{
    public class BattleCancelQueue : IProcessor
    {
        public void Handler(MainResponse response, GameServer context, UserClientTcp clientTcp)
        {
            clientTcp.User.Status.SetInPassive();
            context.BattleManager.RemoveUserForSearch(clientTcp.User);
        }
    }
}

