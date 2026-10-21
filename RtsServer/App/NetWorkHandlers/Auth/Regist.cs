using Microsoft.Extensions.Logging;
using RtsServer.App.Adapters;
using RtsServer.App.DataBase;
using RtsServer.App.DataBase.Dto;
using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto;
using RtsServer.App.NetWorkDto.Response;
using RtsServer.App.NetWorkResponseSender;

namespace RtsServer.App.NetWorkHandlers.Auth
{
    public class Regist : IProcessor
    {
        public async Task Handler(MainResponse response, GameServer context, UserClientTcp clientTcp, CancellationToken cancellationToken)
        {
            NUser req;
            try
            {
                req = response.GetBody<NUser>();
            }
            catch
            {
                await new AuthFailSender(clientTcp)
                    .SetDate(new NAuthFail("Логин и пароль обязательны"))
                    .SendAsync(cancellationToken)
                    .ConfigureAwait(false);
                return;
            }

            if (string.IsNullOrWhiteSpace(req.UserName) || string.IsNullOrWhiteSpace(req.Password))
            {
                await new AuthFailSender(clientTcp)
                    .SetDate(new NAuthFail("Логин и пароль обязательны"))
                    .SendAsync(cancellationToken)
                    .ConfigureAwait(false);
                return;
            }
            using ApplicationContext db = new();
            if (db.Users.Any(e => e.UserName == req.UserName))
            {
                await new AuthFailSender(clientTcp)
                    .SetDate(new NAuthFail("Такой логин уже занят"))
                    .SendAsync(cancellationToken)
                    .ConfigureAwait(false);
                return;
            }

            UserAuth user = UserAdapter.Get(req);
            user.AuthToken = AuthService.IssueToken();
            db.Users.Add(user);
            db.SaveChanges();

            user.Status.SetInPassive();
            clientTcp.SetUser(user);
            context.DbUsers.Add(user);

            await new CurUserDataSender(clientTcp)
                .SetDate(UserAdapter.Get(user))
                .SendAsync(cancellationToken)
                .ConfigureAwait(false);

            context.GetLogger<Regist>().LogInformation(
                "Зарегистрирован {UserName} (client {ClientId})",
                user.UserName,
                clientTcp.Id);
        }
    }
}
