using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto.Response;

namespace RtsServer.App.NetWorkHandlers.Game.Battle
{
    public class BattleExit : IProcessor
    {
        public void Handler(MainResponse response, GameServer context, UserClientTcp clientTcp)
        {
            context.BattleManager.EndBattleByUser(clientTcp.User);
        }
    }
}

