using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto.Response;

namespace RtsServer.App.NetWorkHandlers.Game.Battle
{
    internal class BattlePlayerIsReady : IProcessor
    {
        public void Handler(MainResponse response, GameServer context, UserClientTcp clientTcp, CancellationToken cancellationToken)
        {
            context.BattleManager.Games.ForEach(game =>
                {
                    var player = game?.Players.FirstOrDefault(p => p.UserAuth == clientTcp.User);
                    player?.SetReady();
                }
            );
        }
    }
}
