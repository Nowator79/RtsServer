using RtsServer.App.Battle;
using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto;
using RtsServer.App.NetWorkDto.Response;

namespace RtsServer.App.NetWorkHandlers.Game
{
    public class BuildConstructionProcessor : IProcessor
    {
        public void Handler(MainResponse response, GameServer context, UserClientTcp clientTcp, CancellationToken cancellationToken)
        {
            App.Battle.Game? game = context.BattleManager.Games.Find(
                    gameItem =>
                    {
                        bool isThisUser = false;

                        foreach (App.Battle.Player player in gameItem.Players)
                        {
                            isThisUser = player.UserAuth.Id == clientTcp.User.Id;
                            if (isThisUser) break;
                        }

                        return isThisUser;
                    }
                );
            NBuildConstructionRequest request = response.GetBody<NBuildConstructionRequest>();
            if (game != null)
            {
                Player player = game.Players.First(p => p.UserAuth == clientTcp.User);
                game.TryBuildConstruction(request.Code, request.Position, player.Id);
            }
        }
    }
}
