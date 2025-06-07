using RtsServer.App.Exceptions;
using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto.Response;
using System.Text.Json;

namespace RtsServer.App.NetWorkResponseSender
{
    public class SendAllChatSender : NetWorkSenderBase
    {
        public SendAllChatSender(UserClientTcp clientApi) : base(clientApi)
        {
            _response = new("chat", "/chat/init/", "", "200");
        }

        public override NetWorkSenderBase SetDate(object data)
        {
            _response.SetBody(data);
            return this;
        }
    }
}
