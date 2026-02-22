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
                    try
                    {
                        game?.Players
                        .Where(player => player.UserAuth == clientTcp.User)
                        .First()
                        .SetReady();
                    }
                    catch (Exception) { /** TODO */}
                }
            );
        }
    }
}
