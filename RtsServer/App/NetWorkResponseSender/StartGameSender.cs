using RtsServer.App.Exceptions;
using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto.Response;

namespace RtsServer.App.NetWorkResponseSender
{
    public class StartGameSender : NetWorkSenderBase
    {
        public StartGameSender(UserClientTcp clientApi) : base(clientApi)
        {
            response = new("battle", "/gameBattle/startGame/", "200");
        }

        public override NetWorkSenderBase SetDate(IResponse data)
        {
            throw new ExceptionBlockedFunction();
        }
    }
}
