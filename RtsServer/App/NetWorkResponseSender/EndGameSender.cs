using RtsServer.App.Exceptions;
using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto.Response;

namespace RtsServer.App.NetWorkResponseSender
{
    public class EndGameSender : NetWorkSenderBase
    {
        public EndGameSender(UserClientTcp clientApi) : base(clientApi)
        {
            _response = new("battle", "/gameBattle/endGame/", "", "200");
        }

        public override NetWorkSenderBase SetDate(object data)
        {
            _response.SetBody(data);
            return this;
        }
    }
}
