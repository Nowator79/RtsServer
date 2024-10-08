﻿using RtsServer.App.NetWork.Tcp;

namespace RtsServer.App.NetWorkResponseSender
{
    public class StartGameSender : NetWorkSenderBase
    {
        public StartGameSender(UserClientTcp clientApi) : base(clientApi)
        {
            response = new("battle", "/gameBattle/startGame/", "200");
        }

        public override NetWorkSenderBase SetDate(object data)
        {
            response.SetBody(data);
            return this;
        }
    }
}
