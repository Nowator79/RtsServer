using RtsServer.App.Exceptions;
using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto.Response;
using System.Text.Json;

namespace RtsServer.App.NetWorkResponseSender
{
    public class SendMessageSender : NetWorkSenderBase
    {
        public SendMessageSender(UserClientTcp clientApi) : base(clientApi)
        {
            _response = new("chat", "/chat/send/", "", "200");
        }

        public override NetWorkSenderBase SetDate(object data)
        {
            _response.SetBody(data);
            return this;
        }
    }
}
