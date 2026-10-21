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
    public class Login : IProcessor
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
                await SendFail(clientTcp, "Пустое тело логина", cancellationToken).ConfigureAwait(false);
                return;
            }
            using ApplicationContext db = new();

            bool byToken = !string.IsNullOrWhiteSpace(req.Token);
            UserAuth? user = AuthService.Login(db, req.UserName, req.Password, req.Token);
            if (user == null)
            {
                await SendFail(
                        clientTcp,
                        byToken ? "Неверный или просроченный токен" : "Неверный логин или пароль",
                        cancellationToken)
                    .ConfigureAwait(false);
                return;
            }

            // По паролю — всегда новый токен. По токену — оставляем тот же.
            if (!byToken || string.IsNullOrWhiteSpace(user.AuthToken))
                AuthService.RefreshToken(db, user);

            user.Status.SetInPassive();
            clientTcp.SetUser(user);
            context.DbUsers.Add(user);

            await new CurUserDataSender(clientTcp)
                .SetDate(UserAdapter.Get(user))
                .SendAsync(cancellationToken)
                .ConfigureAwait(false);

            context.GetLogger<Login>().LogInformation(
                "Клиент {ClientId} авторизовался как {UserName} ({Mode})",
                clientTcp.Id,
                user.UserName,
                byToken ? "token" : "password");
        }

        private static Task SendFail(UserClientTcp clientTcp, string message, CancellationToken cancellationToken)
        {
            return new AuthFailSender(clientTcp)
                .SetDate(new NAuthFail(message))
                .SendAsync(cancellationToken);
        }
    }
}
