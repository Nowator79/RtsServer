using RtsServer.App.Battle.Chat;
using RtsServer.App.DataBase.Dto;
using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto;
using RtsServer.App.NetWorkDto.Response;
using RtsServer.App.NetWorkResponseSender;

namespace RtsServer.App.NetWorkHandlers.Game.Chat
{
    public class Open : IProcessor
    {
        public async Task Handler(MainResponse response, GameServer context, UserClientTcp clientTcp, CancellationToken cancellationToken)
        {
            User chatUser = new(clientTcp.User);
            context.ChatSystem.AddUser(chatUser);

            List<NUser> NUsers = [];
            List<NMessage> NMessages = [];

            List<UserAuth> usersAuth = context.GetAuthenticatedUsers();
            usersAuth.ForEach((user) =>
            {
                NUsers.Add(new NUser(user.Id, user.UserName, user.Level, user.Experience));
            });

            List<Message> messages = context.ChatSystem.GetChat();
            messages.ForEach((message) =>
            {
                NMessages.Add(new(message.user.UserAuth.Id, message.message));
            });

            NChat chat = new([.. NUsers], [.. NMessages]);

            await new SendAllChatSender(clientTcp)
                .SetDate(chat)
                .SendAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
