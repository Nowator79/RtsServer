using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto.Response;
using System.Linq;

namespace RtsServer.App.NetWorkHandlers.Game
{
    public class GetGameForUser : IProcessor
    {  
        public void Handler(MainResponse response, GameServer context, UserClientTcp clientTcp)
        {

            MainResponse responseGame = new(response.type, response.action, response.status);
            Buttle.Game? game = context.ButtleManager.Games.Find(
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
            
            clientTcp.Write(responseGame);
        }
    }
}
