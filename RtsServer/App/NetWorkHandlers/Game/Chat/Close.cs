using RtsServer.App.Battle.Chat;
using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto.Response;

namespace RtsServer.App.NetWorkHandlers.Game.Chat
{
    public class Close : IProcessor
    {
        public void Handler(MainResponse response, GameServer context, UserClientTcp clientTcp)
        {
            User user = context.BattleManager.Chat.FindUserByUserAuth(clientTcp.User);
            if (user != null)
            {
                context.BattleManager.Chat.RemoveUser(user);
            }
        }
    }
}
