using Microsoft.Extensions.Logging;
using RtsServer.App.DataBase;
using RtsServer.App.DataBase.Dto;
using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto;
using RtsServer.App.NetWorkDto.Response;
using RtsServer.App.NetWorkResponseSender;

namespace RtsServer.App.NetWorkHandlers.Auth
{
    public class Login : IProcessor
    {
        public async Task Handler(MainResponse response, GameServer context, UserClientTcp clientTcp, CancellationToken cancellationToken)
        {
            NUser? userAuth = response.GetBody<NUser>();
            if (userAuth == null)
                return;

            using ApplicationContext db = new();
            UserAuth? AUser = db.Users.FirstOrDefault(
                e => e.UserName == userAuth.Value.UserName &&
                e.Password == userAuth.Value.Password
                );
            if (AUser == null) return;
            /// todo
            AUser.Status.SetInPassive();
            clientTcp.SetUser(AUser);

            await new CurUserDataSender(clientTcp)
                .SetDate(Adapters.UserAdapter.Get(clientTcp.User))
                .SendAsync(cancellationToken)
                .ConfigureAwait(false);

            context.GetLogger<Login>().LogInformation("Клиент {ClientId} авторизовался под {UserName}", clientTcp.Id, clientTcp.User.UserName);
        }
    }
}
