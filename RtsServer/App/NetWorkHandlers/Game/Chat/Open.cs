using RtsServer.App.Battle.Chat;
using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto.Response;

namespace RtsServer.App.NetWorkHandlers.Game.Chat
{
    public class Open : IProcessor
    {
        public void Handler(MainResponse response, GameServer context, UserClientTcp clientTcp)
        {
            User chatUser = new(clientTcp.User);
            context.BattleManager.Chat.AddUser(chatUser);
        }
    }
}
