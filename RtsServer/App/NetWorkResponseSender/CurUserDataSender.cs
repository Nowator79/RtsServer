using RtsServer.App.Exceptions;
using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto.Response;

namespace RtsServer.App.NetWorkResponseSender
{
    public class CurUserDataSender : NetWorkSenderBase
    {
        public CurUserDataSender(UserClientTcp clientApi) : base(clientApi)
        {
            response = new("auth", "/auth/setCurUser/", "200");
        }

        public override NetWorkSenderBase SetDate(object data)
        {
            response.SetBody(data);
            return this;
        }
    }
}
