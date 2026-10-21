using RtsServer.App.NetWork.Tcp;

namespace RtsServer.App.NetWorkResponseSender
{
    public class AuthFailSender : NetWorkSenderBase
    {
        public AuthFailSender(UserClientTcp clientApi) : base(clientApi)
        {
            _response = new("auth", "/auth/loginFailed/", "", "401");
        }

        public override NetWorkSenderBase SetDate(object data)
        {
            _response.SetBody(data);
            return this;
        }
    }
}
