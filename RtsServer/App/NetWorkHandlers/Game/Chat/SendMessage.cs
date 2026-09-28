using RtsServer.App.Battle.Chat;
using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto;
using RtsServer.App.NetWorkDto.Response;

namespace RtsServer.App.NetWorkHandlers.Game.Chat
{
    internal class SendMessage : IProcessor
    {
        public async Task Handler(MainResponse response, GameServer context, UserClientTcp clientTcp, CancellationToken cancellationToken)
        {
            NMessage NMessage = response.GetBody<NMessage>();
            User user = context.ChatSystem.FindUserByUserAuth(clientTcp.User);
            await context.ChatSystem.SendMessageAsync(new(NMessage.Message, user), cancellationToken).ConfigureAwait(false);
        }
    }
}
