﻿using RtsServer.App.DataBase;
using RtsServer.App.DataBase.Dto;
using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto;
using RtsServer.App.NetWorkDto.Response;

namespace RtsServer.App.NetWorkHandlers.Auth
{
    public class Login : IProcessor
    {
        public void Handler(MainResponse response, GameServer context, UserClientTcp clientTcp)
        {
            NUser? userAuth = response.GetBody<NUser>();
            if (userAuth != null)
            {
                using ApplicationContext db = new();
                UserAuth? AUser = db.Users.FirstOrDefault(
                    e => e.UserName == userAuth.Value.UserName &&
                    e.Password == userAuth.Value.Password
                    );
                if (AUser == null) return;

                AUser.Status.SetInPassive();
                clientTcp.SetUser(AUser);
                Console.WriteLine($"Клиент {clientTcp.Id} авторизовался под {clientTcp.User.UserName}");
            }
        }
    }
}