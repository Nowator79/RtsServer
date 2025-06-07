using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto.Response;
using System.Linq;
using System.Text.Json;

namespace RtsServer.App.NetWorkHandlers.Game
{
    public class GetGameForUser : IProcessor
    {  
        public async void Handler(MainResponse response, GameServer context, UserClientTcp clientTcp, CancellationToken cancellationToken = default)
        {

            MainResponse responseGame = new(response.Type, response.Action, "", response.Status);
            App.Battle.Game? game = context.BattleManager.Games.Find(
                    gameItem =>
                    {
                        bool isThisUser = false;
                        gameItem.Players.ForEach(player =>
                        {
                            isThisUser = player.UserAuth == clientTcp.User;
                        });
                        return isThisUser;
                    }
                );

            if (game == null) return;

            responseGame.SetBody(game);
            
            await clientTcp.WriteAsync(responseGame, cancellationToken);
        }

    }
}
